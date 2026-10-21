using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace VirusMania.Tests
{
    public class VisualCompositionTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Game.Load("Game");
            Game.Call(Game.Find("Core.Brief.BriefInterface"), "PutNewBrief");
            yield return new WaitForSecondsRealtime(.6f);
            Game.Call(Game.Find("Core.Timer.TimerController"), "StopTimer");
        }

        [UnityTest]
        public IEnumerator WindowTitlesAndExitButtonsHaveRoomAndCloseTheirOwnWindow()
        {
            string[] types = { "Core.Analysis.AnalysisInterface", "Core.Computer.ComputerInterface", "Core.MergeLibrary.MergeLibraryInterface" };
            foreach (var type in types)
            {
                var ui = Game.Find(type);
                if (type.Contains("Analysis"))
                {
                    var item = Game.Get(Game.Get(Game.Find("Core.Brief.BriefController"), "actualBrief"), "wantedItem");
                    Game.Call(ui, "ShowAnalysis", Game.Hold(item));
                }
                else Game.Call(ui, "OpenPanel");
                yield return new WaitForSecondsRealtime(.65f);
                Assert.That((bool)Game.Get(ui, "IsOpen"), Is.True, type);
                var header = System.Array.Find(ui.GetComponentsInChildren<RectTransform>(), t => t.name == "Header");
                Assert.That(header, Is.Not.Null, type);
                var title = header.Find("Title").GetComponent<TMP_Text>();
                var button = header.Find("ExitButton").GetComponent<Button>();
                title.ForceMeshUpdate();
                var titleLeft = title.transform.TransformPoint(title.textBounds.min);
                var titleRight = title.transform.TransformPoint(title.textBounds.max);
                var buttonRect = (RectTransform)button.transform;
                var buttonCorners = new Vector3[4]; buttonRect.GetWorldCorners(buttonCorners);
                Assert.That(titleRight.x + 10f, Is.LessThan(buttonCorners[0].x), type + " title must not crowd Exit");
                float titleCenter = title.transform.TransformPoint(title.textBounds.center).y;
                float buttonCenter = buttonRect.TransformPoint(buttonRect.rect.center).y;
                Assert.That(buttonCenter, Is.InRange(Mathf.Min(titleLeft.y, titleRight.y) - 8f,
                    Mathf.Max(titleLeft.y, titleRight.y) + 8f), type + " Exit stays within the title band");
                Assert.That(titleLeft.x, Is.GreaterThanOrEqualTo(0f), type);
                Assert.That(titleRight.y, Is.LessThanOrEqualTo(Screen.height), type);
                var backdrop = (Image)Game.Get(ui, "blackPanel");
                Assert.That(backdrop, Is.Not.Null, type + " needs an assigned backdrop");
                Assert.That(backdrop.gameObject.activeInHierarchy, Is.True, type);
                Assert.That(backdrop.color.a, Is.EqualTo(.6f).Within(.01f), type);
                Assert.That(backdrop.color.r + backdrop.color.g + backdrop.color.b, Is.EqualTo(0f).Within(.01f), type);
                Assert.That(backdrop.raycastTarget, Is.True, type + " must intercept clicks behind the window");
                var backdropCorners = new Vector3[4]; backdrop.rectTransform.GetWorldCorners(backdropCorners);
                Assert.That(backdropCorners[0].x, Is.LessThanOrEqualTo(1f), type);
                Assert.That(backdropCorners[0].y, Is.LessThanOrEqualTo(1f), type);
                Assert.That(backdropCorners[2].x, Is.GreaterThanOrEqualTo(Screen.width - 1f), type);
                Assert.That(backdropCorners[2].y, Is.GreaterThanOrEqualTo(Screen.height - 1f), type);
                if (type.Contains("Analysis"))
                {
                    var card = (RectTransform)header.parent;
                    var cardCorners = new Vector3[4]; card.GetWorldCorners(cardCorners);
                    var canvas = title.GetComponentInParent<Canvas>();
                    Assert.That(Mathf.Abs(titleCenter - cardCorners[1].y),
                        Is.LessThan(title.fontSize * canvas.scaleFactor * .75f), "The title stays attached to the panel edge");
                    float padding = card.InverseTransformPoint(titleLeft).x - card.rect.xMin;
                    Assert.That(padding, Is.GreaterThanOrEqualTo(100f), "Visible left padding");
                }
                button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(.5f);
                Assert.That((bool)Game.Get(ui, "IsOpen"), Is.False, type + " Exit wiring");
                Assert.That(backdrop.color.a, Is.EqualTo(0f).Within(.01f), type);
                Assert.That(backdrop.raycastTarget, Is.False, type);
            }
        }

        [UnityTest]
        public IEnumerator RecipeListOpensAtTheTopAndCanReachTheLastRecipeWithoutCoveringTheHeader()
        {
            var ui = Game.Find("Core.MergeLibrary.MergeLibraryInterface");
            Game.Call(ui, "OpenPanel");
            yield return new WaitForSecondsRealtime(.65f);
            var scroll = ui.GetComponentInChildren<ScrollRect>();
            Assert.That(scroll.viewport, Is.Not.Null);
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(.01f));
            var recipes = (RectTransform)Game.Get(ui, "mergeRecipeTransform");
            AssertInside(scroll.viewport, (RectTransform)recipes.GetChild(0));
            scroll.verticalNormalizedPosition = 0f;
            yield return null;
            var cooking = (RectTransform)Game.Get(ui, "boilingRecipeTransform");
            AssertInside(scroll.viewport, (RectTransform)cooking.GetChild(cooking.childCount - 1));
            Game.Call(ui, "ClosePanel");
            yield return new WaitForSecondsRealtime(.5f);
            Game.Call(ui, "OpenPanel");
            yield return new WaitForSecondsRealtime(.65f);
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(.01f));
            AssertInside(scroll.viewport, (RectTransform)recipes.GetChild(0));
        }

        private static void AssertInside(RectTransform viewport, RectTransform row)
        {
            var corners = new Vector3[4]; row.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var local = viewport.InverseTransformPoint(corner);
                Assert.That(local.y, Is.InRange(viewport.rect.yMin - 1f, viewport.rect.yMax + 1f), row.name);
            }
        }
    }
}
