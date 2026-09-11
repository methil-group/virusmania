using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utils
{
    public static class SceneUtils
    {
        public static T GetComponentInActiveScene<T>(bool includeInactive = true)
            where T : Component
        {
            Scene activeScene = SceneManager.GetActiveScene();

            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(includeInactive);

                if (component != null)
                    return component;
            }

            return null;
        }
    }
}