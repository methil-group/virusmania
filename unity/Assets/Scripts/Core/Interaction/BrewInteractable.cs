using System.Collections.Generic;
using Core.Item;
using Core.Item.Merge;
using Core.Player;
using UnityEngine;
using Core.Item.Holder;
using Framework.Extensions;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Core.Interaction
{
    public class BrewInteractable : ItemHolderInteractable
    {
        [Header("Fusion System")]
        [SerializeField] private float mergeHoldTime = 5f;
        [SerializeField] private float cooldownSpeed = 1f;
        [SerializeField] private float holdReleaseDelay = 0.1f;
        [SerializeField] private Animator mixingTableAnimator;

        [Header("SFX")]
        [SerializeField] private AudioSource mixingSource;

        public Image holdInteractImage;

        private float holdTimer = 0f;
        private bool isBeingHeld = false;
        private float lastHoldTime = -999f;
        private bool isSliderVisible = false;

        public UnityAction<Item.Item> onItemMerged;
        private Item.Item[] _mergeInputs = System.Array.Empty<Item.Item>();
        private bool CanMergeItems => _mergeInputs.Length >= 2 && MergeUtils.CanMerge(_mergeInputs);

        public override void Start()
        {
            base.Start();

            mixingSource.clip = SFXDatabase.Instance.mergeAudioClip;
            mixingSource.volume = 0f;
            mixingSource.loop = true;

            OnItemAdded += (_ => OnItemChanged());
            OnItemRemoved += (_ => OnItemChanged());
            OnItemChanged();

            holdInteractImage.rectTransform.localScale = Vector2.zero;
        }

        private void Update()
        {
            if (Time.time - lastHoldTime > holdReleaseDelay)
            {
                if (isBeingHeld)
                {
                    isBeingHeld = false;
                    mixingTableAnimator.SetBool("IsWorking", false);
                    FadeOutMixing();
                }
            }

            if (!isBeingHeld && holdTimer > 0f)
                holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * cooldownSpeed);

            if (holdTimer <= 0.3f && isSliderVisible)
            {
                isSliderVisible = false;
                LeanTween.cancel(holdInteractImage.gameObject);
                LeanTween.scale(holdInteractImage.rectTransform, Vector3.zero, 0.4f)
                    .setEase(LeanTweenType.easeInBack);
            }
            else if (holdTimer > 0.3f && !isSliderVisible)
            {
                isSliderVisible = true;
                LeanTween.cancel(holdInteractImage.gameObject);
                LeanTween.scale(holdInteractImage.rectTransform, Vector3.one, 0.4f)
                    .setEase(LeanTweenType.easeOutBack);
            }

            holdInteractImage.material.SetFloat("_InnerFillAmount",
                Mathf.Lerp(holdInteractImage.material.GetFloat("_InnerFillAmount"), holdTimer / mergeHoldTime, 0.1f));
        }

        public override void InInteractZone(PlayerController playerController)
        {
            if (HoldingItems.Count == maxHoldableItems)
            {
                base.InInteractZone(playerController);
                return;
            }

            PlayerInteraction p = playerController.updatables.FirstOfType<PlayerInteraction>();
            if (p == null) return;

            if (p.HasItem || HoldingItems.Count > 0)
                base.InInteractZone(playerController);
        }

        public override void InteractHold(PlayerController playerController)
        {
            if (!CanMergeItems) return;

            if (!isBeingHeld)
                StartMixingSound();

            isBeingHeld = true;
            lastHoldTime = Time.time;
            mixingTableAnimator.SetBool("IsWorking", true);

            if (HoldingItems.Count < 2)
            {
                holdTimer = 0f;
                return;
            }

            holdTimer += Time.deltaTime;

            if (holdTimer >= mergeHoldTime)
            {
                TryMergeItems();
                holdTimer = 0f;
                isBeingHeld = false;
                mixingTableAnimator.SetBool("IsWorking", false);
                FadeOutMixing();
            }
        }

        private void StartMixingSound()
        {
            LeanTween.cancel(mixingSource.gameObject);
            
            if (!mixingSource.isPlaying)
                mixingSource.Play();
            
            LeanTween.value(mixingSource.volume, 1f, 0.25f)
                .setOnUpdate(v => mixingSource.volume = v);
        }

        private void FadeOutMixing()
        {
            if (mixingSource.isPlaying)
            {
                LeanTween.cancel(mixingSource.gameObject);
                LeanTween.value(mixingSource.volume, 0f, 0.3f)
                    .setOnUpdate(v => mixingSource.volume = v)
                    .setOnComplete(() => mixingSource.Stop());
            }
        }

        private void TryMergeItems()
        {
            Item.Item[] items = HoldingItems.ConvertAll(h => h.Item).ToArray();
            HoldItem merged = MergeUtils.TryMerge(items);

            if (merged != null)
            {
                foreach (var h in new List<HoldItem>(HoldingItems))
                    RemoveItem(h);

                onItemMerged?.Invoke(merged.Item);
                AddItem(merged);
                ResetFusion();
            }
        }

        private void ResetFusion()
        {
            holdTimer = 0f;
            isBeingHeld = false;
            lastHoldTime = -999f;
            mixingTableAnimator.SetBool("IsWorking", false);
            FadeOutMixing();
        }

        private void OnItemChanged()
        {
            holdTimer = 0f;
            _mergeInputs = HoldingItems.ConvertAll(h => h.Item).ToArray();
        }

        public override bool CanInteract(PlayerController playerController)
            => base.CanInteract(playerController) || CanMergeItems;

        public override string GetInteractionHint(PlayerController playerController)
            => base.CanInteract(playerController) ? base.GetInteractionHint(playerController) : string.Empty;

        public override string GetHoldInteractionHint(PlayerController playerController)
            => CanMergeItems ? "Mix" : string.Empty;
    }
}
