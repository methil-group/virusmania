using System;
using System.Collections.Generic;
using Core.Item;
using Core.Item.Holder;
using Core.Player;
using Framework.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Interaction
{
    public class ItemHolderInteractable : Interactable
    {
        [SerializeField] protected int maxHoldableItems = 3;
        [SerializeField] protected List<Item.Item> startItems;
        [SerializeField] protected Transform itemParent;
        [SerializeField] protected Vector3 itemOffset = new Vector3(0, 1f, 0.5f);

        public UnityAction<HoldItem> OnItemAdded;
        public UnityAction<HoldItem> OnItemRemoved;
        public UnityAction OnItemsChanged;

        [NonSerialized]
        public List<HoldItem> HoldingItems = new List<HoldItem>();
        protected List<GameObject> spawnedPrefabs = new List<GameObject>();

        public override void Start()
        {
            base.Start();
            if (startItems != null && startItems.Count > 0)
            {
                foreach (var item in startItems)
                {
                    if (item != null && HoldingItems.Count < maxHoldableItems)
                        AddItem(item.GetHoldItem());
                }
            }
        }
        
        public override void InInteractZone(PlayerController playerController)
        {
            PlayerInteraction playerInteraction = playerController.updatables.FirstOfType<PlayerInteraction>();
            if (playerInteraction == null) return;
            
            if (playerInteraction.HasItem)
            {
                if (this.HoldingItems.Count < maxHoldableItems) base.InInteractZone(playerController);
            } else
            {
                if (this.HoldingItems.Count > 0) base.InInteractZone(playerController);
            }
        }

        public virtual bool CanPlayerAddItem(PlayerController playerController)
        {
            return HoldingItems.Count < maxHoldableItems;
        }

        public override void Interact(PlayerController playerController)
        {
            PlayerInteraction playerInteraction = playerController.updatables.FirstOfType<PlayerInteraction>();
            if (playerInteraction == null) return;

            if (playerInteraction.HasItem)
            {
                if (CanPlayerAddItem(playerController))
                {
                    HoldItem removedItem = playerInteraction.RemoveItem();
                    if (removedItem != null)
                        AddItem(removedItem);
                }
            }
            else
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

        public virtual void AddItem(HoldItem holdItem)
        {
            if (holdItem == null || holdItem.Item == null) return;
            HoldingItems.Insert(0, holdItem);

            if (holdItem.Item.itemPrefab != null)
            {
                Transform parent = itemParent != null ? itemParent : transform;
                GameObject spawned = Instantiate(holdItem.Item.itemPrefab, parent);
                spawnedPrefabs.Insert(0, spawned);
            }

            UpdateItemPositions();
            OnItemAdded?.Invoke(holdItem);
            OnItemsChanged?.Invoke();
        }

        public virtual void RemoveItem(HoldItem holdItem)
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

                UpdateItemPositions();
                OnItemRemoved?.Invoke(holdItem);
                OnItemsChanged?.Invoke();
            }
        }

        public override void InteractHold(PlayerController playerController)
        {
            Debug.Log("Interacting hold with " + gameObject.name);
        }

        protected virtual void UpdateItemPositions()
        {
            if (spawnedPrefabs == null || spawnedPrefabs.Count == 0) return;
            int count = spawnedPrefabs.Count;
            Transform parent = itemParent != null ? itemParent : transform;
            Vector3 basePos = parent.position + itemOffset;

            if (count == 1)
            {
                spawnedPrefabs[0].transform.position = basePos;
            }
            else if (count == 2)
            {
                spawnedPrefabs[0].transform.position = basePos + parent.right * -0.45f;
                spawnedPrefabs[1].transform.position = basePos + parent.right * 0.45f;
            }
            else
            {
                float radius = 0.5f;
                float angleStep = 360f / count;

                for (int i = 0; i < count; i++)
                {
                    float angle = angleStep * i * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    spawnedPrefabs[i].transform.position = basePos + offset;
                    spawnedPrefabs[i].transform.LookAt(basePos);
                }
            }
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            Vector3 titlePos = transform.position + Vector3.up * 3f;
            GUIStyle titleStyle = new GUIStyle
            {
                normal = new GUIStyleState { textColor = Color.cyan },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            UnityEditor.Handles.Label(titlePos, gameObject.name, titleStyle);

            Vector3 originPos = (itemParent != null ? itemParent.position : transform.position) + itemOffset;
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(originPos, 0.05f);
            UnityEditor.Handles.Label(originPos + Vector3.up * 0.1f, "Item Origin", new GUIStyle
            {
                normal = new GUIStyleState { textColor = Color.magenta },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            });

            if (HoldingItems != null && HoldingItems.Count > 0)
            {
                Vector3 basePos = transform.position + Vector3.up * 2f;
                GUIStyle style = new GUIStyle
                {
                    normal = new GUIStyleState { textColor = Color.yellow },
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };

                for (int i = 0; i < HoldingItems.Count; i++)
                {
                    var holdItem = HoldingItems[i];
                    if (holdItem?.Item != null)
                    {
                        Vector3 pos = basePos + Vector3.up * (0.3f * i);
                        UnityEditor.Handles.Label(pos, holdItem.Item.itemName, style);
                    }
                }
            }
        }
#endif
    }
}
