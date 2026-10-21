using Core.Input;
using Core.PostProcess;
using Framework.Controller.Interfaces;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Framework.Controller
{
    public class InterfaceController<T> : BaseController<T>, IInterfaceController 
        where T : InterfaceController<T>
    {
        [SerializeField] protected GameObject panel;
        [SerializeField] protected Image blackPanel;
        [SerializeField, Min(0f)] private float animationDuration = 0.4f;

        public UnityAction OnPanelOpen;
        public UnityAction OnPanelClose;

        private bool _closing;
        private bool _ownsPause;
        private float _previousTimeScale;
        private GameObject _previousSelection;

        protected virtual bool FreezesTime => false;
        public bool IsOpen => panel != null && panel.activeInHierarchy;

        public virtual void Start()
        {
            if (panel != null)
                panel.SetActive(false);

            if (blackPanel != null)
            {
                blackPanel.color = new Color(0, 0, 0, 0);
                blackPanel.raycastTarget = false;
            }
        }

        public virtual bool CanOpen() => !IsOpen && !_closing;

        public virtual void OpenPanel()
        {
            if (!CanOpen() || panel == null) return;
            _previousSelection = EventSystem.current?.currentSelectedGameObject;
            InputDatabase.Instance?.SetGameplayBlocked(this, true);
            if (blackPanel != null) blackPanel.raycastTarget = true;
            if (PostProcessController.Instance != null)
                PostProcessController.Instance.OnShowPanelPostProcess();
            if (FreezesTime)
            {
                _previousTimeScale = Time.timeScale;
                _ownsPause = true;
                Time.timeScale = 0f;
            }

            LeanTween.cancel(panel);
            panel.transform.localScale = Vector3.zero;
            panel.SetActive(true);
            LeanTween.scale(panel, Vector3.one, animationDuration).setEaseSpring().setIgnoreTimeScale(true)
                .setOnComplete(OnOpeningFinished);
            FadeBackground(0.6f);
            GamepadNavigation.SelectFirstSelectable(panel);
            OnPanelOpen?.Invoke();
        }

        protected virtual void OnOpeningFinished() { }

        public virtual void ClosePanel()
        {
            if (!IsOpen || _closing) return;
            _closing = true;
            GamepadNavigation.ClearSelection(panel);
            FadeBackground(0f);
            LeanTween.cancel(panel);
            LeanTween.scale(panel, Vector3.zero, animationDuration)
                .setEaseOutCirc().setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    panel.SetActive(false);
                    if (blackPanel != null) blackPanel.raycastTarget = false;
                    _closing = false;
                    ReleaseInputAndPause();
                    if (_previousSelection != null && _previousSelection.activeInHierarchy)
                        EventSystem.current?.SetSelectedGameObject(_previousSelection);
                    OnPanelClose?.Invoke();
                });
        }

        private void FadeBackground(float alpha)
        {
            if (blackPanel == null) return;
            LeanTween.cancel(blackPanel.gameObject);
            LeanTween.color(blackPanel.rectTransform, new Color(0, 0, 0, alpha), animationDuration)
                .setEaseOutCirc().setIgnoreTimeScale(true);
        }

        private void ReleaseInputAndPause()
        {
            InputDatabase.Instance?.SetGameplayBlocked(this, false);
            if (_ownsPause)
            {
                Time.timeScale = _previousTimeScale;
                _ownsPause = false;
            }
            var postProcess = PostProcessController.Instance;
            if (postProcess != null && (InputDatabase.Instance == null || !InputDatabase.Instance.GameplayBlocked))
                postProcess.OnHidePanelPostProcess();
        }

        protected virtual void OnDisable()
        {
            if (panel != null) LeanTween.cancel(panel);
            if (blackPanel != null) LeanTween.cancel(blackPanel.gameObject);
            _closing = false;
            ReleaseInputAndPause();
        }
    }
}
