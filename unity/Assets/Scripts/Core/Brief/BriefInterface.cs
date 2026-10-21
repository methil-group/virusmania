using System;
using Core.Input;
using Core.PostProcess;
using Core.Timer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Brief
{
    public class BriefInterface : MonoBehaviour
    {
        [Header("Actual Brief Panel References")]
        public RectTransform actualBriefPanel;
        public TextMeshProUGUI actualBriefTitle;
        public TextMeshProUGUI actualBriefDescription;
        public TextMeshProUGUI actualBriefMoneyGiven;
        
        [Header("New Brief Panel References")]
        public RectTransform briefPanel;
        public TextMeshProUGUI briefNameText;
        public TextMeshProUGUI briefDescriptionText;
        public TextMeshProUGUI briefMoneyGivenText;
        public Image briefVirusImage;
        public Button briefEndButton;
        [SerializeField] protected Image blackPanel;

        private Brief _tempNewBrief;
        private Vector2 _originalActualBriefPosition;
        private Brief _lastDisplayedBrief;
        private bool _isActualBriefVisible = false;

        private Material _actualBriefMaterial;
        public bool IsBriefPending => _tempNewBrief != null;
        
        public void Start()
        {
            _actualBriefMaterial = new Material(actualBriefPanel.GetComponent<Image>().material);
            actualBriefPanel.GetComponent<Image>().material = _actualBriefMaterial;
            briefEndButton.onClick.AddListener(PutNewBrief);
            _originalActualBriefPosition = actualBriefPanel.anchoredPosition;
            actualBriefPanel.gameObject.SetActive(false);

            if (blackPanel != null)
            {
                blackPanel.color = new Color(0, 0, 0, 0);
            }
        }

        public void OnDestroy()
        {
            if (briefEndButton != null)
                briefEndButton.onClick.RemoveListener(PutNewBrief);
            if (_actualBriefMaterial != null) Destroy(_actualBriefMaterial);
        }

        private void OnDisable()
        {
            InputDatabase.Instance?.SetGameplayBlocked(this, false);
            if (briefPanel != null) LeanTween.cancel(briefPanel);
        }

        public void Update()
        {
            if (BriefController.Instance.actualBrief == null)
            {
                if (_isActualBriefVisible)
                {
                    _lastDisplayedBrief = null;
                    HideActualBriefWithAnimation();
                }
            }
            else
            {
                _actualBriefMaterial.SetFloat("_AspectRatio", actualBriefPanel.rect.width / actualBriefPanel.rect.height);
                
                if (_lastDisplayedBrief != BriefController.Instance.actualBrief)
                {
                    _lastDisplayedBrief = BriefController.Instance.actualBrief;
                    ShowActualBriefWithAnimation();
                }
                
                actualBriefTitle.text = BriefController.Instance.actualBrief.briefTitle;
                actualBriefDescription.text = BriefController.Instance.actualBrief.briefDescription;
                actualBriefMoneyGiven.text = $"{BriefController.Instance.actualBrief.moneyGiven} $";
            }
        }
        
        private void ShowActualBriefWithAnimation()
        {
            _isActualBriefVisible = true;
            actualBriefPanel.gameObject.SetActive(true);
            actualBriefPanel.anchoredPosition = new Vector2(_originalActualBriefPosition.x - 1000f, _originalActualBriefPosition.y);
            LeanTween.moveX(actualBriefPanel, _originalActualBriefPosition.x, .6f)
                .setEase(LeanTweenType.easeSpring);
        }
        
        private void HideActualBriefWithAnimation()
        {
            _isActualBriefVisible = false;
            LeanTween.moveX(actualBriefPanel, _originalActualBriefPosition.x - 1000f, .4f)
                .setEase(LeanTweenType.easeSpring)
                .setOnComplete((() =>
                {
                    actualBriefPanel.gameObject.SetActive(false);
                }));
        }

        public void SetupNewBriefShow(Brief brief)
        {
            if (brief == null) return;
            _tempNewBrief = brief;
            briefEndButton.interactable = true;
            PostProcessController.Instance.OnShowPanelPostProcess();
            InputDatabase.Instance.SetGameplayBlocked(this, true);
            
            briefPanel.GetComponent<RectTransform>().localScale = Vector3.zero;
            briefPanel.gameObject.SetActive(true);
            GamepadNavigation.SelectFirstSelectable(briefPanel.gameObject);
            
            LeanTween.scale(briefPanel.GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), .4f)
                .setEase(LeanTweenType.easeSpring).setIgnoreTimeScale(true);
            
            if (blackPanel != null)
            {
                LeanTween.cancel(blackPanel.gameObject);
                LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0f, 0f, 0f, 0.6f), 1f)
                    .setEaseOutCirc();
            }
            
            briefNameText.text = brief.briefTitle;
            briefDescriptionText.text = brief.briefDescription;
            briefMoneyGivenText.text = $"{brief.moneyGiven} $";
            briefVirusImage.sprite = brief.wantedItem.itemIcon;
            
            _tempNewBrief = brief;
            
        }

        public void PutNewBrief()
        {
            if (_tempNewBrief == null) return;
            briefEndButton.interactable = false;
            BriefController.Instance.actualBrief = _tempNewBrief;
            TimerController.Instance.LaunchTimer(_tempNewBrief.timeForBrief);
            _tempNewBrief = null;
            
            HideBriefPanel();
        }
        
        public void HideBriefPanel()
        {
            
            if (blackPanel != null)
            {
                LeanTween.cancel(blackPanel.gameObject);
                LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0f,0f,0f,0f), 1f)
                    .setEaseOutCirc();
            }
            LeanTween.scale(briefPanel.GetComponent<RectTransform>(), new Vector3(0f, 0f, 0f), .4f)
                .setEase(LeanTweenType.easeOutCirc).setIgnoreTimeScale(true)
                .setOnComplete((() =>
                {
                    briefPanel.gameObject.SetActive(false);
                    InputDatabase.Instance.SetGameplayBlocked(this, false);
                    if (!InputDatabase.Instance.GameplayBlocked)
                        PostProcessController.Instance.OnHidePanelPostProcess();
                }));
        }
    }
}
