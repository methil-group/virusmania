using Core.Prefab;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Analysis
{
    public class ThreatOfDiseasePrefab : MonoBehaviour
    {
        public Image threatIconImage;
        public TextMeshProUGUI threatLevelText;
        public TextMeshProUGUI threatNameText;

        public void Setup(Sprite _threatIconImage, int threatLevel, string threatName = null)
        {
            threatIconImage.sprite = _threatIconImage;
            threatLevelText.text = threatLevel > 0 ? $"+{threatLevel}" : threatLevel.ToString();
            if (threatNameText != null) threatNameText.text = threatName ?? string.Empty;
            if (threatLevel > 0)
            {
                threatLevelText.color = new Color(0.1f, 0.34f, 0.12f);
            }
            else
            {
                threatLevelText.color = threatLevel == 0 ? Color.black : new Color(0.55f, 0.08f, 0.07f);
            }
        }
    }
}
