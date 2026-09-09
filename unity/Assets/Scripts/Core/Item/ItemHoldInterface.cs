using System;
using Core.Item.Holder;
using Core.Player;
using Framework.Controller;
using Framework.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Item
{
    public class ItemHoldInterface : BaseController<ItemHoldInterface>
    {
        public GameObject itemHoldInterface;
        public Image holdItemImage;
        public TextMeshProUGUI holdItemName;

        private PlayerInteraction _playerInteraction;

        public void Start()
        {
            if (PlayerController.Instance == null)
                return;

            _playerInteraction = PlayerController.Instance.updatables.FirstOfType<PlayerInteraction>();
            if (_playerInteraction == null)
                return;

            _playerInteraction.OnItemAdded += UpdateInterface;
            _playerInteraction.OnItemRemoved += HideInterface;
            itemHoldInterface.gameObject.SetActive(false);
        }

        public void OnDisable()
        {
            if (_playerInteraction == null)
                return;

            _playerInteraction.OnItemAdded -= UpdateInterface;
            _playerInteraction.OnItemRemoved -= HideInterface;
            _playerInteraction = null;
        }

        private void HideInterface(HoldItem holdItem)
        {
            LeanTween.cancel(itemHoldInterface);
            LeanTween.scale(itemHoldInterface, Vector3.zero, 0.15f)
                .setEaseOutCirc()
                .setOnComplete(() => itemHoldInterface.SetActive(false));
        }

        private void UpdateInterface(HoldItem holdItem)
        {
            LeanTween.cancel(itemHoldInterface);
            itemHoldInterface.transform.localScale = Vector3.zero;
            itemHoldInterface.SetActive(true);

            if (holdItem.Item.itemIcon != null)
                holdItemImage.sprite = holdItem.Item.itemIcon;

            holdItemName.text = holdItem.Item.itemName;

            LeanTween.scale(itemHoldInterface, Vector3.one, 0.3f).setEaseSpring();
        }
    }
}
