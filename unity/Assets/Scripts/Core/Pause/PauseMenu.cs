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
        
        public override void Start()
        {
            base.Start();
            
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
            if (this.panel.activeSelf)
            {
                InputDatabase.Instance.EnableMovementInputs();
                ClosePanel();
            }
            else
            {
                InputDatabase.Instance.DisableMovementInputs();
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
                .setEase(LeanTweenType.easeSpring);
            
            if (blackPanel != null)
            {
                LeanTween.cancel(blackPanel.gameObject);
                LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0, 0, 0, 0.6f), 0.6f)
                    .setEaseOutCirc();
            }

            OnPanelOpen?.Invoke();
            panel.SetActive(true);
            GamepadNavigation.SelectFirstSelectable(panel);
        }

        public override void ClosePanel()
        {
            if (!IsOpen) return;
            if (panel == null) return;

            GamepadNavigation.ClearSelection(panel);

            if(PostProcessController.Instance != null) PostProcessController.Instance.OnHidePanelPostProcess();
            InputDatabase.Instance.EnableMovementInputs();
            
            if (blackPanel != null)
            {
                LeanTween.cancel(blackPanel.gameObject);
                LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0, 0, 0, 0f), 0.6f)
                    .setEaseOutCirc();
            }
            
            LeanTween.cancel(panel);
            LeanTween.scale(panel.GetComponent<RectTransform>(), new Vector3(0f, 0f, 0f), .4f)
                .setEase(LeanTweenType.easeOutCirc)
                .setOnComplete((() =>
                {
                    panel.gameObject.SetActive(false);
                }));

            OnPanelClose?.Invoke();
        }

        public void LoadMainMenu()
        {
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
    }
}
