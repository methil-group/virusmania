using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Coffee.UIEffects;

public static class NeutralPetri
{
    public static object Run()
    {
        const string materialPath = "Assets/Resources/Material/UI/Analysis/NeutralPetri.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        var shader = Shader.Find("UI/NeutralIllustration");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new System.InvalidOperationException("Neutral illustration shader did not compile.");
        if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
        material.shader = shader; material.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
        const string prefabPath = "Assets/Resources/Prefabs/Analysis/AnalysisPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var dish = root.transform.Find("InnerPanel/AnalysisWindow/SamplePreview/PetriDish");
            // Retain the original effect and its outline behind an opaque interior.
            var image = dish.GetComponent<Image>(); image.material = null; image.color = Color.white;
            var effect = dish.GetComponent<UIEffectReplica>() ?? dish.gameObject.AddComponent<UIEffectReplica>();
            effect.target = root.transform.Find("InnerPanel/AnalysisWindow/SamplePreview/SampleImage").GetComponent<UIEffect>();
            var fill = dish.Find("NeutralFill") as RectTransform;
            if (!fill)
            {
                fill = new GameObject("NeutralFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                fill.gameObject.layer = 5; fill.SetParent(dish, false);
            }
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImage = fill.GetComponent<Image>();
            fillImage.sprite = image.sprite; fillImage.preserveAspect = true;
            fillImage.color = Color.white; fillImage.material = material; fillImage.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { material = materialPath, shader = shader.name, originalTexturePreserved = true };
    }
}
