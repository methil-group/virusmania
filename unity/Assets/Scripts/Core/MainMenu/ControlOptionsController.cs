using System;
using Framework.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Core.MainMenu
{
    public class ControlOptionsController : BaseController<ControlOptionsController>
    {
        [SerializeField] private ControlBindingUI[] bindings;

        void Start()
        {
            foreach (var b in bindings)
            {
                var binding = b;
                binding.button.onClick.AddListener(() => StartRebind(binding));
                UpdateLabel(binding);
            }
        }

        void StartRebind(ControlBindingUI binding)
        {
            var first = binding.bindings[0];
            var action = first.action.action;
            int bindIndex = first.bindingIndex;

            action.Disable();
            binding.label.text = "Press a key...";

            action.PerformInteractiveRebinding(bindIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    op.Dispose();
                    action.Enable();

                    foreach (var b in binding.bindings)
                        SaveBinding(b.action.action);

                    UpdateLabel(binding);

                }).Start();
        }

        void UpdateLabel(ControlBindingUI binding)
        {
            var first = binding.bindings[0];
            var action = first.action.action;
            string display = action.GetBindingDisplayString(first.bindingIndex);

            if (display.StartsWith("Tap"))
                display = display.Replace("Tap ", "");

            binding.label.text = display;
        }

        void SaveBinding(InputAction action)
        {
            var json = action.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(action.name, json);
            PlayerPrefs.Save();
        }

        void OnEnable()
        {
            foreach (var ui in bindings)
            {
                foreach (var pair in ui.bindings)
                {
                    var action = pair.action.action;
                    if (PlayerPrefs.HasKey(action.name))
                    {
                        var json = PlayerPrefs.GetString(action.name);
                        action.LoadBindingOverridesFromJson(json);
                    }
                }
            }
        }
    }


    [Serializable]
    public class ControlBindingUI
    {
        public ActionBindingPair[] bindings;
        public Button button;
        public TextMeshProUGUI label;
    }

    [Serializable]
    public class ActionBindingPair
    {
        public InputActionReference action;
        public int bindingIndex;
    }
}
