using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Core.Input
{
    public sealed class GamepadNavigation : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var navigationObject = new GameObject(nameof(GamepadNavigation));
            DontDestroyOnLoad(navigationObject);
            navigationObject.AddComponent<GamepadNavigation>();
        }

        private void Update()
        {
            if (Gamepad.current == null || EventSystem.current == null)
                return;

            ClearInvalidSelection();

            if (EventSystem.current.currentSelectedGameObject == null)
                SelectFirstSelectable();
        }

        public static void SelectFirstSelectable(GameObject root = null)
        {
            if (EventSystem.current == null)
                return;

            Selectable candidate = null;
            var selectables = root == null
                ? Selectable.allSelectablesArray
                : root.GetComponentsInChildren<Selectable>(true);

            foreach (var selectable in selectables)
            {
                if (selectable.isActiveAndEnabled && selectable.IsInteractable())
                {
                    candidate = selectable;
                    break;
                }
            }

            if (candidate != null)
                EventSystem.current.SetSelectedGameObject(candidate.gameObject);
        }

        public static void ClearSelection(GameObject root)
        {
            if (EventSystem.current == null || root == null)
                return;

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(root.transform))
                EventSystem.current.SetSelectedGameObject(null);
        }

        private static void ClearInvalidSelection()
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
                return;

            var selectable = selected.GetComponent<Selectable>();
            if (!selected.activeInHierarchy || selectable == null || !selectable.IsInteractable())
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
