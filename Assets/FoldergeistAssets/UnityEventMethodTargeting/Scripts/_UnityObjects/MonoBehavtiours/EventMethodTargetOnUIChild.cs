#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.SceneManagement.EditorSceneManager;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        [ExecuteAlways]
        public sealed class EventMethodTargetOnUIChild : MonoBehaviour
        {
#pragma warning disable 0649

            [SerializeField]
            private UIEventChild _onlyForInspectorEventChild;

            [SerializeField]
            private TargetMethodData[] _targetMethods;

#pragma warning restore 0649

            public TargetMethodData[] TargetMethods { get => _targetMethods; }

#if UNITY_EDITOR
            private void Awake()
            {
                if (PrefabStageUtility.GetPrefabStage(gameObject) == null && !EditorApplication.isPlaying)
                {
                    if (!gameObject.CompareTag("EditorOnly"))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            DestroyImmediate(this);
                            Debug.LogWarning($"{GetType().ToString()} should only be added on GameObjects tagged EditorOnly");
                        };

                        return;
                    }
                    else if (!gameObject.transform.parent.GetComponent<Button>() && !gameObject.transform.parent.GetComponent<Slider>())
                    {
                        EditorApplication.delayCall += () =>
                        {
                            DestroyImmediate(this);
                            Debug.LogWarning($"{GetType().ToString()} needs a parent of type button or slider");
                        };

                        return;
                    }

                    var eventMethodTargetingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                        AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodTargetingData")[0]));

                    SerializedObject eventMethodTargeting = new SerializedObject(eventMethodTargetingAsset);
                    var methodTargetingDataArray = eventMethodTargeting.FindProperty("_methodTargetingData");

                    methodTargetingDataArray.arraySize++;

                    var eventMethodData = methodTargetingDataArray.GetArrayElementAtIndex(methodTargetingDataArray.arraySize - 1);

                    if (AssetDatabase.Contains(transform.root.gameObject))
                    {
                        eventMethodData.FindPropertyRelative("_sceneGuid").stringValue = "None";
                        eventMethodData.FindPropertyRelative("_objectID").intValue = GetInstanceID();

                        EditorUtility.SetDirty(eventMethodTargetingAsset);
                        eventMethodTargeting.ApplyModifiedProperties();
                    }
                    else
                    {
                        SceneSavedCallback sceneSaved = null;

                        sceneSaved = (scene) =>
                        {
                            SerializedObject serializedObject = new SerializedObject(this);
                            PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
                            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                            SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                        int localId = localIdProp.intValue;

                            eventMethodData.FindPropertyRelative("_sceneGuid").stringValue = AssetDatabase.AssetPathToGUID(gameObject.scene.path);
                            eventMethodData.FindPropertyRelative("_objectID").intValue = localId;

                            EditorUtility.SetDirty(eventMethodTargetingAsset);
                            eventMethodTargeting.ApplyModifiedProperties();
                            EditorSceneManager.sceneSaved -= sceneSaved;
                        };

                        EditorSceneManager.sceneSaved += sceneSaved;

                        EditorSceneManager.MarkSceneDirty(gameObject.scene);
                        EditorSceneManager.SaveScene(gameObject.scene);
                    }
                }                          
            }

            private void OnDestroy()
            {
                if (PrefabStageUtility.GetPrefabStage(gameObject) == null && !EditorApplication.isPlaying)
                {
                    if (gameObject.CompareTag("EditorOnly") && (gameObject.transform.parent.GetComponent<Button>() || gameObject.transform.parent.GetComponent<Slider>()))
                    {
                        Debug.Log("Destroying prefab: " + gameObject.name);
                    }
                }
            }
#endif
        }
    }
}