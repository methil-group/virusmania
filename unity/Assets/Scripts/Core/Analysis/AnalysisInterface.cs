using System.Collections.Generic;
using System.Linq;
using Core.Input;
using Core.Item;
using Core.Item.Holder;
using Core.PostProcess;
using Core.Prefab;
using Framework.Controller;
using TMPro;
using UnityEngine;

namespace Core.Analysis
{
    public class AnalysisInterface : InterfaceController<AnalysisInterface>
    {
        [Header("Disease Text")] 
        public TextMeshProUGUI diseaseNameText;
        public RectTransform diseaseThreatContainer;
        
        private HoldItem _holdItem;
        
        public void ShowAnalysis(HoldItem holdItemAnalyzed)
        {
            _holdItem = holdItemAnalyzed;
            diseaseNameText.text = holdItemAnalyzed.Item.itemName;
            foreach (Transform child in diseaseThreatContainer)
            {
                Destroy(child.gameObject);
            }

            if (holdItemAnalyzed is HoldVirusItem holdVirusItem && holdVirusItem.Item is VirusItem holdVirusItemData)
            {
                List<ThreatParameter> threatImpacts = holdVirusItemData.threatParameters.ToList();
                foreach (var impact in threatImpacts)
                {
                    var threatPrefab = Instantiate(PrefabDatabase.Instance.analysisThreatPrefab, diseaseThreatContainer);
                    threatPrefab.GetComponent<ThreatOfDiseasePrefab>().Setup(impact.threatType.threatTypeIcon, impact.threatImpact);
                }
            }
            
            OpenPanel();
        }
        
        public override void OpenPanel()
        {
            if (!CanOpen() || panel == null) return;
            
            panel.GetComponent<RectTransform>().localScale = Vector3.zero;
            
            PostProcessController.Instance.OnShowPanelPostProcess();
            InputDatabase.Instance.DisableMovementInputs();
            
            LeanTween.cancel(panel);
            LeanTween.scale(panel.GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), .4f)
                .setEase(LeanTweenType.easeSpring);
            
            OnPanelOpen?.Invoke();
            panel.SetActive(true);
            GamepadNavigation.SelectFirstSelectable(panel);
        }

        public override void ClosePanel()
        {
            if (!IsOpen) return;
            if (panel == null) return;

            GamepadNavigation.ClearSelection(panel);
            
            PostProcessController.Instance.OnHidePanelPostProcess();
            InputDatabase.Instance.EnableMovementInputs();
            
            LeanTween.cancel(panel);
            LeanTween.scale(panel.GetComponent<RectTransform>(), new Vector3(0f, 0f, 0f), .4f)
                .setEase(LeanTweenType.easeOutCirc)
                .setOnComplete((() =>
                {
                    panel.gameObject.SetActive(false);
                }));
            
            OnPanelClose?.Invoke();
        }
    }
}
