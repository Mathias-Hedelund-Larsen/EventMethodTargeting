#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using static UnityEditor.SceneManagement.EditorSceneManager;
#endif
using UnityEngine;
using UnityEngine.UI;
using FoldergeistAssets.UnityEventMethodTargeting.Internal;
using UnityEngine.SceneManagement;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        [ExecuteAlways, DisallowMultipleComponent]
        public sealed class EventMethodTargetOnUIChild : MonoBehaviour
        {
#if UNITY_EDITOR
#pragma warning disable 0649

            [SerializeField, EventMethodTarget]
            private UIEventChild _onlyForInspectorEventChild;

            [SerializeField, HideInInspector]
            private bool _onlyForInspectorSceneIsClosing;

#pragma warning restore 0649

            private void OnPrefabAssetCreated()
            {
                Awake();
            }

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

                    if (AssetDatabase.Contains(transform.root.gameObject))
                    {
                        bool isDataContained = CheckIfDataContained(methodTargetingDataArray, "None", GetInstanceID());

                        if (!isDataContained)
                        {
                            methodTargetingDataArray.arraySize++;

                            var eventMethodData = methodTargetingDataArray.GetArrayElementAtIndex(methodTargetingDataArray.arraySize - 1);

                            eventMethodData.FindPropertyRelative("_sceneGuid").stringValue = "None";
                            eventMethodData.FindPropertyRelative("_objectID").intValue = GetInstanceID();

                            EditorUtility.SetDirty(eventMethodTargetingAsset);
                            eventMethodTargeting.ApplyModifiedProperties();
                        }
                    }
                    else
                    {
                        SceneSavedCallback sceneSaved = null;

                        sceneSaved = (scene) =>
                        {
                            EditorSceneManager.sceneSaved -= sceneSaved;
                            
                            SerializedObject serializedObject = new SerializedObject(this);
                            PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
                            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                            SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                            int localId = localIdProp.intValue;

                            bool isDataContained = CheckIfDataContained(methodTargetingDataArray, AssetDatabase.AssetPathToGUID(gameObject.scene.path), localId);

                            if (!isDataContained)
                            {
                                methodTargetingDataArray.arraySize++;
                                var eventMethodData = methodTargetingDataArray.GetArrayElementAtIndex(methodTargetingDataArray.arraySize - 1);

                                eventMethodData.FindPropertyRelative("_sceneGuid").stringValue = AssetDatabase.AssetPathToGUID(gameObject.scene.path);
                                eventMethodData.FindPropertyRelative("_objectID").intValue = localId;

                                EditorUtility.SetDirty(eventMethodTargetingAsset);
                                eventMethodTargeting.ApplyModifiedProperties();
                            }
                        };

                        EditorSceneManager.sceneSaved += sceneSaved;

                        EditorSceneManager.MarkSceneDirty(gameObject.scene);
                        EditorSceneManager.SaveScene(gameObject.scene);
                    }


                    _onlyForInspectorSceneIsClosing = false;

                    EditorSceneManager.sceneClosing += SceneIsClosing;
                    EditorSceneManager.sceneOpening += SceneIsOpening;
                }
            }

            private bool CheckIfDataContained(SerializedProperty methodTargetingDataArray, string guid, int id)
            {
                for (int i = 0; i < methodTargetingDataArray.arraySize; i++)
                {
                    var propertySceneGuid = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneGuid").stringValue;
                    var propertyObjectID = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_objectID").intValue;

                    if(propertySceneGuid == guid && propertyObjectID == id)
                    {
                        return true;
                    }
                }

                return false;
            }

            private void SceneIsOpening(string path, OpenSceneMode mode)
            {
                if (this && mode == OpenSceneMode.Single)
                {
                    _onlyForInspectorSceneIsClosing = true;
                    EditorSceneManager.sceneOpening -= SceneIsOpening;
                }
            }

            private void OnPrefabAssetDestroy()
            {
                OnDestroy();
            }

            private void OnDestroy()
            {
                if (!_onlyForInspectorSceneIsClosing && PrefabStageUtility.GetPrefabStage(gameObject) == null && !EditorApplication.isPlaying)
                {
                    if (gameObject.CompareTag("EditorOnly") && (gameObject.transform.parent.GetComponent<Button>() || gameObject.transform.parent.GetComponent<Slider>()))
                    {
                        var eventMethodTargetingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                        AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodTargetingData")[0]));

                        SerializedObject eventMethodTargeting = new SerializedObject(eventMethodTargetingAsset);
                        var methodTargetingDataArray = eventMethodTargeting.FindProperty("_methodTargetingData");

                        SerializedObject serializedObject = new SerializedObject(this);
                        PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
                        inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                        SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                        int localId = localIdProp.intValue;

                        for (int i = methodTargetingDataArray.arraySize - 1; i >= 0; i--)
                        {
                            if (AssetDatabase.Contains(transform.root.gameObject))
                            {
                                if(methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneGuid").stringValue == "None" &&
                                    methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_objectID").intValue == GetInstanceID())
                                {
                                    methodTargetingDataArray.DeleteArrayElementAtIndex(i);
                                    EditorUtility.SetDirty(eventMethodTargetingAsset);
                                    eventMethodTargeting.ApplyModifiedProperties();

                                    break;
                                }                                                                
                            }
                            else
                            {     
                                if (methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneGuid").stringValue == 
                                    AssetDatabase.AssetPathToGUID(gameObject.scene.path) &&
                                    methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_objectID").intValue == localId)
                                {
                                    methodTargetingDataArray.DeleteArrayElementAtIndex(i);
                                    EditorUtility.SetDirty(eventMethodTargetingAsset);
                                    eventMethodTargeting.ApplyModifiedProperties();

                                    break;
                                }
                            }
                        }                        
                    }
                }
            }

            private void SceneIsClosing(Scene scene, bool removingScene)
            {
                if(this && gameObject.scene == scene)
                {
                    _onlyForInspectorSceneIsClosing = true;
                    EditorSceneManager.sceneClosing -= SceneIsClosing;
                }
            }
#endif
        }
    }
}