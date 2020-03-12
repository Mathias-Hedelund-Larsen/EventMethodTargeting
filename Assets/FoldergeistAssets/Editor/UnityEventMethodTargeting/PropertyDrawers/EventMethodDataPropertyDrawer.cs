using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        [CustomPropertyDrawer(typeof(EventMethodData))]
        public class EventMethodDataPropertyDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                int _heightModifier = 1;
                var sceneGuid = property.FindPropertyRelative("_sceneGuid").stringValue;
                var objectID = property.FindPropertyRelative("_objectID").intValue;

                if (sceneGuid == "None")
                {
                    Object obj = (Object)typeof(Object).GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { objectID });

                    _heightModifier = 1;
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
                        bool drawn = false;
                        for (int i = 0; i < rootObjects.Length; i++)
                        {
                            var components = rootObjects[i].GetComponents<MonoBehaviour>().ToList();

                            components.AddRange(rootObjects[i].GetComponentsInChildren<MonoBehaviour>());

                            for (int t = 0; t < components.Count; t++)
                            {
                                SerializedObject serializedObject = new SerializedObject(components[t]);
                                inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                                SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                                int localId = localIdProp.intValue;

                                if (objectID == localId)
                                {
                                    _heightModifier = 1;
                                    drawn = true;
                                    break;
                                }
                            }
                        }

                        if (!drawn)
                        {
                            _heightModifier = 2;
                        }
                    }
                    else
                    {
                        _heightModifier = 1;
                    }
                }

                return (EditorGUIUtility.singleLineHeight + 5) * _heightModifier;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var sceneGuid = property.FindPropertyRelative("_sceneGuid").stringValue;
                var objectID = property.FindPropertyRelative("_objectID").intValue;
                position.height = EditorGUIUtility.singleLineHeight;                                

                GUI.enabled = false;

                if (sceneGuid == "None")
                {
                    Object obj = (Object)typeof(Object).GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { objectID });

                    EditorGUI.ObjectField(position, new GUIContent("Reference"), obj, typeof(Object), true);
                }
                else
                {
                    var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                    List<Scene> openScenes = new List<Scene>();

                    for (int sceneIndex = 0; sceneIndex < EditorSceneManager.sceneCount; sceneIndex++)
                    {
                        openScenes.Add(EditorSceneManager.GetSceneAt(sceneIndex));
                    }

                    if(openScenes.Any(s => s.path == scenePath))
                    {
                        var rootObjects = openScenes.Find(s => s.path == scenePath).GetRootGameObjects();
                        PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
                        bool drawn = false;
                        for (int i = 0; i < rootObjects.Length; i++)
                        {
                            var components = rootObjects[i].GetComponents<MonoBehaviour>().ToList();

                            components.AddRange(rootObjects[i].GetComponentsInChildren<MonoBehaviour>());

                            for (int t = 0; t < components.Count; t++)
                            {
                                SerializedObject serializedObject = new SerializedObject(components[t]);
                                inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                                SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                                int localId = localIdProp.intValue;

                                if(objectID == localId)
                                {
                                    EditorGUI.ObjectField(position, new GUIContent("Reference"), components[t], typeof(MonoBehaviour), true);
                                    drawn = true;
                                    break;
                                }
                            }
                        }

                        if (!drawn)
                        {
                            EditorGUI.TextField(position, "Couldnt find object in the open scene");
                            position.y += EditorGUIUtility.singleLineHeight + 5;
                            GUI.enabled = true;

                            position.x += 15;
                            position.width -= 15;

                            if (GUI.Button(position, "Delete"))
                            {
                                EditorApplication.delayCall += () =>
                                {
                                    GetSubStringBetweenChars(property.propertyPath, '[', ']', out string full, out string index);

                                    property.serializedObject.FindProperty(property.propertyPath.Split('.')[0]).DeleteArrayElementAtIndex(int.Parse(index));

                                    property.serializedObject.ApplyModifiedProperties();
                                };
                            }
                        }
                    }
                    else
                    {
                        EditorGUI.ObjectField(position, new GUIContent("SceneReference"), AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath), typeof(SceneAsset), false);
                    }
                }

                GUI.enabled = true;
            }

            public void GetSubStringBetweenChars(string origin, char start, char end, out string fullMatch, out string insideEncapsulation)
            {
                var match = Regex.Match(origin, string.Format(@"\{0}(.*?)\{1}", start, end));
                fullMatch = match.Groups[0].Value;
                insideEncapsulation = match.Groups[1].Value;
            }
        }
    }
}