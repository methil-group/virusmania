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

        public void DisableMovementInputs()
        {
            Debug.Log("-- Disable Inputs --");
            moveAction.action.Disable();
            interactionAction.action.Disable();
            interactionHoldAction.action.Disable();
        }

        public void EnableMovementInputs()
        {
            Debug.Log("-- Enable Inputs --");
            moveAction.action.Enable();
            interactionAction.action.Enable();
            interactionHoldAction.action.Enable();
        }

        public void DisablePauseInput()
        {
            Debug.Log("-- Disable Pause Action --");
            pauseAction.action.Disable();
        }

        public void EnablePauseInput()
        {
            Debug.Log("-- Enable Pause Action --");
            pauseAction.action.Enable();
        }
    }
}
