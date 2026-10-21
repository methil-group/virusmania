using Framework.Controller;
using TMPro;
using UnityEngine;

namespace Core.OnBoarding
{
    public class OnBoardingInterface : BaseController<OnBoardingInterface>
    {
        public TextMeshProUGUI onBoardingDescriptionText;
        public TextMeshProUGUI onBoardingHintText;
        
        public void ShowActualBoard()
        {
            OnBoardingData data = OnBoardingController.Instance.GetActualOnBoarding();
            if (data == null) return;
            
            onBoardingDescriptionText.text = data.onBoardingDescription;
            onBoardingHintText.text = data.onBoardingHint;
        }

        public void ShowRecoveryHint(string itemName)
        {
            ShowActualBoard();
            onBoardingHintText.text += "\n\n" + itemName + " has been replaced in the cart. Keep experimenting!";
        }
    }
}
