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

        protected override bool FreezesTime => true;

        public override void Start()
        {
            base.Start();
            Time.timeScale = 1f;
            musicSlider.maxValue = interactionSlider.maxValue = uiSlider.maxValue = 100f;
            musicSlider.value = SFXDatabase.Instance.musicVolume;
            interactionSlider.value = SFXDatabase.Instance.interactionVolume;
            uiSlider.value = SFXDatabase.Instance.uiVolume;
            musicSlider.onValueChanged.AddListener(v => SFXDatabase.Instance.MusicVolume = v);
            interactionSlider.onValueChanged.AddListener(v => SFXDatabase.Instance.InteractionVolume = v);
            uiSlider.onValueChanged.AddListener(v => SFXDatabase.Instance.UserInterfaceVolume = v);
        }

        public void CallPause()
        {
            if (IsOpen) ClosePanel();
            else OpenPanel();
        }

        public override void OpenPanel()
        {
            if (!CanOpen()) return;
            ActivePauseMenu();
            base.OpenPanel();
        }

        public void LoadMainMenu()
        {
            var scene = SceneDatabase.Instance.GetSceneByName("MainMenu");
            if (scene == null)
            {
                Debug.LogError("Scene not found in database: MainMenu");
                return;
            }
            Time.timeScale = 1f;
            SceneTransitor.Instance.LoadScene(scene);
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
