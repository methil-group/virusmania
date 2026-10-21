using System;
using System.Collections.Generic;
using Core.Computer;
using Core.Analysis;
using Core.End;
using Core.Input;
using Core.MergeLibrary;
using Core.Pause;
using Framework.Controller;
using Framework.Controller.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.UI
{
    public class UserInterfaceController : BaseController<UserInterfaceController>
    {
        private List<IInterfaceController> interfaces = new List<IInterfaceController>();
        private InputAction _pauseAction;
        private InputAction _cancelAction;

        private void Start()
        {
            interfaces = new List<IInterfaceController>();
            if (ComputerInterface.Instance != null)
                interfaces.Add(ComputerInterface.Instance);
            if (MergeLibraryInterface.Instance != null)
                interfaces.Add(MergeLibraryInterface.Instance);
            if (AnalysisInterface.Instance != null)
                interfaces.Add(AnalysisInterface.Instance);

            _pauseAction = InputDatabase.Instance?.pauseAction?.action;
            if (_pauseAction != null)
            {
                _pauseAction.performed += OnPausePerformed;
                _pauseAction.Enable();
            }

            _cancelAction = InputDatabase.Instance?.cancelAction?.action;
            if (_cancelAction != null)
            {
                _cancelAction.performed += OnCancelPerformed;
                _cancelAction.Enable();
            }
        }

        private void OnDestroy()
        {
            if (_pauseAction != null)
                _pauseAction.performed -= OnPausePerformed;

            if (_cancelAction != null)
                _cancelAction.performed -= OnCancelPerformed;
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            CallPauseMenu();
        }

        private IInterfaceController GetOpenedInterface()
        {
            return interfaces.Find(interfaceController =>
                interfaceController != null && interfaceController.IsOpen);
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            if (!(context.control.device is Gamepad))
                return;
            if (EndInterface.Instance != null && EndInterface.Instance.IsEnding) return;

            if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen)
            {
                PauseMenu.Instance.ClosePanel();
                return;
            }

            var openedInterface = GetOpenedInterface();
            if (openedInterface != null)
            {
                openedInterface.ClosePanel();
            }
        }

        public void CallPauseMenu()
        {
            if (EndInterface.Instance != null && EndInterface.Instance.IsEnding) return;
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen)
            {
                PauseMenu.Instance.ClosePanel();
                return;
            }
            var openedInterface = GetOpenedInterface();

            if (openedInterface != null)
            {
                openedInterface.ClosePanel();
            }
            else if (PauseMenu.Instance != null)
            {
                PauseMenu.Instance.CallPause();
            }
        }
    }
}
