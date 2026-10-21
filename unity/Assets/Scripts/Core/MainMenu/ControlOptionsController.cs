using System;
using System.Collections.Generic;
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
        private InputActionRebindingExtensions.RebindingOperation _rebind;
        private readonly Dictionary<InputAction, bool> _previousActionStates = new Dictionary<InputAction, bool>();

        private void Start()
        {
            foreach (var entry in bindings)
            {
                var binding = entry;
                binding.button.onClick.AddListener(() => StartRebind(binding));
                UpdateLabel(binding);
            }
        }

        private void StartRebind(ControlBindingUI binding)
        {
            if (_rebind != null || binding.bindings.Length == 0) return;
            var first = binding.bindings[0];
            var action = first.action.action;
            _previousActionStates.Clear();
            foreach (var pair in binding.bindings)
            {
                var groupedAction = pair.action.action;
                if (_previousActionStates.ContainsKey(groupedAction)) continue;
                _previousActionStates.Add(groupedAction, groupedAction.enabled);
                groupedAction.Disable();
            }
            foreach (var entry in bindings) entry.button.interactable = false;
            binding.label.text = "Press a key... (Esc to cancel)";

            _rebind = action.PerformInteractiveRebinding(first.bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    ApplyBindingPath(binding, action.bindings[first.bindingIndex].effectivePath);
                    foreach (var pair in binding.bindings) SaveBinding(pair.action.action);
                    FinishRebind(binding);
                })
                .OnCancel(op => FinishRebind(binding));
            if (action.bindings[first.bindingIndex].path.StartsWith("<Keyboard>"))
                _rebind.WithControlsHavingToMatchPath("<Keyboard>");
            _rebind.Start();
        }

        public static void ApplyBindingPath(ControlBindingUI binding, string path)
        {
            foreach (var pair in binding.bindings)
                pair.action.action.ApplyBindingOverride(pair.bindingIndex, path);
        }

        private void FinishRebind(ControlBindingUI binding)
        {
            var operation = _rebind;
            _rebind = null;
            operation?.Dispose();
            foreach (var state in _previousActionStates)
                if (state.Value) state.Key.Enable();
            _previousActionStates.Clear();
            foreach (var entry in bindings) entry.button.interactable = true;
            UpdateLabel(binding);
        }

        private void UpdateLabel(ControlBindingUI binding)
        {
            var first = binding.bindings[0];
            binding.label.text = first.action.action.GetBindingDisplayString(first.bindingIndex).Replace("Tap ", "");
        }

        private void SaveBinding(InputAction action)
        {
            PlayerPrefs.SetString(action.name, action.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        private void OnEnable()
        {
            foreach (var binding in bindings)
            {
                foreach (var pair in binding.bindings)
                {
                    var action = pair.action.action;
                    if (PlayerPrefs.HasKey(action.name))
                        action.LoadBindingOverridesFromJson(PlayerPrefs.GetString(action.name));
                }
                UpdateLabel(binding);
            }
        }

        private void OnDisable()
        {
            _rebind?.Cancel();
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
