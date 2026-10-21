using System;
using Core.Interaction;
using Framework;
using Framework.Extensions;
using Core.Item.Holder;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Core.SFX;
using Core.Input;

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
        [SerializeField] private LayerMask interactionLayers = ~0;

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

        private Collider[] _hits = new Collider[32];
        private Interactable _target;
        private Interactable _holdTarget;
        public Interactable CurrentTarget => _target;
        public string InteractionHint => _target != null ? _target.GetInteractionHint(_playerController) : string.Empty;

        public UnityAction<HoldItem> OnItemAdded;
        public UnityAction<HoldItem> OnItemRemoved;

        public override void Start(PlayerController controller)
        {
            _playerController = controller;
            _playerMovement = controller.updatables.FirstOfType<PlayerMovement>();
            var prompt = controller.GetComponent<PlayerInteractionPrompt>();
            if (prompt == null) prompt = controller.gameObject.AddComponent<PlayerInteractionPrompt>();
            prompt.Setup(this, controller);

            if (interactionAction == null || interactionHoldAction == null)
                return;

            _interactionInput = interactionAction.action;
            _interactionHoldInput = interactionHoldAction.action;

            _interactionInput.performed += OnInteractionPerformed;

            _interactionHoldInput.performed += OnInteractionHoldPerformed;
            _interactionHoldInput.canceled += OnInteractionHoldCanceled;
            InputDatabase.Instance.RefreshGameplayInputs();
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
            SetTarget(null, controller);
            _playerController = null;
        }

        private void OnInteractionPerformed(InputAction.CallbackContext context)
        {
            if (_playerController != null)
                Interact(_playerController);
        }

        private void OnInteractionHoldPerformed(InputAction.CallbackContext context)
        {
            if (!CanUseInputs()) return;
            _isInteractingHeld = true;
            _holdTarget = FindTarget(_playerController);
        }

        private void OnInteractionHoldCanceled(InputAction.CallbackContext context)
        {
            _isInteractingHeld = false;
            _holdTarget = null;
        }

        public override void Update(PlayerController controller)
        {
            if (!CanUseInputs())
            {
                _isInteractingHeld = false;
                _holdTarget = null;
                SetTarget(null, controller);
                return;
            }
            SetTarget(FindTarget(controller), controller);
            if (_target != null) _target.InInteractZone(controller);
            if (_isInteractingHeld)
                InteractHold(controller);
        }

        private bool CanUseInputs()
        {
            return _playerController != null && Time.timeScale > 0f &&
                   InputDatabase.Instance != null && !InputDatabase.Instance.GameplayBlocked;
        }

        private Interactable FindTarget(PlayerController controller)
        {
            if (controller?.body == null || _playerMovement == null) return null;
            Vector3 direction = _playerMovement.direction.sqrMagnitude > 0.001f
                ? _playerMovement.direction : controller.body.forward;
            Vector3 center = controller.body.position.AddY(1.4f) + direction * interactionDistance;
            int count;
            while ((count = Physics.OverlapSphereNonAlloc(center, interactionRadius, _hits,
                       interactionLayers, QueryTriggerInteraction.Collide)) == _hits.Length)
                Array.Resize(ref _hits, _hits.Length * 2);
            Interactable best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var candidate = _hits[i].GetComponentInParent<Interactable>();
                if (candidate == null || !candidate.isActiveAndEnabled || !candidate.CanInteract(controller)) continue;
                float distance = (_hits[i].ClosestPoint(center) - center).sqrMagnitude;
                if (distance < bestDistance - 0.0001f ||
                    (Mathf.Abs(distance - bestDistance) <= 0.0001f &&
                     (candidate == _target || (best != _target &&
                      (best == null || candidate.GetEntityId().CompareTo(best.GetEntityId()) < 0)))))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private void SetTarget(Interactable target, PlayerController controller)
        {
            if (_target == target) return;
            if (_target != null) _target.HideIndicator(controller);
            _target = target;
        }

        public void InteractHold(PlayerController playerController)
        {
            if (!CanUseInputs()) return;
            if (_holdTarget == null || _holdTarget != _target || !_holdTarget.CanInteract(playerController))
            {
                _isInteractingHeld = false;
                return;
            }
            _holdTarget.InteractHold(playerController);
        }

        public void Interact(PlayerController controller)
        {
            if (!CanUseInputs()) return;
            SetTarget(FindTarget(controller), controller);
            if (_target != null) _target.Interact(controller);
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
