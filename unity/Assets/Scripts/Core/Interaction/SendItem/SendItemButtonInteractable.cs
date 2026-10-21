using Core.Player;
using Core.SFX;
using UnityEngine;

namespace Core.Interaction
{
    public class SendItemButtonInteractable : Interactable
    {
        [SerializeField] private Animator sendItemButtonAnimator;
        [SerializeField] private SendItemInteractable sendItemInteractable;

        public override bool CanInteract(PlayerController playerController)
            => sendItemInteractable != null && sendItemInteractable.CanSendItem;

        public override string GetInteractionHint(PlayerController playerController) => "Send sample";
        
        public override void Interact(PlayerController playerController)
        {
            sendItemButtonAnimator.SetTrigger("Activate");
            if (sendItemInteractable == null)
            {
                Debug.LogError("SendItemInteractable is null, cannot launch animation.");
            }
            else
            {
                sendItemInteractable.SendItem();
            }
            Debug.Log("Interacting with " + playerController.gameObject.name);
        }

        public void PlayLeverSound()
        {
            SFXController.Instance.PlayInteraction(SFXDatabase.Instance.leverClip);
        }

        public override void InteractHold(PlayerController playerController)
        {
            Debug.LogWarning("InteractHold -- Useless in SendItemButtonInteractable");
        }
    }
}
