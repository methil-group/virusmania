using System;
using System.Linq;
using System.IO;
using Core.Analysis;
using Core.Computer;
using Core.MergeLibrary;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Editor-only authoring through Pipeline. Production windows share a nested prefab.
public static class ComposeModalHeaders
{
    const string Prefabs = "Assets/Resources/Prefabs/";
    static readonly Vector2 TopLeft = new Vector2(0, 1);
    static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        return rect;
    }

    static GameObject MakeHeader()
    {
        var root = Rect("ModalHeader", null);
        root.anchorMin = new Vector2(0, 1); root.anchorMax = Vector2.one;
        root.pivot = new Vector2(.5f, 1); root.anchoredPosition = new Vector2(24, 96);
        root.sizeDelta = new Vector2(-176, 128);
        var ignore = root.gameObject.AddComponent<LayoutElement>(); ignore.ignoreLayout = true;
        var title = Rect("Title", root).gameObject.AddComponent<TextMeshProUGUI>();
        title.rectTransform.pivot = new Vector2(0, .5f);
        title.rectTransform.anchorMin = Vector2.zero; title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = Vector2.zero; title.rectTransform.offsetMax = new Vector2(-304, 0);
        var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "UserInterface/Button.prefab");
        title.font = buttonPrefab.GetComponentInChildren<TMP_Text>(true).font;
        title.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Material/UI/AnalysisHeading.mat");
        title.fontSize = 104; title.text = "TITLE"; title.color = Color.white;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.textWrappingMode = TextWrappingModes.NoWrap; title.overflowMode = TextOverflowModes.Overflow;
        title.extraPadding = true; title.raycastTarget = false;
        var exitPrefab = MakeExitReference();
        var exit = (GameObject)PrefabUtility.InstantiatePrefab(exitPrefab, root);
        exit.name = "ExitButton";
        var rect = (RectTransform)exit.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1, .5f);
        rect.pivot = new Vector2(1, .5f); rect.anchoredPosition = Vector2.zero;
        var result = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Prefabs + "UserInterface/ModalHeader.prefab");
        Object.DestroyImmediate(root.gameObject);
        return result;
    }

    static GameObject MakeExitReference()
    {
        const string output = Prefabs + "UserInterface/ModalExitButton.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(output);
        if (existing) return existing;
        const string temporary = "Assets/Editor/ModalExitReference.prefab";
        File.Copy(Path.Combine(Application.dataPath, "../Logs/OriginalComputerPanel.prefab"), temporary, true);
        AssetDatabase.ImportAsset(temporary, ImportAssetOptions.ForceSynchronousImport);
        GameObject clone = null;
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(temporary);
            var original = source.transform.Find("ComputerInnerPanel/Computer/PathoNet/PathoNetItem/ExitButton");
            clone = Object.Instantiate(original.gameObject);
            clone.name = "ModalExitButton";
            if (PrefabUtility.IsPartOfPrefabInstance(clone))
                PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            clone.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            clone.GetComponentInChildren<TMP_Text>(true).ForceMeshUpdate(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)clone.transform);
            return PrefabUtility.SaveAsPrefabAsset(clone, output);
        }
        finally
        {
            if (clone) Object.DestroyImmediate(clone);
            AssetDatabase.DeleteAsset(temporary);
        }
    }

    static void Backdrop(GameObject root)
    {
        var black = root.transform.Find("BlackPanel") as RectTransform;
        if (!black) black = Rect("BlackPanel", root.transform);
        black.anchorMin = Vector2.zero; black.anchorMax = Vector2.one;
        black.offsetMin = black.offsetMax = Vector2.zero;
        black.localScale = Vector3.one; black.SetAsFirstSibling(); black.gameObject.SetActive(true);
        var image = black.GetComponent<Image>() ?? black.gameObject.AddComponent<Image>();
        image.material = null; image.sprite = null; image.type = Image.Type.Simple;
        image.color = Color.clear; image.raycastTarget = false;
        var controller = new SerializedObject(root.GetComponent<AnalysisInterface>());
        controller.FindProperty("blackPanel").objectReferenceValue = image;
        controller.ApplyModifiedPropertiesWithoutUndo();
    }

    static Transform Header(Transform parent, GameObject prefab, string title, UnityAction close)
    {
        var old = parent.Find("Header"); if (old) Object.DestroyImmediate(old.gameObject);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = "Header";
        instance.GetComponentInChildren<TMP_Text>().text = title;
        UnityEventTools.AddPersistentListener(instance.GetComponentInChildren<Button>().onClick, close);
        return instance.transform;
    }

    static void Edit(string path, Action<GameObject> edit)
    {
        path = Prefabs + path + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static object Run()
    {
        var header = MakeHeader();
        Edit("Analysis/AnalysisPanel", root => {
            Backdrop(root);
            var ui = root.GetComponent<AnalysisInterface>();
            var card = root.transform.Find("InnerPanel/AnalysisWindow");
            foreach (string name in new[] { "Heading", "CloseButton" })
            { var old = card.Find(name); if (old) Object.DestroyImmediate(old.gameObject); }
            var analysisHeader = Header(card, header, "ANALYSIS", ui.ClosePanel);
            analysisHeader.Find("Title").localRotation = Quaternion.Euler(0, 0, 3);
            var report = card.Find("ReportContent");
            foreach (string name in new[] { "SampleName", "SampleDescription", "RequestBadge", "ThreatHeading", "EmptyThreatState", "ThreatViewport" })
            {
                var rect = (RectTransform)report.Find(name);
                rect.anchoredPosition = new Vector2(112, rect.anchoredPosition.y);
                rect.sizeDelta = new Vector2(552, rect.sizeDelta.y);
            }
            var status = (RectTransform)report.Find("RequestBadge/RequestStatus");
            status.sizeDelta = new Vector2(512, status.sizeDelta.y);
        });
        Edit("Computer/ComputerPanel", root => {
            var ui = root.GetComponent<ComputerInterface>();
            var card = root.transform.Find("ComputerInnerPanel/Computer/PathoNet/PathoNetItem");
            card.Find("ShopImage").gameObject.SetActive(false);
            var old = card.Find("ExitButton"); if (old) Object.DestroyImmediate(old.gameObject);
            Header(card, header, "SHOP", ui.ClosePanel);
        });
        Edit("MergeLibrary/MergeLibraryPanel", root => {
            var ui = root.GetComponent<MergeLibraryInterface>();
            var card = (RectTransform)root.transform.Find("MergeLibraryInnerPanel");
            card.sizeDelta = new Vector2(1120, 760);
            card.Find("Image").gameObject.SetActive(false);
            var old = card.Find("Button"); if (old) Object.DestroyImmediate(old.gameObject);
            var mask = card.GetComponent<RectMask2D>(); if (mask) Object.DestroyImmediate(mask);
            var viewport = card.Find("RecipeViewport") as RectTransform;
            if (!viewport) {
                viewport = Rect("RecipeViewport", card);
                viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
                viewport.offsetMin = new Vector2(64, 112); viewport.offsetMax = new Vector2(-64, -112);
                viewport.gameObject.AddComponent<RectMask2D>();
                // A transparent graphic lets wheel and pointer drags reach the scroll rect.
                var hitArea = viewport.gameObject.AddComponent<Image>(); hitArea.color = Color.clear;
                var content = (RectTransform)card.Find("Panel"); content.SetParent(viewport, false);
            }
            viewport.offsetMin = new Vector2(64, 112); viewport.offsetMax = new Vector2(-64, -112);
            var panel = (RectTransform)viewport.Find("Panel");
            panel.anchorMin = TopLeft; panel.anchorMax = Vector2.one; panel.pivot = new Vector2(.5f, 1);
            panel.anchoredPosition = Vector2.zero; panel.sizeDelta = Vector2.zero;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 16, 24); layout.spacing = 24;
            var scroll = card.GetComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = panel;
            scroll.scrollSensitivity = 36; scroll.movementType = ScrollRect.MovementType.Clamped;
            var image = card.GetComponent<Image>();
            image.color = new Color(.14509805f, 1, .27450982f);
            image.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Material/UI/Analysis/AnalysisSurface.mat");
            Header(card, header, "RECIPES", ui.ClosePanel);
            var hint = card.Find("ScrollHint")?.GetComponent<TMP_Text>();
            if (!hint) hint = Rect("ScrollHint", card).gameObject.AddComponent<TextMeshProUGUI>();
            hint.font = header.GetComponentInChildren<TMP_Text>().font;
            hint.fontSharedMaterial = hint.font.material; hint.color = Color.black; hint.fontSize = 22;
            hint.text = "SCROLL TO EXPLORE RECIPES"; hint.alignment = TextAlignmentOptions.Center;
            hint.raycastTarget = false;
            hint.rectTransform.anchorMin = new Vector2(0, 0); hint.rectTransform.anchorMax = new Vector2(1, 0);
            hint.rectTransform.pivot = new Vector2(.5f, 0); hint.rectTransform.anchoredPosition = new Vector2(0, 56);
            hint.rectTransform.sizeDelta = new Vector2(-128, 32);
        });
        const string menuPath = "Assets/Scenes/MainMenu.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(menuPath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(menuPath, OpenSceneMode.Additive);
        try
        {
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Core.MainMenu.MainMenuController>(true)).Single();
            var rect = (RectTransform)menu.transform;
            rect.anchorMin = new Vector2(.65f, 0); rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(-32, 22);
            rect.sizeDelta = new Vector2(-64, 620.593f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            var logo = (RectTransform)menu.transform.Find("Logo");
            logo.anchorMin = new Vector2(0, .5f); logo.anchorMax = new Vector2(1, .5f);
            logo.sizeDelta = new Vector2(-48, 448);
            logo.GetComponent<Image>().preserveAspect = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(logo);
            PrefabUtility.RecordPrefabInstancePropertyModifications(logo.GetComponent<Image>());
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        return new { sharedHeader = Prefabs + "UserInterface/ModalHeader.prefab", windows = 3 };
    }
}
