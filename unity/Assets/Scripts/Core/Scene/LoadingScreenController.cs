using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.Scene
{
    public class LoadingScreenController : MonoBehaviour
    {
        public Image loadingScreenImage;
        [SerializeField] private Image loadingSlider;
        private bool _sceneIsSwapping;
        [SerializeField] private TextMeshProUGUI loadingText;
        private Coroutine loadingTextRoutine;

        public void StartToLoadScene(int sceneToLoad){
            StartToLoadScene(sceneToLoad, () => {});
        }

        public void StartToLoadScene(int sceneToLoad, Action onEndCallback){
            CanvasGroup canvasGroup = loadingScreenImage.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            if(_sceneIsSwapping == true)
                return;
            DontDestroyOnLoad(this.gameObject);
            StartCoroutine(LoadScene(sceneToLoad, onEndCallback));
        }

        private IEnumerator AnimateLoadingText() {
            string baseText = "Loading";
            int dotCount = 0;
            while (true) {
                dotCount = (dotCount + 1) % 4;
                loadingText.text = baseText + new string('.', dotCount);
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private IEnumerator LoadScene(int sceneToLoad, Action onEndCallback){
            if (loadingSlider != null)
            {
                loadingSlider.material = new Material(loadingSlider.material);
                loadingSlider.material.SetFloat("_InnerFillAmount", 0f);
                loadingSlider.gameObject.SetActive(true);
            }
            _sceneIsSwapping = true;
            float startPosition = loadingScreenImage.rectTransform.position.y;
            CanvasGroup canvasGroup = loadingScreenImage.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            loadingTextRoutine = StartCoroutine(AnimateLoadingText());

            LeanTween.alphaCanvas(canvasGroup, 1f, 1f)
                .setEase( LeanTweenType.easeOutQuart )
                .setIgnoreTimeScale(true);

            yield return new WaitForSecondsRealtime(1f);

            AsyncOperation asyncSceneToLoad = SceneManager.LoadSceneAsync(sceneToLoad);
            asyncSceneToLoad.allowSceneActivation = false;
            while (asyncSceneToLoad.progress < 0.9f)
            {
                if (loadingSlider != null)
                {
                    loadingSlider.material.SetFloat("_InnerFillAmount", asyncSceneToLoad.progress);
                }
                yield return new WaitForEndOfFrame();
            } 
            
            yield return new WaitForSecondsRealtime(0.2f);
            if (loadingSlider != null) loadingSlider.material.SetFloat("_InnerFillAmount", 1f);
            asyncSceneToLoad.allowSceneActivation = true;
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();

            onEndCallback.Invoke();
            yield return new WaitForSeconds(0.2f);

            if (loadingTextRoutine != null) StopCoroutine(loadingTextRoutine);
            loadingText.text = "Loading...";

            if (loadingSlider != null) loadingSlider.gameObject.SetActive(false);

            LeanTween.alphaCanvas(canvasGroup, 0f, 1f)
                .setEase( LeanTweenType.easeOutQuart )
                .setIgnoreTimeScale(true);

            yield return new WaitForSecondsRealtime(1.2f);
            Destroy(this.gameObject);
            _sceneIsSwapping = false;
        }
    }
}
