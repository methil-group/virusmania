using System.Collections.Generic;
using Core.End;
using Core.Item.Holder;
using Core.Money;
using Framework.Controller;
using Framework.Extensions;
using UnityEngine;

namespace Core.Brief
{
    public class BriefController : BaseController<BriefController>
    {
        [SerializeField]
        BriefInterface briefInterface;
        [SerializeField]
        public Brief actualBrief;
        
        [SerializeField]
        private bool onlyOnBoarding = false;
        
        private List<Brief> _briefsDone =  new List<Brief>();

        public void Start()
        {
            NewBrief();
        }
        
        public void NewBrief()
        {
            actualBrief = null;

            var onlyNotOnBoarding = BriefDatabase.Instance.Database
                .FindAll(b => b.onBoarding == onlyOnBoarding && !_briefsDone.Contains(b));

            if (onlyNotOnBoarding.Count == 0)
            {
                EndInterface.Instance.OpenWinPanel();
                return;
            }

            onlyNotOnBoarding.Sort((a, b) => a.difficulty.CompareTo(b.difficulty));
            Brief newBrief = onlyNotOnBoarding[0];

            briefInterface.SetupNewBriefShow(newBrief);
        }


        public bool CanCompleteBrief(HoldItem itemToValidate)
        {
            if (itemToValidate?.Item == null) return false;

            if (actualBrief != null && actualBrief.wantedItem == itemToValidate.Item)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public bool TryToCompleteBrief(HoldItem itemToValidate)
        {
            if (itemToValidate?.Item == null) return false;

            if (actualBrief != null && actualBrief.wantedItem == itemToValidate.Item)
            {
                MoneyController.Instance.AddMoney(actualBrief.moneyGiven);
                _briefsDone.Add(actualBrief);
                actualBrief = null;
                NewBrief();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
