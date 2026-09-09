using System;
using Core.Input;
using Framework;
using Framework.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Player
{
    [Serializable]
    public class PlayerMovement : Updatable<PlayerController>
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 5f;
        [SerializeField] private float acceleration = 10f;
        
        private InputActionReference _moveAction;
        
        [Header("Animation Settings")]
        [SerializeField] private Animator animator;

        [Header("Audio")]
        [SerializeField] private AudioSource walkSource;

        private Rigidbody rb;
        private Vector3 currentVelocity;
        
        [NonSerialized] public Vector3 direction = Vector3.zero;

        public override void Start(PlayerController controller)
        {
            _moveAction = InputDatabase.Instance.moveAction;
            _moveAction.action.Enable();
            rb = controller.GetComponent<Rigidbody>();
            walkSource.loop = true;
            walkSource.clip = SFXDatabase.Instance.walkClip;
        }

        public override void FixedUpdate(PlayerController controller)
        {
            Vector2 input = _moveAction.action.ReadValue<Vector2>();
            Vector3 targetVelocity = new Vector3(input.x, 0, input.y) * speed;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            animator.SetFloat("Speed", currentVelocity.magnitude);
            Vector3 velocityChange = currentVelocity - rb.linearVelocity;
            velocityChange.y = 0;
            rb.AddForce(velocityChange, ForceMode.VelocityChange);

            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                if (!walkSource.isPlaying) walkSource.Play();
            }
            else
            {
                if (walkSource.isPlaying) walkSource.Stop();
            }
            if (currentVelocity.sqrMagnitude > 0.001f) {
                direction = currentVelocity.normalized;
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, acceleration * Time.fixedDeltaTime));
            }
        }

        public override void OnDrawGizmos(PlayerController controller)
        {
            if (direction == Vector3.zero) return;
            Gizmos.color = Color.cyan;
            Vector3 origin = controller.body.transform.position;
            origin = origin.AddY(1.4f);
            Vector3 end = origin + direction * 2f;
            Gizmos.DrawLine(origin, end);
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
            Gizmos.DrawLine(end, end + right * 0.5f);
            Gizmos.DrawLine(end, end + left * 0.5f);
        }
    }
}
