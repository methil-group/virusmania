using System;
using Framework.Controller;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Scene
{
    public class SceneTransitor : BaseController<SceneTransitor>
    {
        public UnityAction OnLoadNewScene;
        public UnityAction OnEndLoadNewScene;
    
        public GameObject loadingScreen;

        private LoadingScreenController CreateLoadingScreen(Scene sceneToLoad)
        {
            if (sceneToLoad == null)
            {
                Debug.LogError("Cannot load a null scene.");
                return null;
            }

            if (loadingScreen == null)
            {
                Debug.LogError("Cannot load a scene because the loading screen prefab is missing.");
                return null;
            }

            var loadingScreenObject = GameObject.Instantiate(loadingScreen);
            var loadingScreenController = loadingScreenObject.GetComponent<LoadingScreenController>();
            if (loadingScreenController == null)
            {
                Debug.LogError("The loading screen prefab is missing a LoadingScreenController component.");
                GameObject.Destroy(loadingScreenObject);
            }

            return loadingScreenController;
        }
    
        public void LoadScene(Scene sceneToLoad){
            var loadingScreenController = CreateLoadingScreen(sceneToLoad);
            if (loadingScreenController == null) return;

            OnLoadNewScene?.Invoke();
            loadingScreenController.StartToLoadScene(sceneToLoad.sceneKey, () =>
                {
                    OnEndLoadNewScene?.Invoke();
                });
        }
    
        public void LoadScene(Scene sceneToLoad, Action onEndCallback){
            var loadingScreenController = CreateLoadingScreen(sceneToLoad);
            if (loadingScreenController == null) return;

            OnLoadNewScene?.Invoke();
            loadingScreenController.StartToLoadScene(sceneToLoad.sceneKey, () =>
                {
                    onEndCallback?.Invoke();
                    OnEndLoadNewScene?.Invoke();
                });
        }
    }
}
