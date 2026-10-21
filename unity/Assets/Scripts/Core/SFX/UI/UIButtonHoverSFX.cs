using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.SFX.UI
{
    public class UIButtonHover : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Shadow shadow;
        private Vector2 originalDistance;
        private LTDescr tween;

        private void Awake()
        {
            if (!shadow) shadow = GetComponent<Shadow>();
            if (shadow) originalDistance = shadow.effectDistance;
        }

        public void OnDisable()
        {
            LeanTween.cancel(gameObject, false);
            if (shadow) shadow.effectDistance = originalDistance;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SFXController.Instance.PlayUI(SFXDatabase.Instance.popUiClip);

            if (shadow)
            {
                LeanTween.cancel(gameObject, false);
                tween = LeanTween.value(gameObject, shadow.effectDistance, Vector2.zero, 0.15f)
                    .setEaseOutQuad()
                    .setIgnoreTimeScale(true)
                    .setOnUpdate((Vector2 v) => shadow.effectDistance = v);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (shadow)
            {
                LeanTween.cancel(gameObject, false);
                tween = LeanTween.value(gameObject, shadow.effectDistance, originalDistance, 0.2f)
                    .setEaseOutQuad()
                    .setIgnoreTimeScale(true)
                    .setOnUpdate((Vector2 v) => shadow.effectDistance = v);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SFXController.Instance.PlayUI(SFXDatabase.Instance.clickUiClip);
        }
    }
}
