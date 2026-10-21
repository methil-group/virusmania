//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Outline : MonoBehaviour {
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    public enum Mode {
        OutlineAll,
        OutlineVisible,
        OutlineHidden,
        OutlineAndSilhouette,
        SilhouetteOnly
    }

    public Mode OutlineMode {
        get { return outlineMode; }
        set {
            outlineMode = value;
            needsUpdate = true;
        }
    }

    public Color OutlineColor {
        get { return outlineColor; }
        set {
            outlineColor = value;
            needsUpdate = true;
        }
    }

    public float OutlineWidth {
        get { return outlineWidth; }
        set {
            outlineWidth = value;
            needsUpdate = true;
        }
    }

    public int SortingLayer
    {
        get { return sortingLayer; }
        set
        {
            sortingLayer = value;
            needsUpdate = true;
        }
    }

    [Serializable]
    private class ListVector3 {
        public List<Vector3> data;
    }

    [SerializeField]
    private Mode outlineMode = Mode.OutlineVisible;

    [SerializeField]
    private Color outlineColor = Color.black;

    [SerializeField, Range(0f, 10f)]
    private float outlineWidth = 6f;

    [SerializeField]
    private int sortingLayer;

    [Header("Optional")]

    [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
      + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
    private bool precomputeOutline;

    [SerializeField, Tooltip("If enabled, applies outline to all child meshes.")]
    private bool outlineChildren = false; // Désactivé par défaut

    [SerializeField, HideInInspector]
    private List<Mesh> bakeKeys = new List<Mesh>();

    [SerializeField, HideInInspector]
    private List<ListVector3> bakeValues = new List<ListVector3>();

    private Renderer[] renderers;
    private Material outlineMaskMaterial;
    private Material outlineFillMaterial;

    private bool needsUpdate;

    void Awake() {
        // Cache renderers selon outlineChildren
        if (outlineChildren)
            renderers = GetComponentsInChildren<Renderer>();
        else
            renderers = GetComponents<Renderer>();

        // Instantiate outline materials
        outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
        outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

        outlineMaskMaterial.name = "OutlineMask (Instance)";
        outlineFillMaterial.name = "OutlineFill (Instance)";

        // Retrieve or generate smooth normals
        LoadSmoothNormals();

        // Apply material properties immediately
        UpdateMaterialProperties();
        needsUpdate = true;
    }

    void OnEnable() {
        foreach (var renderer in renderers) {
            // Append outline shaders
            var materials = renderer.sharedMaterials.ToList();
            materials.Add(outlineMaskMaterial);
            materials.Add(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnValidate() {
        // Update material properties
        needsUpdate = true;

        // Clear cache when baking is disabled or corrupted
        if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count) {
            bakeKeys.Clear();
            bakeValues.Clear();
        }

        // Generate smooth normals when baking is enabled
        if (precomputeOutline && bakeKeys.Count == 0) {
            Bake();
        }
    }

    void Update() {
        if (needsUpdate) {
            needsUpdate = false;
            UpdateMaterialProperties();
        }
    }

    void OnDisable() {
        foreach (var renderer in renderers) {
            // Remove outline shaders
            var materials = renderer.sharedMaterials.ToList();
            materials.Remove(outlineMaskMaterial);
            materials.Remove(outlineFillMaterial);
            renderer.materials = materials.ToArray();
        }
    }

    void OnDestroy() {
        // Destroy material instances
        Destroy(outlineMaskMaterial);
        Destroy(outlineFillMaterial);
    }

    void Bake() {
        // Generate smooth normals for each mesh
        var bakedMeshes = new HashSet<Mesh>();

        IEnumerable<MeshFilter> meshFilters = outlineChildren ? GetComponentsInChildren<MeshFilter>() : GetComponents<MeshFilter>();

        foreach (var meshFilter in meshFilters) {
            // Skip duplicates
            if (!bakedMeshes.Add(meshFilter.sharedMesh)) continue;

            // Serialize smooth normals
            var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

            bakeKeys.Add(meshFilter.sharedMesh);
            bakeValues.Add(new ListVector3() { data = smoothNormals });
        }
    }

    void LoadSmoothNormals() {
        IEnumerable<MeshFilter> meshFilters = outlineChildren ? GetComponentsInChildren<MeshFilter>() : GetComponents<MeshFilter>();
        IEnumerable<SkinnedMeshRenderer> skinnedMeshes = outlineChildren ? GetComponentsInChildren<SkinnedMeshRenderer>() : GetComponents<SkinnedMeshRenderer>();

        // Retrieve or generate smooth normals for MeshFilters
        foreach (var meshFilter in meshFilters) {
            if (!registeredMeshes.Add(meshFilter.sharedMesh)) continue;

            var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
            var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

            // Store smooth normals in UV3
            meshFilter.sharedMesh.SetUVs(3, smoothNormals);

            var renderer = meshFilter.GetComponent<Renderer>();
            if (renderer != null) {
                CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
            }
        }

        // Clear UV3 on SkinnedMeshRenderers
        foreach (var skinnedMeshRenderer in skinnedMeshes) {
            if (!registeredMeshes.Add(skinnedMeshRenderer.sharedMesh)) continue;

            skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];
            CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
        }
    }

    List<Vector3> SmoothNormals(Mesh mesh) {
        var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);
        var smoothNormals = new List<Vector3>(mesh.normals);

        foreach (var group in groups) {
            if (group.Count() == 1) continue;

            var smoothNormal = Vector3.zero;
            foreach (var pair in group) smoothNormal += smoothNormals[pair.Value];
            smoothNormal.Normalize();

            foreach (var pair in group) smoothNormals[pair.Value] = smoothNormal;
        }

        return smoothNormals;
    }

    void CombineSubmeshes(Mesh mesh, Material[] materials) {
        if (mesh.subMeshCount == 1) return;
        if (mesh.subMeshCount > materials.Length) return;

        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    void UpdateMaterialProperties() {
        outlineFillMaterial.SetColor("_OutlineColor", outlineColor);
        outlineFillMaterial.SetFloat("_ZRef", sortingLayer + 1);
        outlineMaskMaterial.SetFloat("_ZRef", sortingLayer + 1);
        outlineMaskMaterial.renderQueue = 3100 + 20 * sortingLayer;
        outlineFillMaterial.renderQueue = 3110 + 20 * sortingLayer;

        switch (outlineMode) {
            case Mode.OutlineAll:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineVisible:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineHidden:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.OutlineAndSilhouette:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;

            case Mode.SilhouetteOnly:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
                break;
        }
    }
}
