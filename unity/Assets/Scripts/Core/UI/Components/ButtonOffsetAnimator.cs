using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI
{
    public class ButtonOffsetAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Material _targetMaterial;
        public float duration = 0.2f;
        public LeanTweenType easeType = LeanTweenType.easeOutCubic;
        public float pressDarken = 0.7f;

        private float _originalOffsetX;
        private float _originalOffsetY;
        private int _offsetX;
        private int _offsetY;

        void Start()
        {
            var graphic = GetComponent<Graphic>();
            if (graphic == null || graphic.material == null) return;
            string prefix = graphic.material.HasProperty("_BorderOffsetX") ? "_BorderOffset" : "_OutlineOffset";
            _offsetX = Shader.PropertyToID(prefix + "X");
            _offsetY = Shader.PropertyToID(prefix + "Y");
            if (!graphic.material.HasProperty(_offsetX) || !graphic.material.HasProperty(_offsetY))
            {
                enabled = false;
                return;
            }
            _targetMaterial = new Material(graphic.material);
            graphic.material = _targetMaterial;
            _originalOffsetX = _targetMaterial.GetFloat(_offsetX);
            _originalOffsetY = _targetMaterial.GetFloat(_offsetY);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AnimateOffsets(0f, 0f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateOffsets(_originalOffsetX, _originalOffsetY);
        }

        private void AnimateOffsets(float targetX, float targetY)
        {
            if (_targetMaterial == null) return;
            LeanTween.cancel(gameObject);
            float currentX = _targetMaterial.GetFloat(_offsetX);
            LeanTween.value(gameObject, currentX, targetX, duration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true)
                     .setOnUpdate(val => _targetMaterial.SetFloat(_offsetX, val));

            float currentY = _targetMaterial.GetFloat(_offsetY);
            LeanTween.value(gameObject, currentY, targetY, duration)
                     .setEase(easeType)
                     .setIgnoreTimeScale(true)
                     .setOnUpdate(val => _targetMaterial.SetFloat(_offsetY, val));
        }

        private void OnDisable()
        {
            LeanTween.cancel(gameObject);
            if (_targetMaterial == null) return;
            _targetMaterial.SetFloat(_offsetX, _originalOffsetX);
            _targetMaterial.SetFloat(_offsetY, _originalOffsetY);
        }

        private void OnDestroy()
        {
            if (_targetMaterial != null) Destroy(_targetMaterial);
        }
    }
}
