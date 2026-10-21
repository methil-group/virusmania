using System.Collections.Generic;
using System.Linq;
using Core.Item;
using Core.Item.Holder;
using Core.Brief;
using Core.Prefab;
using Framework.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Analysis
{
    public class AnalysisInterface : InterfaceController<AnalysisInterface>
    {
        [Header("Disease Text")] 
        public TextMeshProUGUI diseaseNameText;
        public RectTransform diseaseThreatContainer;
        
        [Header("Sample report")]
        [SerializeField] private Image sampleImage;
        [SerializeField] private TextMeshProUGUI sampleDescriptionText;
        [SerializeField] private TextMeshProUGUI requestStatusText;
        [SerializeField] private TextMeshProUGUI emptyThreatText;
        [SerializeField] private TextMeshProUGUI missingImageText;
        [SerializeField] private GameObject threatListPanel;
        [SerializeField] private AnalysisPresentation presentation;
        
        public void ShowAnalysis(HoldItem holdItemAnalyzed)
        {
            if (holdItemAnalyzed?.Item == null)
            {
                ClearSample();
                if (IsOpen) ClosePanel();
                Debug.LogWarning("Cannot show analysis for an empty item.");
                return;
            }

            var item = holdItemAnalyzed.Item;
            diseaseNameText.text = item.itemName;
            if (sampleImage != null)
            {
                sampleImage.sprite = item.itemIcon;
                sampleImage.enabled = item.itemIcon != null;
                sampleImage.preserveAspect = true;
            }
            if (missingImageText != null) missingImageText.gameObject.SetActive(item.itemIcon == null);
            if (sampleDescriptionText != null)
                sampleDescriptionText.text = string.IsNullOrWhiteSpace(item.itemDescription)
                    ? "No sample notes recorded." : item.itemDescription;
            UpdateRequestStatus(item);
            ClearThreatRows();
            int count = 0;

            if (holdItemAnalyzed is HoldVirusItem holdVirusItem && holdVirusItem.Item is VirusItem holdVirusItemData)
            {
                IEnumerable<ThreatParameter> threatImpacts = holdVirusItemData.threatParameters ?? Enumerable.Empty<ThreatParameter>();
                foreach (var impact in threatImpacts)
                {
                    if (impact?.threatType == null || impact.threatType.threatTypeIcon == null)
                    {
                        Debug.LogWarning($"Ignoring incomplete threat data on {holdVirusItemData.itemName}.");
                        continue;
                    }

                    var threatPrefab = Instantiate(PrefabDatabase.Instance.analysisThreatPrefab, diseaseThreatContainer);
                    threatPrefab.GetComponent<ThreatOfDiseasePrefab>().Setup(
                        impact.threatType.threatTypeIcon, impact.threatImpact, impact.threatType.threatTypeName);
                    count++;
                }
            }
            if (emptyThreatText != null)
            {
                emptyThreatText.text = "No threat measurements recorded\nfor this sample.";
                emptyThreatText.gameObject.SetActive(count == 0);
            }
            if (threatListPanel != null) threatListPanel.SetActive(count > 0);
            OpenPanel();
            LayoutRebuilder.ForceRebuildLayoutImmediate(diseaseThreatContainer);
            if (presentation != null) presentation.Present();
        }

        private void UpdateRequestStatus(Item.Item item)
        {
            if (requestStatusText == null) return;
            var brief = BriefController.Instance != null ? BriefController.Instance.actualBrief : null;
            bool matches = brief != null && brief.wantedItem == item;
            requestStatusText.text = brief == null ? "NO ACTIVE REQUEST"
                : matches ? "MATCHES CURRENT REQUEST" : "DIFFERENT SAMPLE TO REQUEST";
            requestStatusText.color = matches ? new Color(0.1f, 0.34f, 0.12f)
                : brief == null ? Color.black : new Color(0.55f, 0.08f, 0.07f);
        }

        private void ClearThreatRows()
        {
            foreach (Transform child in diseaseThreatContainer)
            {
                // Destroy is deferred; hide old rows immediately when changing samples.
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private void ClearSample()
        {
            diseaseNameText.text = string.Empty;
            if (sampleImage != null) { sampleImage.sprite = null; sampleImage.enabled = false; }
            if (sampleDescriptionText != null) sampleDescriptionText.text = string.Empty;
            if (requestStatusText != null) requestStatusText.text = string.Empty;
            if (emptyThreatText != null) emptyThreatText.gameObject.SetActive(false);
            if (missingImageText != null) missingImageText.gameObject.SetActive(false);
            if (threatListPanel != null) threatListPanel.SetActive(false);
            ClearThreatRows();
        }
    }
}
