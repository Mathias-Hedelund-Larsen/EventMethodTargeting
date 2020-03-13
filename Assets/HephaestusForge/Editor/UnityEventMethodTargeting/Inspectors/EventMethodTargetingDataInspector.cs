using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HephaestusForge
{
    namespace UnityEventMethodTargeting
    {
        [CustomEditor(typeof(EventMethodTargetingData))]

        public class EventMethodTargetingDataInspector : Editor
        {
            private SerializedObject _target;

            private void OnEnable()
            {
                _target = new SerializedObject(target);
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                var methodTargetingDataArray = _target.FindProperty("_methodTargetingData");
                List<int> indexesToClear = new List<int>();

                for (int i = methodTargetingDataArray.arraySize - 1; i >= 0; i--)
                {
                    var sceneGuid = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneGuid").stringValue;
                    var objectID = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_objectID").intValue;

                    if (sceneGuid == "None")
                    {
                        Object obj = (Object)typeof(Object).GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { objectID });

                        if (!obj)
                        {
                            indexesToClear.Add(i);
                        }
                    }
                    else
                    {
                        var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                        List<Scene> openScenes = new List<Scene>();

                        for (int sceneIndex = 0; sceneIndex < EditorSceneManager.sceneCount; sceneIndex++)
                        {
                            openScenes.Add(EditorSceneManager.GetSceneAt(sceneIndex));
                        }

                        if (openScenes.Any(s => s.path == scenePath))
                        {
                            var rootObjects = openScenes.Find(s => s.path == scenePath).GetRootGameObjects();
                            PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

                            bool exists = false;

                            for (int t = 0; t < rootObjects.Length; t++)
                            {
                                var components = rootObjects[t].GetComponents<MonoBehaviour>().ToList();

                                components.AddRange(rootObjects[t].GetComponentsInChildren<MonoBehaviour>());

                                for (int x = 0; x < components.Count; x++)
                                {
                                    SerializedObject serializedObject = new SerializedObject(components[x]);
                                    inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                                    SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                                    int localId = localIdProp.intValue;

                                    if (objectID == localId)
                                    {
                                        exists = true;
                                        break;
                                    }
                                }
                            }

                            if (!exists)
                            {
                                indexesToClear.Add(i);
                            }
                        }
                        else
                        {
                            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

                            if (!sceneAsset)
                            {
                                indexesToClear.Add(i);
                            }
                        }
                    }
                }

                if (indexesToClear.Count > 0)
                {                    
                    for (int i = 0; i < indexesToClear.Count; i++)
                    {
                        methodTargetingDataArray.DeleteArrayElementAtIndex(indexesToClear[i]);
                    }

                    _target.ApplyModifiedProperties();

                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}