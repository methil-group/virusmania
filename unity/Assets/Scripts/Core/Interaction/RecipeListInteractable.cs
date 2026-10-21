using Core.MergeLibrary;
using Core.Player;
using UnityEngine.Events;

namespace Core.Interaction
{
    public class RecipeListInteractable : Interactable
    {
        public override string GetInteractionHint(PlayerController playerController) => "View recipes";
        public UnityAction OnInteractRecipeList;
        
        public override void Interact(PlayerController playerController)
        {
            OnInteractRecipeList?.Invoke();
            MergeLibraryInterface.Instance.OpenPanel();
            // Silence...
        }

        public override void InteractHold(PlayerController playerController)
        {
            // Silence...
        }
    }
}
