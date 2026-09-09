using Core.Input;
using Core.Scene;
using Framework.Controller;
using UnityEngine;

namespace Core.MainMenu
{
    public class OnBoardingMainMenuController : InterfaceController<OnBoardingMainMenuController>
    {
        public override void OpenPanel()
        {
            base.OpenPanel();
            GamepadNavigation.SelectFirstSelectable(panel);
        }

        public void GoOnOnBoarding()
        {
            this.ClosePanel();
            var scene = SceneDatabase.Instance.GetSceneByName("OnBoarding");
            if (!scene)
            {
                Debug.LogError($"Scene not found in database: 'OnBoarding'");
                return;
            }
            GoOnNewScene(scene);
        }

        public void GoOnGame()
        {
            this.ClosePanel();
            var scene = SceneDatabase.Instance.GetSceneByName("Game");
            if (!scene)
            {
                Debug.LogError($"Scene not found in database: 'Game'");
                return;
            }
            GoOnNewScene(scene);
        }

        private void GoOnNewScene(Scene.Scene scene)
        {
            LeanTween.scale(gameObject, Vector3.zero, 0.6f).setEaseSpring();

            LeanTween.value(gameObject, v => MainMenuController.Instance.sharedMaterial.SetFloat("_MaskSize", v),
                MainMenuController.Instance.sharedMaterial.GetFloat("_MaskSize"), 0f, 0.6f).setEaseSpring();

            LeanTween.value(gameObject, v => MainMenuController.Instance.sharedMaterial.SetVector("_MaskOffset", v),
                MainMenuController.Instance.sharedMaterial.GetVector("_MaskOffset"), new Vector3(0.2f, 0.5f, 0f), 0.6f).setEaseSpring();

            LeanTween.delayedCall(0.4f, () => SceneTransitor.Instance.LoadScene(scene));
        }
    }
}
