using UnityEngine;
using UnityEngine.Rendering;
using Framework.Controller;
using UnityEngine.Rendering.Universal;

namespace Core.PostProcess
{
    public class PostProcessController : BaseController<PostProcessController>
    {
        public Volume postProcessVolume;

        private DepthOfField depthOfFieldEffect;
        private float originalFocusDistance = 2f;
        private int dofTweenId = -1;

        private Vignette vignetteEffect;
        private int vignetteTweenId = -1;

        protected override void Awake()
        {
            base.Awake();
            
            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet<DepthOfField>(out depthOfFieldEffect);
                if (depthOfFieldEffect != null)
                {
                    originalFocusDistance = depthOfFieldEffect.focusDistance.value;
                }

                postProcessVolume.profile.TryGet<Vignette>(out vignetteEffect);
                if (vignetteEffect != null)
                {
                    vignetteEffect.active = true;
                    vignetteEffect.intensity.value = 0f;
                }
            }
        }

        public void OnShowPanelPostProcess()
        {
            if (depthOfFieldEffect != null)
            {
                depthOfFieldEffect.active = true;
                if (dofTweenId != -1) LeanTween.cancel(dofTweenId);
                dofTweenId = LeanTween.value(gameObject, depthOfFieldEffect.focusDistance.value, 0f, 0.24f)
                    .setOnUpdate((float val) => { depthOfFieldEffect.focusDistance.value = val; })
                    .setIgnoreTimeScale(true)
                    .id;
            }

            if (vignetteEffect != null)
            {
                if (vignetteTweenId != -1) LeanTween.cancel(vignetteTweenId);
                vignetteTweenId = LeanTween.value(gameObject, vignetteEffect.intensity.value, 0.2f, 0.3f)
                    .setOnUpdate((float val) => { vignetteEffect.intensity.value = val; })
                    .setIgnoreTimeScale(true)
                    .id;
            }
        }

        public void OnHidePanelPostProcess()
        {
            if (depthOfFieldEffect != null)
            {
                if (dofTweenId != -1) LeanTween.cancel(dofTweenId);
                dofTweenId = LeanTween.value(gameObject, depthOfFieldEffect.focusDistance.value, originalFocusDistance, 0.24f)
                    .setOnUpdate((float val) => { depthOfFieldEffect.focusDistance.value = val; })
                    .setOnComplete(() => { depthOfFieldEffect.active = false; })
                    .setIgnoreTimeScale(true)
                    .id;
            }

            if (vignetteEffect != null)
            {
                if (vignetteTweenId != -1) LeanTween.cancel(vignetteTweenId);
                vignetteTweenId = LeanTween.value(gameObject, vignetteEffect.intensity.value, 0f, 0.24f)
                    .setOnUpdate((float val) => { vignetteEffect.intensity.value = val; })
                    .setOnComplete(() => { vignetteEffect.active = false; })
                    .setIgnoreTimeScale(true)
                    .id;
            }
        }
    }
}
