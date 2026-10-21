using Core.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Core.Player
{
    // Built from the active input bindings so keyboard rebinds and gamepads share the same hints.
    public sealed class PlayerInteractionPrompt : MonoBehaviour
    {
        private PlayerInteraction _interaction;
        private PlayerController _player;
        private GameObject _canvas;
        private GameObject _panel;
        private TextMeshProUGUI _label;

        public void Setup(PlayerInteraction interaction, PlayerController player)
        {
            _interaction = interaction;
            _player = player;
            _canvas = new GameObject("InteractionHints", typeof(Canvas), typeof(CanvasScaler));
            var canvas = _canvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = _canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _panel = new GameObject("Action", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_canvas.transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 210f);
            rect.sizeDelta = new Vector2(720f, 58f);
            var background = _panel.GetComponent<Image>();
            background.color = new Color(0.05f, 0.09f, 0.12f, 0.9f);
            background.raycastTarget = false;

            var text = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(_panel.transform, false);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 4f);
            textRect.offsetMax = new Vector2(-16f, -4f);
            _label = text.GetComponent<TextMeshProUGUI>();
            if (Core.Item.ItemHoldInterface.Instance != null && Core.Item.ItemHoldInterface.Instance.holdItemName != null)
                _label.font = Core.Item.ItemHoldInterface.Instance.holdItemName.font;
            _label.fontSize = 24f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Color.white;
            _label.raycastTarget = false;
            _panel.SetActive(false);
        }

        private void LateUpdate()
        {
            var target = _interaction?.CurrentTarget;
            if (_panel == null) return;
            if (target == null || InputDatabase.Instance.GameplayBlocked)
            {
                _panel.SetActive(false);
                return;
            }
            string tap = target.GetInteractionHint(_player);
            string hold = target.GetHoldInteractionHint(_player);
            string hint = string.IsNullOrEmpty(tap) ? string.Empty :
                GetBindingLabel(InputDatabase.Instance.interactionAction) + "  " + tap;
            if (!string.IsNullOrEmpty(hold))
                hint += (hint.Length > 0 ? "    /    " : string.Empty) + "Hold " +
                        GetBindingLabel(InputDatabase.Instance.interactionHoldAction) + "  " + hold;
            if (_label.text != hint) _label.text = hint;
            _panel.SetActive(hint.Length > 0);
        }

        public static string GetBindingLabel(InputActionReference reference)
        {
            bool gamepad = Gamepad.current != null &&
                (Keyboard.current == null || Gamepad.current.lastUpdateTime > Keyboard.current.lastUpdateTime);
            return reference.action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions,
                group: gamepad ? "Gamepad" : "Keyboard&Mouse");
        }

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas);
        }
    }
}
