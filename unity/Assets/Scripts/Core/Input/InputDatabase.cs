using System;
using System.Collections.Generic;
using Framework.ScriptableObjects;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Input
{
    [CreateAssetMenu(fileName = "InputDatabase", menuName = "Input/InputDatabase")]
    public class InputDatabase : SingletonScriptableObject<InputDatabase>
    {
        [Header("Movement Input")]
        public InputActionReference moveAction;
        
        [Header("Interaction Input")]
        public InputActionReference interactionAction;
        public InputActionReference interactionHoldAction;
        
        public InputActionReference pauseAction;
        public InputActionReference cancelAction;

        [NonSerialized] private HashSet<UnityEngine.Object> _gameplayBlockers = new HashSet<UnityEngine.Object>();
        [NonSerialized] private bool _legacyBlocked;

        public bool GameplayBlocked
        {
            get
            {
                _gameplayBlockers.RemoveWhere(owner => owner == null);
                return _legacyBlocked || _gameplayBlockers.Count > 0;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetSession()
        {
            if (Instance == null) return;
            Instance._gameplayBlockers.Clear();
            Instance._legacyBlocked = false;
            Instance.RefreshGameplayInputs();
            Instance.EnablePauseInput();
        }

        public void SetGameplayBlocked(UnityEngine.Object owner, bool blocked)
        {
            if (owner == null) return;
            if (blocked) _gameplayBlockers.Add(owner);
            else _gameplayBlockers.Remove(owner);
            RefreshGameplayInputs();
        }

        public void RefreshGameplayInputs()
        {
            SetEnabled(moveAction, !GameplayBlocked);
            SetEnabled(interactionAction, !GameplayBlocked);
            SetEnabled(interactionHoldAction, !GameplayBlocked);
        }

        private static void SetEnabled(InputActionReference reference, bool enabled)
        {
            if (reference?.action == null) return;
            if (enabled) reference.action.Enable();
            else reference.action.Disable();
        }

        public void DisableMovementInputs()
        {
            _legacyBlocked = true;
            RefreshGameplayInputs();
        }

        public void EnableMovementInputs()
        {
            _legacyBlocked = false;
            RefreshGameplayInputs();
        }

        public void DisablePauseInput()
        {
            SetEnabled(pauseAction, false);
        }

        public void EnablePauseInput()
        {
            SetEnabled(pauseAction, true);
        }
    }
}
