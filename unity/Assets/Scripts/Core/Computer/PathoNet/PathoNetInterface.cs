using System.Collections.Generic;
using Core.Interaction;
using UnityEngine;
using UnityEngine.UI;
using Core.Item;
using Core.Money;
using Utils;
using TMPro;
using UnityEngine.Events;
using Core.SFX;

namespace Core.Computer.PathoNet
{
    public class PathoNetInterface : MonoBehaviour
    {
        public GameObject pathoBuyableItemPrefab;
        public Transform pathoBuyableItemContainer;
        public Button buyButton;
        public PathoNetItemReceiver pathoItemReceiver;
        public UnityAction OnBuyCart;
        public UnityAction<Item.Item> OnBuyItem;

        public RectTransform noMoneyPanel;
        
        [SerializeField] 
        private bool onlyOnBoarding;

        public TextMeshProUGUI dollarAmountText;

        CanvasGroup noMoneyCanvasGroup;

        protected void Start()
        {
            noMoneyCanvasGroup = noMoneyPanel.GetComponent<CanvasGroup>();
            if (noMoneyCanvasGroup == null)
                noMoneyCanvasGroup = noMoneyPanel.gameObject.AddComponent<CanvasGroup>();

            noMoneyPanel.gameObject.SetActive(false);
            noMoneyCanvasGroup.alpha = 0f;

            foreach (Transform child in pathoBuyableItemContainer)
                Destroy(child.gameObject);
            
            var database = onlyOnBoarding ? ItemDatabase.Instance.buyableOnBoardingItems : ItemDatabase.Instance.buyableItems;
            foreach (var item in database)
            {
                GameObject go = Instantiate(pathoBuyableItemPrefab, pathoBuyableItemContainer);
                var buyableBehaviour = go.GetComponent<PathoNetBuyableItemBehaviour>();
                if (buyableBehaviour != null)
                    buyableBehaviour.Setup(item, this, onlyOnBoarding);
            }
            if (pathoItemReceiver == null)
                pathoItemReceiver = SceneUtils.GetComponentInActiveScene<PathoNetItemReceiver>();
                
            buyButton.onClick.AddListener(BuyCart);
        }

        public void UpdateInterface()
        {
            int total = 0;

            foreach (Transform child in pathoBuyableItemContainer)
            {
                var buyable = child.GetComponent<PathoNetBuyableItemBehaviour>();
                if (buyable != null && buyable.itemData != null)
                    total += buyable.Amount * buyable.itemData.price;
            }

            dollarAmountText.text = "$" + total;
        }

        private void BuyCart()
        {
            int total = 0;
            List<PathoNetBuyableItemBehaviour> itemsToBuy = new List<PathoNetBuyableItemBehaviour>();

            foreach (Transform child in pathoBuyableItemContainer)
            {
                var buyable = child.GetComponent<PathoNetBuyableItemBehaviour>();
                if (buyable != null && buyable.itemData != null && buyable.Amount > 0)
                {
                    itemsToBuy.Add(buyable);
                    total += buyable.Amount * buyable.itemData.price;
                }
            }

            if (MoneyController.Instance != null)
            {
                if (!MoneyController.Instance.CanRemoveMoney(total))
                {
                    TriggerNoMoney();
                    MoneyController.Instance.OnMoneyInsufficient?.Invoke();
                    return;
                }

                MoneyController.Instance.RemoveMoney(total);
            }
            else
            {
                Debug.LogWarning("MoneyController instance is null");
            }

            foreach (PathoNetBuyableItemBehaviour buyable in itemsToBuy)
            {
                buyable.Buy();
            }

            UpdateInterface();
            OnBuyCart?.Invoke();
        }
        
        public void TriggerNoMoney()
        {
            LeanTween.cancel(noMoneyPanel);
            LeanTween.cancel(noMoneyCanvasGroup.gameObject);

            noMoneyPanel.gameObject.SetActive(true);
            noMoneyCanvasGroup.alpha = 1f;
            noMoneyPanel.anchoredPosition = Vector2.zero;

            LeanTween.value(noMoneyPanel.gameObject, 0f, 1f, 0.5f)
                .setOnStart(() =>
                {
                    LeanTween.cancel(noMoneyPanel);
                    LeanTween.cancel(noMoneyCanvasGroup.gameObject);

                    LeanTween.rotateZ(noMoneyPanel.gameObject, 10f, 0.05f).setLoopPingPong(5)
                    .setOnComplete(() =>
                    {
                        LeanTween.delayedCall(1f, () =>
                        {
                            noMoneyPanel.gameObject.SetActive(false);
                            if (SFXDatabase.Instance.noMoneyClip != null)
                                SFXController.Instance.PlayUI(SFXDatabase.Instance.noMoneyClip);
                        });
                    });
                });
        }
    }
}
