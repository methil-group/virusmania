using System;
using System.Collections.Generic;
using Core.Interaction;
using Framework;
using Framework.Extensions;
using Core.Item.Holder;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Core.SFX;

namespace Core.Player
{
    [Serializable]
    public class PlayerInteraction : Updatable<PlayerController>
    {
        private PlayerMovement _playerMovement;
        private PlayerController _playerController;
        private InputAction _interactionInput;
        private InputAction _interactionHoldInput;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 2f;
        [SerializeField] private float interactionRadius = 0.5f;

        [Header("Interaction Input")]
        [SerializeField] private InputActionReference interactionAction;
        [SerializeField] private InputActionReference interactionHoldAction;

        [Header("Animation Settings")]
        [SerializeField] private Animator animator;

        [Header("Item Holding")]
        [SerializeField] private Transform holdingItemTransform;

        private HoldItem _holdingItem;
        private GameObject _spawnedHeldItem;
        private bool _isInteractingHeld;

        public HoldItem HoldingItem => _holdingItem;
        public bool HasItem => _holdingItem != null;

        private HashSet<Interactable> _interactablesInRange = new HashSet<Interactable>();

        public UnityAction<HoldItem> OnItemAdded;
        public UnityAction<HoldItem> OnItemRemoved;

        public override void Start(PlayerController controller)
        {
            _playerController = controller;
            _playerMovement = controller.updatables.FirstOfType<PlayerMovement>();

            if (interactionAction == null || interactionHoldAction == null)
                return;

            _interactionInput = interactionAction.action;
            _interactionHoldInput = interactionHoldAction.action;

            _interactionInput.performed += OnInteractionPerformed;
            _interactionInput.Enable();

            _interactionHoldInput.performed += OnInteractionHoldPerformed;
            _interactionHoldInput.canceled += OnInteractionHoldCanceled;
            _interactionHoldInput.Enable();
        }

        public override void OnDestroy(PlayerController controller)
        {
            if (_interactionInput != null)
                _interactionInput.performed -= OnInteractionPerformed;

            if (_interactionHoldInput != null)
            {
                _interactionHoldInput.performed -= OnInteractionHoldPerformed;
                _interactionHoldInput.canceled -= OnInteractionHoldCanceled;
            }

            _isInteractingHeld = false;
            _playerController = null;
        }

        private void OnInteractionPerformed(InputAction.CallbackContext context)
        {
            if (_playerController != null)
                Interact(_playerController);
        }

        private void OnInteractionHoldPerformed(InputAction.CallbackContext context)
        {
            _isInteractingHeld = true;
        }

        private void OnInteractionHoldCanceled(InputAction.CallbackContext context)
        {
            _isInteractingHeld = false;
        }

        public override void Update(PlayerController controller)
        {
            if (_isInteractingHeld)
                InteractHold(controller);

            Vector3 origin = controller.body.transform.position.AddY(1.4f);
            Vector3 center = origin + _playerMovement.direction * interactionDistance;

            Collider[] hits = Physics.OverlapSphere(center, interactionRadius);

            HashSet<Interactable> currentFrameInteractables = new HashSet<Interactable>();

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Interactable>(out var interactable) &&
                    interactable.CanInteract(controller))
                {
                    interactable.InInteractZone(controller);
                    currentFrameInteractables.Add(interactable);
                    break;
                }
            }

            foreach (var oldInteractable in _interactablesInRange)
            {
                if (!currentFrameInteractables.Contains(oldInteractable))
                {
                    oldInteractable.HideIndicator(controller);
                }
            }

            _interactablesInRange = currentFrameInteractables;
        }

        public void InteractHold(PlayerController playerController)
        {
            Vector3 origin = playerController.body.transform.position.AddY(1.4f);
            Vector3 center = origin + _playerMovement.direction * interactionDistance;

            Collider[] hits = Physics.OverlapSphere(center, interactionRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Interactable>(out var holdable) &&
                    holdable.CanInteract(playerController))
                {
                    holdable.InteractHold(playerController);
                    break;
                }
            }
        }

        public void Interact(PlayerController controller)
        {
            Vector3 origin = controller.body.transform.position.AddY(1.4f);
            Vector3 center = origin + _playerMovement.direction * interactionDistance;

            Collider[] hits = Physics.OverlapSphere(center, interactionRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Interactable>(out var interactable) &&
                    interactable.CanInteract(controller))
                {
                    interactable.Interact(controller);
                    break;
                }
            }
        }

        public HoldItem RemoveItem()
        {
            var item = _holdingItem;
            _holdingItem = null;

            if (_spawnedHeldItem != null)
            {
                UnityEngine.Object.Destroy(_spawnedHeldItem);
                _spawnedHeldItem = null;
            }

            animator.SetBool("Holding", false);

            if (SFXDatabase.Instance.putItemClip != null)
                SFXController.Instance.PlayInteraction(SFXDatabase.Instance.putItemClip);

            if (item != null)
                OnItemRemoved?.Invoke(item);

            return item;
        }

        public bool GiveItem(HoldItem newItem)
        {
            if (_holdingItem != null || newItem == null || newItem.Item == null)
                return false;

            _holdingItem = newItem;
            animator.SetBool("Holding", true);

            if (SFXDatabase.Instance.getItemClip != null)
                SFXController.Instance.PlayInteraction(SFXDatabase.Instance.getItemClip);

            SpawnHeldItem();

            OnItemAdded?.Invoke(newItem);

            return true;
        }

        private void SpawnHeldItem()
        {
            if (_holdingItem?.Item?.itemPrefab == null || holdingItemTransform == null)
                return;

            Vector3 prefabEuler = _holdingItem.Item.itemPrefab.transform.rotation.eulerAngles;
            Vector3 holdEuler = holdingItemTransform.rotation.eulerAngles;

            Quaternion finalRot = Quaternion.Euler(prefabEuler.x, holdEuler.y, holdEuler.z);

            _spawnedHeldItem = UnityEngine.Object.Instantiate(
                _holdingItem.Item.itemPrefab,
                holdingItemTransform.position,
                finalRot,
                holdingItemTransform
            );
        }

        public override void OnDrawGizmos(PlayerController controller)
        {
            if (controller == null || controller.body == null || _playerMovement == null) return;

            Gizmos.color = Color.yellow;
            Vector3 origin = controller.body.transform.position.AddY(1.4f);
            Vector3 center = origin + _playerMovement.direction * interactionDistance;
            Gizmos.DrawWireSphere(center, interactionRadius);

            if (_holdingItem?.Item == null) return;

#if UNITY_EDITOR
            GUIStyle style = new GUIStyle
            {
                normal = new GUIStyleState { textColor = Color.yellow },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            UnityEditor.Handles.Label(origin, _holdingItem.Item.itemName, style);
#endif
        }
    }
}
