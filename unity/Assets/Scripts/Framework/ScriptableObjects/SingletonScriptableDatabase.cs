using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Framework.ScriptableObjects
{
    public class SingletonScriptableDatabase<T, TData> : SingletonScriptableObject<T> 
        where T : SingletonScriptableObject<T> where TData : ScriptableObject
    {
        [SerializeField]
        public List<TData> Database;

#if UNITY_EDITOR 
        // Crazy function, cannot work without editor functions in Editor/GenericSingletonDatabaseEditor
        public void GetAllData()
        {
            Database = new List<TData>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(TData).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TData asset = AssetDatabase.LoadAssetAtPath<TData>(path);
                if (asset != null && !Database.Contains(asset)) Database.Add(asset);
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
