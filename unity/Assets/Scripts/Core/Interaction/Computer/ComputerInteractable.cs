using Core.Computer;
using Core.Player;
using Core.Utils;
using UnityEngine;

namespace Core.Interaction
{
    public class ComputerInteractable : Interactable
    {
        public override string GetInteractionHint(PlayerController playerController) => "Open shop";
        public Light computerLight;
        public Renderer computerScreenRenderer;
        private Material _computerScreenMaterial;
        private bool _isLightAnimating = false;

        public override void Awake()
        {
            base.Awake();
            if (computerLight != null)
                computerLight.intensity = 0f;

            computerScreenRenderer.material = new Material(computerScreenRenderer.material);
            _computerScreenMaterial =  computerScreenRenderer.material;
            _computerScreenMaterial.SetVector("_TurnWhiteEffect", new Vector2(0f, 0.05f));
            
            InInteraction += () =>
            {
                if (computerLight != null)
                {
                    LeanTween.cancel(computerLight.gameObject);
                    LeanTween.value(computerLight.gameObject, computerLight.intensity, 2f, 0.5f)
                        .setOnUpdate((float val) => computerLight.intensity = val);
                    if (_isLightAnimating == false)
                    {
                        LeanTween.cancel(this.gameObject);
                        ScreenLightUtils.AnimateShaderOnInteraction(this.gameObject, _computerScreenMaterial);
                    }
                    _isLightAnimating = true;
                }
            };

            OutInteraction += () =>
            {
                if (computerLight != null)
                {
                    LeanTween.cancel(computerLight.gameObject);
                    LeanTween.value(computerLight.gameObject, computerLight.intensity, 0f, 0.5f)
                        .setOnUpdate((float val) => computerLight.intensity = val);

                    _isLightAnimating = false;
                    LeanTween.cancel(this.gameObject);
                    ScreenLightUtils.AnimateShaderOutInteraction(this.gameObject, _computerScreenMaterial);
                }
            };
        }

        public override void Interact(PlayerController playerController)
        {
            if(ComputerInterface.Instance != null)
                ComputerInterface.Instance.OpenPanel();
            else
                Debug.LogWarning("Computer Interactor does not exist <- Instantiate it maybe ?");
        }

        public override void InteractHold(PlayerController playerController)
        {
            
        }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            Vector3 titlePos = transform.position + Vector3.up * 3f;
            GUIStyle titleStyle = new GUIStyle
            {
                normal = new GUIStyleState { textColor = new Color(0.5960785f, 0.9843138f, 0.5960785f, 1f) },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            UnityEditor.Handles.Label(titlePos, gameObject.name, titleStyle);
        }
#endif
    }
}
