using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace VirusMania.Tests
{
    // The game uses Unity's predefined Assembly-CSharp. Reflection keeps the tests isolated
    // without reorganizing the game's assemblies or its third-party plugins just for testing.
    internal static class Game
    {
        internal static Type Type(string name) => System.Type.GetType(name + ", Assembly-CSharp", true);
        internal static Component Find(string name) => (Component)Object.FindFirstObjectByType(Type(name));
        internal static object Instance(string name) => Type(name).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
        internal static object Get(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) return field.GetValue(target);
                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null) return property.GetValue(target);
            }
            throw new MissingMemberException(target.GetType().Name, name);
        }
        internal static void Set(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            throw new MissingMemberException(target.GetType().Name, name);
        }
        internal static object Call(object target, string name, params object[] arguments)
        {
            var method = target.GetType().GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(target, arguments);
        }
        internal static object PlayerInteraction()
        {
            foreach (var module in (IEnumerable)Get(Find("Core.Player.PlayerController"), "updatables"))
                if (module.GetType().Name == "PlayerInteraction") return module;
            throw new InvalidOperationException("Player interaction module is missing.");
        }
        internal static object Hold(object item) => Call(item, "GetHoldItem");
        internal static IList Items(object holder) => (IList)Get(holder, "HoldingItems");
        internal static IEnumerator Load(string scene)
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            yield return null;
        }
    }

    public class GameplayReliabilityTests
    {
        private object _input;
        private InputActionReference _tap;
        private InputActionReference _hold;
        private InputActionReference _move;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Game.Load("OnBoarding");
            _input = Game.Instance("Core.Input.InputDatabase");
            _tap = (InputActionReference)Game.Get(_input, "interactionAction");
            _hold = (InputActionReference)Game.Get(_input, "interactionHoldAction");
            _move = (InputActionReference)Game.Get(_input, "moveAction");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ClosingOneOwnerDoesNotUnlockAnotherWindow()
        {
            var first = new GameObject("First input owner");
            var second = new GameObject("Second input owner");
            try
            {
                Game.Call(_input, "SetGameplayBlocked", first, true);
                Game.Call(_input, "SetGameplayBlocked", first, true);
                Game.Call(_input, "SetGameplayBlocked", second, true);
                Game.Call(_input, "SetGameplayBlocked", first, false);
                Assert.That(_move.action.enabled, Is.False);
                Assert.That(_tap.action.enabled, Is.False);
                Assert.That(_hold.action.enabled, Is.False);
                Game.Call(_input, "SetGameplayBlocked", second, false);
                Assert.That(_move.action.enabled && _tap.action.enabled && _hold.action.enabled, Is.True);
            }
            finally
            {
                Game.Call(_input, "SetGameplayBlocked", first, false);
                Game.Call(_input, "SetGameplayBlocked", second, false);
                Object.Destroy(first);
                Object.Destroy(second);
            }
        }

        [UnityTest]
        public IEnumerator RecipesBlockActionsUntilClosingAnimationEnds()
        {
            var library = Game.Find("Core.MergeLibrary.MergeLibraryInterface");
            Game.Call(library, "OpenPanel");
            Assert.That((bool)Game.Get(library, "IsOpen"), Is.True);
            Assert.That(_move.action.enabled || _tap.action.enabled || _hold.action.enabled, Is.False);
            Game.Call(Game.Find("Core.UI.UserInterfaceController"), "CallPauseMenu");
            Assert.That(_move.action.enabled, Is.False, "Closing animation must retain the lock.");
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That((bool)Game.Get(library, "IsOpen"), Is.False);
            Assert.That(_move.action.enabled, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator PauseClosesWithUnscaledTimeAndReleasesItsLock()
        {
            var pause = Game.Find("Core.Pause.PauseMenu");
            Game.Call(pause, "OpenPanel");
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(_tap.action.enabled, Is.False);
            Game.Call(pause, "ClosePanel");
            Game.Call(pause, "ClosePanel");
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_move.action.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator PauseOverBriefCannotReleaseTheBriefLock()
        {
            yield return Game.Load("Game");
            var brief = Game.Find("Core.Brief.BriefInterface");
            Assert.That((bool)Game.Get(brief, "IsBriefPending"), Is.True);
            var pause = Game.Find("Core.Pause.PauseMenu");
            Game.Call(pause, "OpenPanel");
            Game.Call(pause, "ClosePanel");
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(_move.action.enabled, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Game.Call(brief, "PutNewBrief");
            Game.Call(brief, "PutNewBrief");
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(_move.action.enabled, Is.True);
            Assert.That(Game.Get(Game.Find("Core.Brief.BriefController"), "actualBrief"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator AnalysisUsesTheSameBackActionAsOtherWindows()
        {
            yield return Game.Load("Game");
            Game.Call(Game.Find("Core.Brief.BriefInterface"), "PutNewBrief");
            yield return new WaitForSecondsRealtime(0.6f);
            var analysis = Game.Find("Core.Analysis.AnalysisInterface");
            Game.Call(analysis, "OpenPanel");
            Game.Call(Game.Find("Core.UI.UserInterfaceController"), "CallPauseMenu");
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That((bool)Game.Get(analysis, "IsOpen"), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_move.action.enabled, Is.True);
        }

        [Test]
        public void GroupedRebindUpdatesTapAndHoldWithoutChangingGamepadBindings()
        {
            string originalTap = _tap.action.SaveBindingOverridesAsJson();
            string originalHold = _hold.action.SaveBindingOverridesAsJson();
            try
            {
                var pairType = Game.Type("Core.MainMenu.ActionBindingPair");
                var pairs = Array.CreateInstance(pairType, 2);
                var actions = new[] { _tap, _hold };
                for (int i = 0; i < 2; i++)
                {
                    var pair = Activator.CreateInstance(pairType);
                    Game.Set(pair, "action", actions[i]);
                    Game.Set(pair, "bindingIndex", 0);
                    pairs.SetValue(pair, i);
                }
                var group = Activator.CreateInstance(Game.Type("Core.MainMenu.ControlBindingUI"));
                Game.Set(group, "bindings", pairs);
                Game.Type("Core.MainMenu.ControlOptionsController").GetMethod("ApplyBindingPath")
                    .Invoke(null, new[] { group, "<Keyboard>/f" });
                Assert.That(_tap.action.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/f"));
                Assert.That(_hold.action.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/f"));
                Assert.That(_tap.action.bindings[1].overridePath, Is.Null);
                Assert.That(_hold.action.bindings[1].overridePath, Is.Null);
            }
            finally
            {
                _tap.action.LoadBindingOverridesFromJson(originalTap);
                _hold.action.LoadBindingOverridesFromJson(originalHold);
            }
        }

        [UnityTest]
        public IEnumerator CancelRebindRestoresActionsAndLabel()
        {
            // Use the real controls prefab; cancel before any user input or preference is written.
            var prefab = Resources.Load<GameObject>("Prefabs/MainMenu/ControlOption");
            var root = Object.Instantiate(prefab);
            yield return null;
            var controller = root.GetComponent(Game.Type("Core.MainMenu.ControlOptionsController"));
            var groups = (Array)Game.Get(controller, "bindings");
            var group = groups.GetValue(groups.Length - 1);
            bool tapEnabled = _tap.action.enabled;
            bool holdEnabled = _hold.action.enabled;
            string previousPath = _tap.action.bindings[0].effectivePath;
            Game.Call(controller, "StartRebind", group);
            Assert.That(_tap.action.enabled || _hold.action.enabled, Is.False);
            var operation = (InputActionRebindingExtensions.RebindingOperation)Game.Get(controller, "_rebind");
            operation.Cancel();
            Assert.That(_tap.action.enabled, Is.EqualTo(tapEnabled));
            Assert.That(_hold.action.enabled, Is.EqualTo(holdEnabled));
            Assert.That(_tap.action.bindings[0].effectivePath, Is.EqualTo(previousPath));
            Assert.That(((TextMeshProUGUI)Game.Get(group, "label")).text, Does.Not.Contain("Press a key"));
            Game.Call(controller, "StartRebind", group);
            root.SetActive(false);
            Assert.That(Game.Get(controller, "_rebind"), Is.Null);
            Assert.That(_tap.action.enabled, Is.EqualTo(tapEnabled));
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator LostTutorialIngredientIsReplacedWithoutCharge()
        {
            var tutorial = Game.Find("Core.OnBoarding.OnBoardingController");
            var shop = (Component)Game.Get(tutorial, "pathoNetInterface");
            Game.Call(Game.Find("Core.Computer.ComputerInterface"), "OpenPanel");
            yield return null;
            yield return null;
            var receiver = Game.Get(shop, "pathoItemReceiver");
            var data = Game.Instance("Core.OnBoarding.OnBoardingDatabase");
            var money = new GameObject("Recovery test wallet").AddComponent(Game.Type("Core.Money.MoneyController"));
            int beforeMoney = (int)Game.Get(money, "money");
            var ingredient = Game.Get(data, "itemToBuy1");
            int before = Game.Items(receiver).Count;
            Assert.That(Game.Call(tutorial, "RecoverLostItem", ingredient), Is.EqualTo(true));
            Assert.That(Game.Items(receiver).Count, Is.EqualTo(before + 1));
            Assert.That(Game.Get(Game.Items(receiver)[0], "Item"), Is.SameAs(ingredient));
            Assert.That(Game.Get(money, "money"), Is.EqualTo(beforeMoney));
            Object.Destroy(money.gameObject);
        }

        [UnityTest]
        public IEnumerator WrongCookingAndWrongShipmentKeepTutorialRecoverable()
        {
            var tutorial = Game.Find("Core.OnBoarding.OnBoardingController");
            var shop = (Component)Game.Get(tutorial, "pathoNetInterface");
            Game.Call(Game.Find("Core.Computer.ComputerInterface"), "OpenPanel");
            yield return null;
            yield return null;
            var receiver = Game.Get(shop, "pathoItemReceiver");
            var data = Game.Instance("Core.OnBoarding.OnBoardingDatabase");
            var wrongInput = Game.Get(data, "itemToBuy1");
            var cooking = (IEnumerable)Game.Get(Game.Instance("Core.Item.Cook.CookDatabase"), "Database");
            object wrongOutput = null;
            foreach (var recipe in cooking)
                if (Game.Get(recipe, "inputItem") == wrongInput) wrongOutput = Game.Get(recipe, "resultItem");
            Assert.That(wrongOutput, Is.Not.Null);
            int count = Game.Items(receiver).Count;
            Game.Call(tutorial, "OnItemCooked", wrongOutput);
            Assert.That(Game.Items(receiver).Count, Is.EqualTo(count + 1));
            Game.Call(tutorial, "OnItemSent", wrongInput);
            Assert.That(Game.Items(receiver).Count, Is.EqualTo(count + 2));
        }

        [UnityTest]
        public IEnumerator TutorialCompletesAfterLosingAndRecoveringItsFinalSample()
        {
            var player = Game.Find("Core.Player.PlayerController");
            var interaction = Game.PlayerInteraction();
            var tutorial = Game.Find("Core.OnBoarding.OnBoardingController");
            Game.Call(Game.Get(tutorial, "recipeListInteractable"), "Interact", player);
            Game.Call(Game.Find("Core.MergeLibrary.MergeLibraryInterface"), "ClosePanel");
            yield return new WaitForSecondsRealtime(0.6f);
            Game.Call(Game.Find("Core.Computer.ComputerInterface"), "OpenPanel");
            yield return null;
            yield return null;
            var shop = Game.Get(tutorial, "pathoNetInterface");
            var container = (Transform)Game.Get(shop, "pathoBuyableItemContainer");
            foreach (Transform child in container)
            {
                var buyable = child.GetComponent(Game.Type("Core.Computer.PathoNet.PathoNetBuyableItemBehaviour"));
                if (buyable != null) Game.Call(buyable, "AddItem");
            }
            ((Button)Game.Get(shop, "buyButton")).onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(Game.Get(tutorial, "actualOnBoardingIndex"), Is.EqualTo(2));
            var receiver = Game.Get(shop, "pathoItemReceiver");
            var data = Game.Instance("Core.OnBoarding.OnBoardingDatabase");
            object rawToCook = null;
            foreach (var held in Game.Items(receiver))
                if (Game.Get(held, "Item") == Game.Get(data, "itemToBuy2")) rawToCook = held;
            Assert.That(rawToCook, Is.Not.Null);
            Assert.That(Game.Call(interaction, "GiveItem", rawToCook), Is.EqualTo(true));
            Game.Call(receiver, "RemoveItem", rawToCook);
            var cooker = Game.Get(tutorial, "cookInteractable");
            Game.Call(cooker, "Interact", player);
            yield return new WaitForSeconds((float)Game.Get(cooker, "cookTime") + 0.3f);
            Assert.That(Game.Get(tutorial, "actualOnBoardingIndex"), Is.EqualTo(3));
            Game.Call(cooker, "Interact", player);
            var brewer = Game.Get(tutorial, "brewInteractable");
            Game.Call(brewer, "Interact", player);
            Game.Call(receiver, "Interact", player);
            Game.Call(brewer, "Interact", player);
            float deadline = Time.realtimeSinceStartup + 7f;
            while ((int)Game.Get(tutorial, "actualOnBoardingIndex") == 3 && Time.realtimeSinceStartup < deadline)
            {
                Game.Call(brewer, "InteractHold", player);
                yield return null;
            }
            Assert.That(Game.Get(tutorial, "actualOnBoardingIndex"), Is.EqualTo(4));
            Game.Call(brewer, "Interact", player);
            Game.Call(interaction, "RemoveItem");
            Assert.That(Game.Call(tutorial, "RecoverLostItem", Game.Get(data, "itemToSent")), Is.EqualTo(true));
            Game.Call(receiver, "Interact", player);
            var sender = Game.Get(tutorial, "sendItemInteractable");
            Game.Call(sender, "Interact", player);
            Game.Call(sender, "SendItem");
            deadline = Time.realtimeSinceStartup + 15f;
            while (SceneManager.GetActiveScene().name != "Game" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Game"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator NearestTargetAndPromptAgreeAndDoNotJumpWhileHolding()
        {
            var player = Game.Find("Core.Player.PlayerController");
            var interaction = Game.PlayerInteraction();
            var body = (Transform)Game.Get(player, "body");
            player.GetComponent<Rigidbody>().isKinematic = true;
            body.position = new Vector3(100f, 0f, 100f);
            body.rotation = Quaternion.identity;
            Vector3 center = body.position + new Vector3(0, 1.4f, (float)Game.Get(interaction, "interactionDistance"));
            var first = new GameObject("Near station");
            var second = new GameObject("Far station");
            first.transform.position = center + Vector3.right * 0.1f;
            second.transform.position = center + Vector3.right * 0.45f;
            first.AddComponent<SphereCollider>().radius = 0.1f;
            second.AddComponent<SphereCollider>().radius = 0.1f;
            var near = first.AddComponent(Game.Type("Core.Interaction.DebugInteractable"));
            var far = second.AddComponent(Game.Type("Core.Interaction.DebugInteractable"));
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(Game.Get(interaction, "CurrentTarget"), Is.SameAs(near));
            var label = GameObject.Find("InteractionHints/Action/Hint").GetComponent<TextMeshProUGUI>();
            Assert.That(label.text, Does.Contain("Use"));
            Game.Set(interaction, "_holdTarget", near);
            Game.Set(interaction, "_isInteractingHeld", true);
            first.SetActive(false);
            yield return null;
            Assert.That(Game.Get(interaction, "CurrentTarget"), Is.SameAs(far));
            Assert.That(Game.Get(interaction, "_isInteractingHeld"), Is.EqualTo(false));
            Game.Call(Game.Find("Core.MergeLibrary.MergeLibraryInterface"), "OpenPanel");
            yield return null;
            yield return null;
            Assert.That(Game.Get(interaction, "CurrentTarget"), Is.Null);
            Assert.That(label.gameObject.activeInHierarchy, Is.False);
            Object.Destroy(first);
            Object.Destroy(second);
        }

        [UnityTest]
        public IEnumerator PauseDoesNotIntroduceNaNInLiquidMaterials()
        {
            var ingredient = Game.Get(Game.Instance("Core.OnBoarding.OnBoardingDatabase"), "itemToBuy1");
            var root = Object.Instantiate((GameObject)Game.Get(ingredient, "itemPrefab"));
            var wobble = root.GetComponentInChildren(Game.Type("Wobble"));
            yield return null;
            Time.timeScale = 0f;
            yield return null;
            yield return null;
            Time.timeScale = 1f;
            yield return null;
            Assert.That(float.IsNaN((float)Game.Get(wobble, "wobbleAmountToAddX")), Is.False);
            Assert.That(float.IsNaN((float)Game.Get(wobble, "wobbleAmountToAddZ")), Is.False);
            Object.Destroy(root);
        }
    }
}
