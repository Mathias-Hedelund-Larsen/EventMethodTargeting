using UnityEngine;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public sealed class EventMethodTargetingData : ScriptableObject
    {
        [SerializeField]
        private EventMethodData[] _methodTargetingData;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/HephaestusForge/Limited to one/EventMethodTargetingData", false, 0)]
        private static void CreateInstance()
        {
            if (UnityEditor.AssetDatabase.FindAssets("t:Settings").Length == 0)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);

                if (path.Length > 0)
                {
                    if (System.IO.Directory.Exists(path))
                    {
                        UnityEditor.AssetDatabase.CreateAsset(CreateInstance<EventMethodTargetingData>(), path + "/EventMethodTargetingData.asset");
                    }
                }
            }
            else
            {
                Debug.LogWarning("An instance of Settings already exists");
            }
        }
#endif
    }
}