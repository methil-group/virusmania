using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Money
{
    public class MoneyInterface : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI moneyText;

        private void Start()
        {
            if (MoneyController.Instance != null)
            {
                // We don't care about amount, it's just for the callback of the money controller
                UpdateMoneyText(0);
                MoneyController.Instance.OnMoneyAdded += UpdateMoneyText;
                MoneyController.Instance.OnMoneyRemoved += UpdateMoneyText;
            }
        }

        private void OnDisable()
        {
            if (MoneyController.Instance != null)
            {
                MoneyController.Instance.OnMoneyAdded -= UpdateMoneyText;
                MoneyController.Instance.OnMoneyRemoved -= UpdateMoneyText;
            }
        }

        private void UpdateMoneyText(int amount)
        {
            if (MoneyController.Instance != null && moneyText != null)
            {
                moneyText.text = MoneyController.Instance.money.ToString() + " $";
            }
        }
    }
}
