using System;
using Core.Input;
using Core.PostProcess;
using Core.Scene;
using Core.Timer;
using Framework.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Core.End
{
    public class EndInterface : BaseController<EndInterface>
    {
        public Image blackPanel;
        
        public RectTransform openWinPanel;
        public RectTransform openLosePanel;

        private bool IsOnePanelActive()
        {
            return openWinPanel.gameObject.activeSelf || openLosePanel.gameObject.activeSelf;
        }
        
        public void Start()
        {
            blackPanel.color = new Color(0, 0, 0, 0);
            openWinPanel.gameObject.SetActive(false);
            openLosePanel.gameObject.SetActive(false);
            
            TimerController.Instance.OnTimerEnd += OpenLosePanel;
        }

        public void OnDisable()
        {
            TimerController.Instance.OnTimerEnd -= OpenLosePanel;
        }

        public void OpenWinPanel()
        {
            if (IsOnePanelActive()) return;
            InputDatabase.Instance.DisableMovementInputs();
            InputDatabase.Instance.DisablePauseInput();
            LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0, 0, 0, 0.6f), 0.6f)
                .setEaseOutCirc();;
            
            openWinPanel.GetComponent<RectTransform>().localScale = Vector3.zero;
            openWinPanel.gameObject.SetActive(true);
            GamepadNavigation.SelectFirstSelectable(openWinPanel.gameObject);
            if (PostProcessController.Instance != null) 
                PostProcessController.Instance.OnShowPanelPostProcess();
            LeanTween.cancel(openWinPanel);
            LeanTween.scale(openWinPanel.GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), .4f)
                .setEase(LeanTweenType.easeSpring);
        }

        public void OpenLosePanel()
        {
            if (IsOnePanelActive()) return;
            InputDatabase.Instance.DisableMovementInputs();
            InputDatabase.Instance.DisablePauseInput();
            LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0, 0, 0, 0.6f), 0.6f)
                .setEaseOutCirc();;
            
            openLosePanel.GetComponent<RectTransform>().localScale = Vector3.zero;
            openLosePanel.gameObject.SetActive(true);
            GamepadNavigation.SelectFirstSelectable(openLosePanel.gameObject);
            if (PostProcessController.Instance != null) 
                PostProcessController.Instance.OnShowPanelPostProcess();
            LeanTween.cancel(openLosePanel);
            LeanTween.scale(openLosePanel.GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), .4f)
                .setEase(LeanTweenType.easeSpring);
        }

        public void ReturnToMainMenu()
        {
            InputDatabase.Instance.EnableMovementInputs();
            InputDatabase.Instance.EnablePauseInput();
            var scene = SceneDatabase.Instance.GetSceneByName("MainMenu");
            SceneTransitor.Instance.LoadScene(scene);
        }
    }
}
