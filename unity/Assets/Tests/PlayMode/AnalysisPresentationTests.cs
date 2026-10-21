using System;
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace VirusMania.Tests
{
    public class AnalysisPresentationTests
    {
        private Component _ui;
        private object _requested;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Game.Load("Game");
            Game.Call(Game.Find("Core.Brief.BriefInterface"), "PutNewBrief");
            yield return new WaitForSecondsRealtime(0.6f);
            Game.Call(Game.Find("Core.Timer.TimerController"), "StopTimer");
            _ui = Game.Find("Core.Analysis.AnalysisInterface");
            _requested = Game.Get(Game.Get(Game.Find("Core.Brief.BriefController"), "actualBrief"), "wantedItem");
        }

        private void Show(object item) => Game.Call(_ui, "ShowAnalysis", Game.Hold(item));
        private Image Preview => (Image)Game.Get(_ui, "sampleImage");
        private TMP_Text Label(string name) => (TMP_Text)Game.Get(_ui, name);

        [UnityTest]
        public IEnumerator ChangingVirusUpdatesTheImageNameDescriptionAndRequestStatus()
        {
            var briefs = (IList)Game.Get(Game.Instance("Core.Brief.BriefDatabase"), "Database");
            foreach (var brief in briefs)
            {
                var item = Game.Get(brief, "wantedItem");
                Show(item);
                yield return null;
                Assert.That(Preview.sprite, Is.SameAs(Game.Get(item, "itemIcon")));
                Assert.That(Preview.enabled && Preview.preserveAspect, Is.True);
                Assert.That(Label("diseaseNameText").text, Is.EqualTo(Game.Get(item, "itemName")));
                Assert.That(Label("sampleDescriptionText").text, Is.EqualTo(Game.Get(item, "itemDescription")));
                Assert.That(Label("requestStatusText").text,
                    Is.EqualTo(item == _requested ? "MATCHES CURRENT REQUEST" : "DIFFERENT SAMPLE TO REQUEST"));
            }
        }

        [UnityTest]
        public IEnumerator EmptyThreatProfileShowsAnExplicitStateWithoutInventingMeasurements()
        {
            Show(_requested);
            yield return null;
            Assert.That(Label("emptyThreatText").gameObject.activeInHierarchy, Is.True);
            Assert.That(Label("emptyThreatText").text, Does.Contain("No threat measurements"));
            Assert.That(((GameObject)Game.Get(_ui, "threatListPanel")).activeSelf, Is.False);
            Assert.That(((RectTransform)Game.Get(_ui, "diseaseThreatContainer")).childCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PopulatedProfileUsesItsActualNamesAndSignedValuesAndClearsOnSampleChange()
        {
            var virus = ScriptableObject.CreateInstance(Game.Type("Core.Item.VirusItem"));
            var threatTypes = new ScriptableObject[3];
            try
            {
                Game.Set(virus, "itemName", "Layout test sample");
                Game.Set(virus, "itemIcon", Game.Get(_requested, "itemIcon"));
                var parameters = Array.CreateInstance(Game.Type("Core.Item.ThreatParameter"), 3);
                var levels = new[] { 4, -2, 0 };
                for (int i = 0; i < 3; i++)
                {
                    threatTypes[i] = ScriptableObject.CreateInstance(Game.Type("Core.Threats.ThreatType"));
                    Game.Set(threatTypes[i], "threatTypeName", "Measured property " + i);
                    Game.Set(threatTypes[i], "threatTypeIcon", Game.Get(_requested, "itemIcon"));
                    var parameter = Activator.CreateInstance(Game.Type("Core.Item.ThreatParameter"));
                    Game.Set(parameter, "threatType", threatTypes[i]);
                    Game.Set(parameter, "threatImpact", levels[i]);
                    parameters.SetValue(parameter, i);
                }
                Game.Set(virus, "threatParameters", parameters);
                Show(virus);
                yield return null;
                var content = (RectTransform)Game.Get(_ui, "diseaseThreatContainer");
                Assert.That(content.childCount, Is.EqualTo(3));
                Assert.That(Label("emptyThreatText").gameObject.activeSelf, Is.False);
                var expectedValues = new[] { "+4", "-2", "0" };
                for (int i = 0; i < 3; i++)
                {
                    var row = content.GetChild(i).GetComponent(Game.Type("Core.Analysis.ThreatOfDiseasePrefab"));
                    Assert.That(((TMP_Text)Game.Get(row, "threatNameText")).text, Is.EqualTo("Measured property " + i));
                    Assert.That(((TMP_Text)Game.Get(row, "threatLevelText")).text, Is.EqualTo(expectedValues[i]));
                }
                Show(_requested);
                foreach (Transform child in content)
                    Assert.That(child.gameObject.activeSelf, Is.False, "Deferred destruction must not leave old rows visible.");
                yield return null;
                Assert.That(content.childCount, Is.Zero);
                Assert.That(Label("emptyThreatText").gameObject.activeInHierarchy, Is.True);
            }
            finally
            {
                Object.Destroy(virus);
                foreach (var threat in threatTypes) if (threat != null) Object.Destroy(threat);
            }
        }

        [UnityTest]
        public IEnumerator MissingIconDoesNotReuseThePreviousSampleImage()
        {
            var item = ScriptableObject.CreateInstance(Game.Type("Core.Item.Item"));
            try
            {
                Game.Set(item, "itemName", "Unpictured sample");
                Show(_requested);
                Show(item);
                yield return null;
                Assert.That(Preview.sprite, Is.Null);
                Assert.That(Preview.enabled, Is.False);
                Assert.That(Label("missingImageText").gameObject.activeInHierarchy, Is.True);
                Assert.That(Label("sampleDescriptionText").text, Is.EqualTo("No sample notes recorded."));
            }
            finally { Object.Destroy(item); }
        }

        [UnityTest]
        public IEnumerator EmptySampleClearsThePreviousReportAndClosesTheWindow()
        {
            Show(_requested);
            yield return new WaitForSecondsRealtime(0.5f);
            LogAssert.Expect(LogType.Warning, "Cannot show analysis for an empty item.");
            Game.Call(_ui, "ShowAnalysis", new object[] { null });
            Assert.That(Preview.sprite, Is.Null);
            Assert.That(Label("diseaseNameText").text, Is.Empty);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That((bool)Game.Get(_ui, "IsOpen"), Is.False);
        }

        [UnityTest]
        public IEnumerator ReportTextAndPreviewStayInSeparateColumnsAndExitClosesTheWindow()
        {
            Show(_requested);
            yield return new WaitForSecondsRealtime(0.6f);
            Canvas.ForceUpdateCanvases();
            var nameCorners = new Vector3[4];
            var imageCorners = new Vector3[4];
            Label("diseaseNameText").rectTransform.GetWorldCorners(nameCorners);
            Preview.rectTransform.GetWorldCorners(imageCorners);
            Assert.That(nameCorners[2].x, Is.LessThan(imageCorners[0].x));
            var card = _ui.transform.Find("InnerPanel/AnalysisWindow") as RectTransform;
            Assert.That(card, Is.Not.Null);
            var corners = new Vector3[4];
            card.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(null, corner);
                Assert.That(screen.x, Is.InRange(-1f, Screen.width + 1f));
                Assert.That(screen.y, Is.InRange(-1f, Screen.height + 1f));
            }
            _ui.transform.Find("InnerPanel/AnalysisWindow/Header/ExitButton").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That((bool)Game.Get(_ui, "IsOpen"), Is.False);
        }

        [UnityTest]
        public IEnumerator IllustrationMovesWithUnscaledTimeAndResetsWithoutDrift()
        {
            Show(_requested);
            yield return new WaitForSecondsRealtime(0.7f);
            var presentation = (Behaviour)Game.Get(_ui, "presentation");
            var stage = (RectTransform)Game.Get(presentation, "sampleStage");
            var basePosition = (Vector2)Game.Get(presentation, "_stagePosition");
            try
            {
                Time.timeScale = 0f;
                float low = stage.anchoredPosition.y, high = low;
                float until = Time.realtimeSinceStartup + .6f;
                while (Time.realtimeSinceStartup < until)
                {
                    yield return null;
                    low = Mathf.Min(low, stage.anchoredPosition.y);
                    high = Mathf.Max(high, stage.anchoredPosition.y);
                }
                Assert.That(high - low, Is.GreaterThan(0.1f));
                presentation.enabled = false;
                Assert.That(stage.anchoredPosition, Is.EqualTo(basePosition));
                presentation.enabled = true;
                Show(_requested);
                yield return new WaitForSecondsRealtime(0.7f);
                Assert.That(Mathf.Abs(stage.anchoredPosition.y - basePosition.y), Is.LessThanOrEqualTo(5.1f));
                Assert.That(stage.localScale.x, Is.EqualTo(1f).Within(0.01f));
            }
            finally { Time.timeScale = 1f; }
        }

        [UnityTest]
        public IEnumerator WavySurfaceAndExitHoverUseTheExistingAnimatedUiStyle()
        {
            Show(_requested);
            yield return new WaitForSecondsRealtime(0.6f);
            var card = _ui.transform.Find("InnerPanel/AnalysisWindow");
            var surface = card.GetComponent<Image>().material;
            Assert.That(surface.shader.name, Is.EqualTo("UI/MethilUiWavyBlobOutline"));
            Assert.That(surface.GetFloat("_NoiseAmplitude"), Is.GreaterThan(0f));
            var button = card.Find("Header/ExitButton").GetComponent<Image>();
            var standard = Resources.Load<GameObject>("Prefabs/UserInterface/ModalExitButton").GetComponent<Image>();
            Assert.That(button.sprite, Is.SameAs(standard.sprite));
            Assert.That(button.GetComponentInChildren<TMP_Text>().font, Is.SameAs(standard.GetComponentInChildren<TMP_Text>().font));
            Assert.That(button.GetComponentsInChildren<Image>().Length, Is.EqualTo(standard.GetComponentsInChildren<Image>().Length));
            var animator = button.GetComponent(Game.Type("Core.SFX.UI.UIButtonHover"));
            var shadow = button.GetComponent<Shadow>();
            var initial = shadow.effectDistance;
            Assert.That(initial.sqrMagnitude, Is.GreaterThan(0f));
            try
            {
                Time.timeScale = 0f;
                Game.Call(animator, "OnPointerEnter", new PointerEventData(EventSystem.current));
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(shadow.effectDistance.magnitude, Is.LessThan(0.01f));
                Game.Call(animator, "OnPointerExit", new PointerEventData(EventSystem.current));
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(Vector2.Distance(shadow.effectDistance, initial), Is.LessThan(0.01f));
            }
            finally { Time.timeScale = 1f; }
        }
    }
}
