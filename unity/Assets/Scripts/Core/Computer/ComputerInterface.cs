using Core.Computer.PathoNet;
using Framework.Controller;

namespace Core.Computer
{
    public class ComputerInterface : InterfaceController<ComputerInterface>
    {
        private PathoNetInterface _shop;

        public override void Start()
        {
            base.Start();
            _shop = panel.GetComponentInChildren<PathoNetInterface>(true);
            if (_shop != null) _shop.OnBuyCart += ClosePanel;
        }

        private void OnDestroy()
        {
            if (_shop != null) _shop.OnBuyCart -= ClosePanel;
        }
    }
}
