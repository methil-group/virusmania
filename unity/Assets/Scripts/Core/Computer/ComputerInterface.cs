using System;
using System.Collections.Generic;
using Core.Computer.PathoNet;
using Core.Input;
using Core.PostProcess;
using UnityEngine;
using UnityEngine.UI;
using Framework.Controller;

namespace Core.Computer
{
    public class ComputerInterface : InterfaceController<ComputerInterface>
    {
        public override void Start()
        {
            if (InputDatabase.Instance == null)
                Debug.LogError("Input Database is null and will cause errors.");
            
            base.Start();

            panel.GetComponentInChildren<PathoNetInterface>().OnBuyCart += ClosePanel;
        }
        
        public override void OpenPanel()
        {
            if (!CanOpen() || panel == null) return;
            
            panel.GetComponent<RectTransform>().localScale = Vector3.zero;
            
            if (PostProcessController.Instance != null) 
                PostProcessController.Instance.OnShowPanelPostProcess();
            InputDatabase.Instance.DisableMovementInputs();
            
            LeanTween.cancel(panel);
            LeanTween.scale(panel.GetComponent<RectTransform>(), new Vector3(1f, 1f, 1f), .4f)
                .setEase(LeanTweenType.easeSpring);

            if (blackPanel != null)
            {
                LeanTween.cancel(blackPanel.gameObject);
                LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0, 0, 0, 0.6f), 0.6f)
                    .setEaseOutCirc();;
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
            
            if (PostProcessController.Instance != null) 
                PostProcessController.Instance.OnHidePanelPostProcess();
            InputDatabase.Instance.EnableMovementInputs();
            
            if (blackPanel != null)
            {
                LeanTween.cancel(blackPanel.gameObject);
                LeanTween.color(blackPanel.GetComponent<RectTransform>(), new Color(0, 0, 0, 0f), 0.6f)
                    .setEaseOutCirc();;
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
    }
}
