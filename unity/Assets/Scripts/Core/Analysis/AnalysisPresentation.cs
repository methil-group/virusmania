using UnityEngine;

namespace Core.Analysis
{
    // Only the illustration and entrance move; report text stays still after appearing.
    public class AnalysisPresentation : MonoBehaviour
    {
        [SerializeField] private RectTransform sampleStage;
        [SerializeField] private CanvasGroup report;
        [SerializeField] private RectTransform requestBadge;

        private Vector2 _stagePosition;
        private Vector3 _stageScale;
        private Quaternion _stageRotation;
        private Vector3 _badgeScale;
        private float _presentedAt;
        private bool _initialized;

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _stagePosition = sampleStage.anchoredPosition;
            _stageScale = sampleStage.localScale;
            _stageRotation = sampleStage.localRotation;
            _badgeScale = requestBadge.localScale;
        }

        public void Present()
        {
            Initialize();
            _presentedAt = Time.unscaledTime;
            report.alpha = 0f;
            Apply(0f);
        }

        private void LateUpdate()
        {
            if (!_initialized || !sampleStage.gameObject.activeInHierarchy) return;
            Apply(Time.unscaledTime - _presentedAt);
        }

        private void Apply(float elapsed)
        {
            float entrance = Mathf.Clamp01((elapsed - 0.08f) / 0.45f);
            float shifted = entrance - 1f;
            float pop = 1f + 2.4f * shifted * shifted * shifted + 1.4f * shifted * shifted;
            float bob = Mathf.Sin(elapsed * 1.8f) * 5f;
            sampleStage.anchoredPosition = _stagePosition + new Vector2(0f, bob + (1f - entrance) * -18f);
            sampleStage.localScale = _stageScale * Mathf.LerpUnclamped(0.8f, 1f, pop);
            sampleStage.localRotation = _stageRotation * Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 1.35f) * 1.8f);
            report.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - 0.12f) / 0.23f));
            requestBadge.localScale = Vector3.Scale(_badgeScale,
                new Vector3(1f, Mathf.Lerp(0.9f, 1f, report.alpha), 1f));
        }

        private void OnDisable()
        {
            if (!_initialized) return;
            sampleStage.anchoredPosition = _stagePosition;
            sampleStage.localScale = _stageScale;
            sampleStage.localRotation = _stageRotation;
            requestBadge.localScale = _badgeScale;
            report.alpha = 1f;
        }
    }
}
