using System;
using System.Collections;
using Core.Scene;
using Core.SFX;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.SplashScreen
{
    public class SplashScreen : MonoBehaviour
    {
        public CanvasGroup parentCanvasGroup;

        public void Start()
        {
            DontDestroyOnLoad(parentCanvasGroup.gameObject);
        }

        public void LoadMainMenu()
        {
            StartCoroutine(LoadMainMenuCoroutine());
        }

        public void LaunchSound()
        {
            SFXController.Instance.PlayUI(SFXDatabase.Instance.splashScreen);
        }

        public IEnumerator LoadMainMenuCoroutine()
        {
            yield return null;
            var mainMenu = SceneDatabase.Instance.GetSceneByName("MainMenu");
            
            AsyncOperation asyncSceneToLoad = SceneManager.LoadSceneAsync(mainMenu.sceneKey);
            asyncSceneToLoad.allowSceneActivation = false; // stop the level from activating
            while (asyncSceneToLoad.progress < 0.9f)
            {
                yield return null;
            } 
            asyncSceneToLoad.allowSceneActivation = true; // this will enter the level now
            yield return asyncSceneToLoad;
            
            LeanTween.alphaCanvas(parentCanvasGroup, 0f, 1f)
                .setEase( LeanTweenType.easeOutQuart )
                .setIgnoreTimeScale(true);
            
            yield return new WaitForSecondsRealtime(1f);
            Destroy(parentCanvasGroup.gameObject);
        }
    }
}
