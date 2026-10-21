using System;
using Core.Player;
using Core.Prefab;
using Core.Input;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Interaction
{
    public abstract class Interactable : MonoBehaviour
    {
        private GameObject _interactionIndicator;
        private bool _indicatorVisible;
        private TMP_Text _indicatorLabel;

        public UnityAction InInteraction;
        public UnityAction OutInteraction;

        [SerializeField]
        private Vector3 interactionIndicatorOffset;

        public virtual void Awake()
        {
            if (PrefabDatabase.Instance != null && PrefabDatabase.Instance.interactionIndicationPrefab != null)
            {
                Transform parentToAttach = transform.parent != null ? transform.parent : null;

                _interactionIndicator = Instantiate(
                    PrefabDatabase.Instance.interactionIndicationPrefab,
                    parentToAttach
                );

                _interactionIndicator.transform.position = transform.position + new Vector3(0f, 3.5f, 0.5f);
                _interactionIndicator.transform.position =
                    (_interactionIndicator.transform.position + interactionIndicatorOffset);
                _interactionIndicator.transform.rotation = Quaternion.identity;
                _interactionIndicator.transform.localScale = Vector3.zero;
                _interactionIndicator.SetActive(false);
                _indicatorLabel = _interactionIndicator.GetComponentInChildren<TMP_Text>(true);
            }
        }

        public virtual void Start()
        {
        }

        public virtual void InInteractZone(PlayerController playerController)
        {
            if (_indicatorLabel != null && InputDatabase.Instance != null)
            {
                var action = string.IsNullOrEmpty(GetInteractionHint(playerController))
                    ? InputDatabase.Instance.interactionHoldAction : InputDatabase.Instance.interactionAction;
                string label = PlayerInteractionPrompt.GetBindingLabel(action);
                if (_indicatorLabel.text != label) _indicatorLabel.text = label;
            }
            if (_indicatorVisible) return;
            _indicatorVisible = true;
            InInteraction?.Invoke();
            if (_interactionIndicator != null)
            {
                LeanTween.cancel(_interactionIndicator);

                _interactionIndicator.SetActive(true);
                Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
                float desiredWorldScale = 0.0024f;
                Vector3 targetScale = new Vector3(
                    desiredWorldScale / parentScale.x,
                    desiredWorldScale / parentScale.y,
                    desiredWorldScale / parentScale.z
                );
                LeanTween.scale(_interactionIndicator, targetScale, 0.3f).setEaseOutBack();
            }
        }

        public void HideIndicator(PlayerController playerController)
        {
            if (!_indicatorVisible) return;
            _indicatorVisible = false;
            OutInteraction?.Invoke();
            if (_interactionIndicator != null && _interactionIndicator.activeSelf)
            {
                LeanTween.cancel(_interactionIndicator);

                LeanTween.scale(_interactionIndicator, Vector3.zero, 0.2f).setEaseInBack().setOnComplete(() =>
                {
                    _interactionIndicator.SetActive(false);
                });
            }
        }

        public virtual bool CanInteract(PlayerController playerController)
        {
            return true;
        }

        public virtual string GetInteractionHint(PlayerController playerController) => "Use";
        public virtual string GetHoldInteractionHint(PlayerController playerController) => string.Empty;

        protected virtual void OnDestroy()
        {
            if (_interactionIndicator != null) Destroy(_interactionIndicator);
        }

        public abstract void Interact(PlayerController playerController);
        public abstract void InteractHold(PlayerController playerController);
    }
}
