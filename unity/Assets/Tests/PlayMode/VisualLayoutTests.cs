using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace VirusMania.Tests
{
    public class VisualLayoutTests
    {
        private static void AssertFits(TMP_Text title, TMP_Text body)
        {
            title.ForceMeshUpdate();
            body.ForceMeshUpdate();
            Assert.That(title.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(title.preferredHeight), title.name);
            Assert.That(body.rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(body.preferredHeight), body.name);
            var titleCorners = new Vector3[4];
            var bodyCorners = new Vector3[4];
            title.rectTransform.GetWorldCorners(titleCorners);
            body.rectTransform.GetWorldCorners(bodyCorners);
            Assert.That(titleCorners[0].y, Is.GreaterThan(bodyCorners[1].y), "Title must leave space above the description.");
        }

        [UnityTest]
        public IEnumerator TutorialHintsHaveRoomForEveryStep()
        {
            yield return Game.Load("OnBoarding");
            var controller = Game.Find("Core.OnBoarding.OnBoardingController");
            var ui = Game.Find("Core.OnBoarding.OnBoardingInterface");
            var body = (TMP_Text)Game.Get(ui, "onBoardingHintText");
            var title = body.transform.parent.Find("HintTitle").GetComponent<TMP_Text>();
            var boards = (IList)Game.Get(Game.Instance("Core.OnBoarding.OnBoardingDatabase"), "OnBoardings");
            for (int i = 0; i < boards.Count; i++)
            {
                Game.Set(controller, "actualOnBoardingIndex", i);
                Game.Call(ui, "ShowActualBoard");
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertFits(title, body);
                var corners = new Vector3[4];
                ((RectTransform)body.transform.parent).GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var screen = RectTransformUtility.WorldToScreenPoint(null, corner);
                    Assert.That(screen.x, Is.InRange(-1f, Screen.width + 1f));
                    Assert.That(screen.y, Is.InRange(-1f, Screen.height + 1f));
                }
            }
        }

        [UnityTest]
        public IEnumerator MissionBriefsHaveRoomForTheirTitleAndDescription()
        {
            yield return Game.Load("Game");
            var ui = Game.Find("Core.Brief.BriefInterface");
            var briefs = (IList)Game.Get(Game.Instance("Core.Brief.BriefDatabase"), "Database");
            foreach (var brief in briefs)
            {
                Game.Call(ui, "SetupNewBriefShow", brief);
                yield return new WaitForSecondsRealtime(0.5f);
                Canvas.ForceUpdateCanvases();
                AssertFits((TMP_Text)Game.Get(ui, "briefNameText"), (TMP_Text)Game.Get(ui, "briefDescriptionText"));
            }
        }

        [UnityTest]
        public IEnumerator OutlineMasksAndFillsUseTheSameStencilReference()
        {
            yield return Game.Load("Game");
            var type = Type.GetType("Outline, Assembly-CSharp-firstpass", true);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var maskField = type.GetField("outlineMaskMaterial", flags);
            var fillField = type.GetField("outlineFillMaterial", flags);
            var outlines = Object.FindObjectsByType(type, FindObjectsInactive.Exclude);
            Assert.That(outlines.Length, Is.GreaterThan(0));
            foreach (var outline in outlines)
            {
                var mask = (Material)maskField.GetValue(outline);
                var fill = (Material)fillField.GetValue(outline);
                Assert.That(mask.GetFloat("_ZRef"), Is.EqualTo(fill.GetFloat("_ZRef")), outline.name);
            }
        }
    }
}
