using System;
using System.Linq;
using Core.Brief;
using Core.Player;
using Framework.Extensions;
using UnityEngine;
using Core.Item.Holder;
using Core.SFX;
using Core.Timer;
using UnityEngine.Events;

namespace Core.Interaction
{
    public class SendItemInteractable : ItemHolderInteractable
    {
        [SerializeField] private Animator threadMillAnimator;
        [SerializeField] private Vector3 objectDestination;
        [SerializeField] private Renderer alarmRenderer;
        [SerializeField] private Light alarmLight;

        public UnityAction<Item.Item> onItemSent;

        [SerializeField] private bool isOnBoarding = false;
        private bool _isSendingItem = false;

        public override void Start()
        {
            base.Start();
            alarmRenderer.material = new Material(alarmRenderer.material);
            alarmLight.intensity = 0;
            alarmLight.color = Color.white;
            alarmRenderer.material.color = Color.white;
        }
        
        public override void InInteractZone(PlayerController playerController)
        {
            PlayerInteraction playerInteraction = playerController.updatables.FirstOfType<PlayerInteraction>();
            if (playerInteraction == null) return;
            if (_isSendingItem) return;
            
            if (playerInteraction.HasItem)
            {
                if (this.HoldingItems.Count < maxHoldableItems) base.InInteractZone(playerController);
            } else if (this.HoldingItems.Count > 0)
            {
                base.InInteractZone(playerController);
            }
        }

        public void SendItem()
        {
            if (_isSendingItem) return;

            HoldItem holdItem = HoldingItems.Count > 0 ? HoldingItems.First() : null;
            if (holdItem?.Item == null)
            {
                Debug.LogWarning("Cannot send an empty item.");
                return;
            }

            if (spawnedPrefabs.Count == 0 || spawnedPrefabs.First() == null)
            {
                Debug.LogError($"Cannot send {holdItem.Item.itemName}: its item prefab is missing.");
                return;
            }

            _isSendingItem = true;
            threadMillAnimator.SetBool("Roll", true);
            LeanTween.delayedCall(2.084f, () =>
            {
                _isSendingItem = false;
                threadMillAnimator.SetBool("Roll", false);
            });

            LeanTween.moveLocalX(spawnedPrefabs.First(), objectDestination.x, 1.6f).setOnComplete(() =>
            {
                onItemSent?.Invoke(holdItem.Item);
                RemoveItem(holdItem);
            });

            if (isOnBoarding == true) return;
            if (BriefController.Instance != null && BriefController.Instance.CanCompleteBrief(holdItem))
            {
                TimerController.Instance.StopTimer();
                LeanTween.delayedCall(1.4f, () =>
                {
                    alarmRenderer.material.color = Color.green;
                    SFXController.Instance.PlayInteraction(SFXDatabase.Instance.greenAlarmClip);
                    StartBlink(Color.green, () =>
                    {
                        BriefController.Instance.TryToCompleteBrief(holdItem);
                    });
                    Debug.Log("Send Item -> Validate brief !");
                });
            }
            else
            {
                LeanTween.delayedCall(1.4f, () =>
                {
                    SFXController.Instance.PlayInteraction(SFXDatabase.Instance.redAlarmClip);
                    alarmRenderer.material.color = Color.red;
                    StartBlink(Color.red);
                    Debug.Log("Send Item -> Brief not validated...");
                });
            }
        }
        private void StartBlink(Color color, System.Action callback = null)
        {
            LeanTween.cancel(gameObject);

            alarmLight.color = color;

            int flashCount = 6;
            float flashTime = 0.15f;
            float maxIntensity = 5f;

            LTSeq seq = LeanTween.sequence();

            for (int i = 0; i < flashCount; i++)
            {
                seq.append(
                    LeanTween.value(gameObject, 0f, maxIntensity, flashTime)
                        .setOnUpdate(v => alarmLight.intensity = v)
                );

                seq.append(
                    LeanTween.value(gameObject, maxIntensity, 0f, flashTime)
                        .setOnUpdate(v => alarmLight.intensity = v)
                );
            }

            seq.append(() => alarmLight.intensity = 0);
            seq.append(() => alarmLight.color = Color.white);
            seq.append(() => alarmRenderer.material.color = Color.white);
            seq.append(() => callback?.Invoke());
        }

        public override void Interact(PlayerController playerController)
        {
            if(_isSendingItem) return;
            base.Interact(playerController);
        }


        public override void InteractHold(PlayerController playerController)
        {
            
        }
        
#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            Vector3 objectRelativeDestination = (itemParent != null ? itemParent.position : transform.position) + objectDestination;
            Gizmos.color = new Color(0.6470588f, 0.1647059f, 0.1647059f, 1f);
            Gizmos.DrawSphere(objectRelativeDestination, 0.05f);
        }
#endif
    }
}
