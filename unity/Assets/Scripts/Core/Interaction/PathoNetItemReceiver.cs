using System.Collections.Generic;
using Core.Item.Holder;
using Core.Player;
using Framework.Extensions;
using UnityEngine;

namespace Core.Interaction
{
    public class PathoNetItemReceiver : ItemHolderInteractable
    {
        public Animator flapAnimator;
        
        private Queue<HoldItem> itemQueue = new Queue<HoldItem>();

        public override bool CanPlayerAddItem(PlayerController playerController) => false;

        public override bool CanInteract(PlayerController playerController)
        {
            var player = playerController.updatables.FirstOfType<PlayerInteraction>();
            return player != null && !player.HasItem && HoldingItems.Count > 0;
        }


        public override void InInteractZone(PlayerController playerController)
        {
            PlayerInteraction playerInteraction = playerController.updatables.FirstOfType<PlayerInteraction>();
            if (playerInteraction == null) return;
            
            if (!playerInteraction.HasItem)
            {
                if (this.HoldingItems.Count > 0) base.InInteractZone(playerController);
            }
        }
        
        public override void Interact(PlayerController playerController)
        {
            PlayerInteraction playerInteraction = playerController.updatables.FirstOfType<PlayerInteraction>();
            if (playerInteraction == null) return;

            if (!playerInteraction.HasItem)
            {
                if (HoldingItems.Count > 0)
                {
                    HoldItem itemToGive = HoldingItems[HoldingItems.Count - 1];
                    bool givedItem = playerInteraction.GiveItem(itemToGive);
                    if (givedItem)
                        RemoveItem(itemToGive);
                }
            }
        }

        public override void AddItem(HoldItem holdItem)
        {
            if (holdItem == null || holdItem.Item == null) return;

            TriggerFlapAnimation();

            if (HoldingItems.Count >= maxHoldableItems)
            {
                itemQueue.Enqueue(holdItem);
                return;
            }

            HoldingItems.Insert(0, holdItem);

            if (holdItem.Item.itemPrefab != null)
            {
                Transform parent = itemParent != null ? itemParent : transform;
                GameObject spawned = Instantiate(holdItem.Item.itemPrefab, parent);
                spawned.GetComponent<Collider>().enabled = true;
                spawned.AddComponent<Rigidbody>();
                spawnedPrefabs.Insert(0, spawned);
                UpdateItemPosition(spawned);
            }

            OnItemAdded?.Invoke(holdItem);
            OnItemsChanged?.Invoke();
        }

        public override void RemoveItem(HoldItem holdItem)
        {
            if (holdItem == null) return;
            int index = HoldingItems.IndexOf(holdItem);
            if (index >= 0)
            {
                HoldingItems.RemoveAt(index);
                if (index < spawnedPrefabs.Count && spawnedPrefabs[index] != null)
                {
                    Destroy(spawnedPrefabs[index]);
                    spawnedPrefabs.RemoveAt(index);
                }

                OnItemRemoved?.Invoke(holdItem);
                OnItemsChanged?.Invoke();

                if (itemQueue.Count > 0)
                {
                    HoldItem nextItem = itemQueue.Dequeue();
                    AddItem(nextItem);
                }
            }
        }

        public void TriggerFlapAnimation()
        {
            flapAnimator.SetTrigger("Open");
        }

        private void UpdateItemPosition(GameObject itemPrefab)
        {
            if (spawnedPrefabs == null || spawnedPrefabs.Count == 0) return;
            Transform parent = itemParent != null ? itemParent : transform;
            Vector3 basePos = parent.position + itemOffset;

            itemPrefab.transform.position = basePos;
            itemPrefab.transform.LookAt(basePos);
        }

        protected override void UpdateItemPositions()
        {
            return;
        }
    }
}
