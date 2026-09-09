using System;
using System.Collections.Generic;
using Core.Computer;
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
        private InputAction _cancelAction;

        private void Start()
        {
            interfaces = new List<IInterfaceController>()
            {
                ComputerInterface.Instance,
                MergeLibraryInterface.Instance
            };

            InputDatabase.Instance.pauseAction.action.performed += context => CallPauseMenu();

            _cancelAction = InputDatabase.Instance.cancelAction?.action;
            if (_cancelAction != null)
            {
                _cancelAction.performed += OnCancelPerformed;
                _cancelAction.Enable();
            }
        }

        private void OnDestroy()
        {
            if (_cancelAction != null)
                _cancelAction.performed -= OnCancelPerformed;
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            if (!(context.control.device is Gamepad))
                return;

            var openedInterface = interfaces.Find(_interface => _interface.IsOpen);
            if (openedInterface != null)
            {
                openedInterface.ClosePanel();
            }
            else if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen)
            {
                PauseMenu.Instance.ClosePanel();
            }
        }

        public void CallPauseMenu()
        {
            var openedInterface = interfaces.Find(_interface => _interface.IsOpen);

            if (openedInterface != null)
            {
                openedInterface.ClosePanel();
            }
            else
            {
                PauseMenu.Instance.CallPause();
            }
        }
    }
}
