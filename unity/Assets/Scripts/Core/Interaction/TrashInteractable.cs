using Core.Item.Holder;
using Core.Player;
using Framework.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Interaction
{
    public class TrashInteractable : Interactable
    {
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private AudioSource audioSource;

        public UnityAction<Item.Item> onItemRecycled;

        public override bool CanInteract(PlayerController playerController)
            => playerController.updatables.FirstOfType<PlayerInteraction>()?.HasItem == true;

        public override string GetInteractionHint(PlayerController playerController) => "Recycle";
        
        public override void InInteractZone(PlayerController playerController)
        {
            PlayerInteraction playerInteraction = playerController.updatables.FirstOfType<PlayerInteraction>();
            if (playerInteraction == null) return;
            
            if (playerInteraction.HasItem)
            {
                base.InInteractZone(playerController);
            }
        }
        
        public override void Interact(PlayerController playerController)
        {
            PlayerInteraction playerInteraction = playerController.updatables.FirstOfType<PlayerInteraction>();
            if (playerInteraction == null) return;

            if (playerInteraction.HasItem)
            {
                HoldItem removedItem = playerInteraction.RemoveItem();
                onItemRecycled?.Invoke(removedItem.Item);
                Core.OnBoarding.OnBoardingController.Instance?.RecoverLostItem(removedItem.Item);
                animator.SetTrigger("TriggerTrash");
                audioSource.PlayOneShot(SFXDatabase.Instance.triggerTrashClip);
                HideIndicator(playerController);
            }
        }

        public override void InteractHold(PlayerController playerController)
        {
        }
    }
}
