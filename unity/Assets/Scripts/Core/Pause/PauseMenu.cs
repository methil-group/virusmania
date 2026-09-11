using Core.Input;
using Core.PostProcess;
using Core.Scene;
using Framework.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Pause
{
    public class PauseMenu : InterfaceController<PauseMenu>
    {
        public Slider musicSlider;
        public Slider interactionSlider;
        public Slider uiSlider;
        
        public GameObject pauseMenu;
        public GameObject settingsMenu;

        private bool _pauseApplied;
        
        public override void Start()
        {
            base.Start();

            // A scene may be loaded while the previous scene was paused.
            Time.timeScale = 1f;
            _pauseApplied = false;
            
            musicSlider.maxValue = 100f;
            interactionSlider.maxValue = 100f;
            uiSlider.maxValue = 100f;

            musicSlider.value = SFXDatabase.Instance.musicVolume;
            interactionSlider.value = SFXDatabase.Instance.interactionVolume;
            uiSlider.value = SFXDatabase.Instance.uiVolume;

            musicSlider.onValueChanged.AddListener(v => SFXDatabase.Instance.MusicVolume = v);
            interactionSlider.onValueChanged.AddListener(v => SFXDatabase.Instance.InteractionVolume = v);
            uiSlider.onValueChanged.AddListener(v => SFXDatabase.Instance.UserInterfaceVolume = v);
        }

        public void CallPause()
        {
            if (IsOpen)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }

        public override void OpenPanel()
        {
            ActivePauseMenu();
            if (!CanOpen() || panel == null) return;

            panel.GetComponent<RectTransform>().localScale = Vector3.zero;
            if(PostProcessController.Instance != null) PostProcessController.Instance.OnShowPanelPostProcess();
            InputDatabase.Instance.DisableMovementInputs();

            LeanTween.cancel(panel);
            LeanTween.scale(panel.GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), .4f)
                .setEase(LeanTweenType.easeSpring)
                .setIgnoreTimeScale(true);
            
            if (blackPanel != null)
            {
                LeanTween.cancel(blackPanel.gameObject);
                LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0, 0, 0, 0.6f), 0.6f)
                    .setEaseOutCirc()
                    .setIgnoreTimeScale(true);
            }

            OnPanelOpen?.Invoke();
            panel.SetActive(true);
            Time.timeScale = 0f;
            _pauseApplied = true;
            GamepadNavigation.SelectFirstSelectable(panel);
        }

        public override void ClosePanel()
        {
            if (!IsOpen) return;
            if (panel == null) return;

            GamepadNavigation.ClearSelection(panel);

            if(PostProcessController.Instance != null) PostProcessController.Instance.OnHidePanelPostProcess();
            
            if (blackPanel != null)
            {
                LeanTween.cancel(blackPanel.gameObject);
                LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0, 0, 0, 0f), 0.6f)
                    .setEaseOutCirc()
                    .setIgnoreTimeScale(true);
            }
            
            LeanTween.cancel(panel);
            LeanTween.scale(panel.GetComponent<RectTransform>(), new Vector3(0f, 0f, 0f), .4f)
                .setEase(LeanTweenType.easeOutCirc)
                .setIgnoreTimeScale(true)
                .setOnComplete((() =>
                {
                    panel.gameObject.SetActive(false);
                    Time.timeScale = 1f;
                    _pauseApplied = false;
                    InputDatabase.Instance.EnableMovementInputs();
                }));

            OnPanelClose?.Invoke();
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            _pauseApplied = false;
            var sceneName = "MainMenu";
            var newScene = SceneDatabase.Instance.GetSceneByName(sceneName);
            if (newScene != null)
                SceneTransitor.Instance.LoadScene(newScene);
            else
                Debug.LogError("Scene not found in database : " + sceneName);
        }

        public void ActivePauseMenu()
        {
            pauseMenu.SetActive(true);
            settingsMenu.SetActive(false);
        }

        public void ActiveOptionsMenu()
        {
            pauseMenu.SetActive(false);
            settingsMenu.SetActive(true);
        }

        private void OnDisable()
        {
            if (!_pauseApplied) return;

            Time.timeScale = 1f;
            _pauseApplied = false;
        }
    }
}
