using System;
using Core.Input;
using Framework.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Core.MainMenu
{
    public class OptionsController : BaseController<OptionsController>
    {
        public RectTransform optionPanel;
        public float duration = 0.25f;

        public Slider musicSlider;
        public Slider interactionSlider;
        public Slider uiSlider;

        Vector3 _startPos;
        Vector3 _offscreenPos;
        bool _initialized;

        [SerializeField] private ButtonWithPanel[] buttonWithPanel;

        void Start()
        {
            foreach (var b in buttonWithPanel)
            {
                var targetPanel = b.panel;
                b.button.onClick.AddListener(() =>
                {
                    foreach (var bp in buttonWithPanel)
                        bp.panel.SetActive(false);
                    targetPanel.SetActive(true);
                });
                b.panel.gameObject.SetActive(false);
            }
            buttonWithPanel[0].panel.gameObject.SetActive(true);
        }

        void Init()
        {
            if (_initialized) return;
            _initialized = true;

            musicSlider.maxValue = 100f;
            interactionSlider.maxValue = 100f;
            uiSlider.maxValue = 100f;

            musicSlider.value = SFXDatabase.Instance.musicVolume;
            interactionSlider.value = SFXDatabase.Instance.interactionVolume;
            uiSlider.value = SFXDatabase.Instance.uiVolume;

            musicSlider.onValueChanged.AddListener(v => SFXDatabase.Instance.MusicVolume = v);
            interactionSlider.onValueChanged.AddListener(v => SFXDatabase.Instance.InteractionVolume = v);
            uiSlider.onValueChanged.AddListener(v => SFXDatabase.Instance.UserInterfaceVolume = v);

            _startPos = optionPanel.localPosition;
            _offscreenPos = _startPos + new Vector3(optionPanel.rect.width * 1.5f, 0, 0);
            optionPanel.localPosition = _offscreenPos;
            optionPanel.gameObject.SetActive(false);
        }

        public void OpenOption()
        {
            Init();
            optionPanel.gameObject.SetActive(true);
            GamepadNavigation.SelectFirstSelectable(optionPanel.gameObject);
            if (MainMenuController.Instance != null)
                LeanTween.scale(MainMenuController.Instance.gameObject, new Vector3(0f, 0f, 0f), duration).setEaseSpring();
            LeanTween.moveLocal(optionPanel.gameObject, _startPos, duration).setEaseSpring();
        }

        public void CloseOption()
        {
            Init();
            GamepadNavigation.ClearSelection(optionPanel.gameObject);
            if (MainMenuController.Instance != null)
                LeanTween.scale(MainMenuController.Instance.gameObject, new Vector3(1f, 1f, 1f), duration).setEaseSpring();
            LeanTween.moveLocal(optionPanel.gameObject, _offscreenPos, duration)
                .setEaseOutCirc()
                .setOnComplete(() => optionPanel.gameObject.SetActive(false));
        }
    }

    [Serializable]
    class ButtonWithPanel
    {
        public Button button;
        public GameObject panel;
    }
}
