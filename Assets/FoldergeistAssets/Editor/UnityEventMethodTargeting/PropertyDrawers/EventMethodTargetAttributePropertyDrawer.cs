using FoldergeistAssets.UnityEventMethodTargeting.Internal;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        [CustomPropertyDrawer(typeof(EventMethodTargetAttribute))]

        public class EventMethodTargetAttributePropertyDrawer : PropertyDrawer
        {
            private static int _propertyHeightMultiplier = 1;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (fieldInfo.FieldType == typeof(UnityEvent) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<>)) ||
                    fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,>)) ||
                    fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,,>)))
                {
                    return new UnityEventDrawer().GetPropertyHeight(property, label);
                }
                else if(fieldInfo.FieldType == typeof(UIEventChild))
                {
                    return (EditorGUIUtility.singleLineHeight + 5) * _propertyHeightMultiplier;
                }
                else
                {
                    return 0;
                }
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (fieldInfo.FieldType == typeof(UIEventChild) || fieldInfo.FieldType == typeof(UnityEvent) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<>)) ||
                    fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,>)) ||
                    fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,,>)))
                {
                    DrawEventTargeting(position, property, label);
                }
                else
                {
                    Debug.LogError("Only add the EventMethodTargetAttribute to UnityEvents also the generic ones");
                }
            }

            private void DrawEventTargeting(Rect position, SerializedProperty property, GUIContent label)
            {
                var eventMethodTargetingAsset = AssetDatabase.LoadAssetAtPath<EventMethodTargetingData>(
                       AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodTargetingData")[0]));

                SerializedObject eventMethodTargeting = new SerializedObject(eventMethodTargetingAsset);
                var methodTargetingDataArray = eventMethodTargeting.FindProperty("_methodTargetingData");

                if(property.serializedObject.targetObject is ScriptableObject || 
                    AssetDatabase.Contains((property.serializedObject.targetObject as MonoBehaviour).transform.root.gameObject))
                {
                    SerializedProperty toBeDrawn = FindDataArrayElementForAsset(methodTargetingDataArray, property.serializedObject.targetObject);

                    EditorGUI.BeginChangeCheck();

                    if (toBeDrawn != null)
                    {
                        ToBeDrawPropertyHeightModification(toBeDrawn);

                        EditorGUI.PropertyField(position, toBeDrawn, new GUIContent("TargetMethods"), true);
                    }
                    else
                    {
                        EditorGUI.TextField(position, $"Couldnt find component of type {fieldInfo.FieldType.Name} on the preafab asset");
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(eventMethodTargetingAsset);
                        eventMethodTargeting.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                    }
                }
                else if(PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);
                    SerializedProperty toBeDrawn = null;

                    var components = prefab.GetComponents(property.serializedObject.targetObject.GetType()).ToList();

                    components.AddRange(prefab.GetComponentsInChildren(property.serializedObject.targetObject.GetType()));

                    for (int i = 0; i < components.Count; i++)
                    {
                        toBeDrawn = FindDataArrayElementForAsset(methodTargetingDataArray, components[i]);
                        
                        if(toBeDrawn != null)
                        {
                            break;
                        }
                    }

                    EditorGUI.BeginChangeCheck();

                    if (toBeDrawn != null)
                    {
                        ToBeDrawPropertyHeightModification(toBeDrawn);

                        EditorGUI.PropertyField(position, toBeDrawn, new GUIContent("TargetMethods"), true);
                    }
                    else
                    {
                        EditorGUI.TextField(position, $"Couldnt find component of type {fieldInfo.FieldType.Name} on the preafab asset");
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(eventMethodTargetingAsset);
                        eventMethodTargeting.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                    }
                }
                else
                {
                    SerializedObject serializedObject = new SerializedObject(property.serializedObject.targetObject);
                    PropertyInfo inspectorModeInfo = typeof(SerializedObject).
                        GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

                    inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                    SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                    int localId = localIdProp.intValue;

                    var sceneGuid = AssetDatabase.AssetPathToGUID((property.serializedObject.targetObject as MonoBehaviour).gameObject.scene.path);

                    SerializedProperty toBeDrawn = FindProeprtyFromSceneAssetWithID(methodTargetingDataArray, sceneGuid, localId);

                    EditorGUI.BeginChangeCheck();

                    if (toBeDrawn != null)
                    {
                        ToBeDrawPropertyHeightModification(toBeDrawn);

                        EditorGUI.PropertyField(position, toBeDrawn, new GUIContent("TargetMethods"), true);
                    }
                    else
                    {
                        EditorGUI.TextField(position, $"Couldnt find component of type {fieldInfo.FieldType.Name} on the preafab asset");
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(eventMethodTargetingAsset);
                        eventMethodTargeting.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            private SerializedProperty FindProeprtyFromSceneAssetWithID(SerializedProperty methodTargetingDataArray, string sceneGuid, int objectID)
            {
                for (int i = methodTargetingDataArray.arraySize - 1; i >= 0; i--)
                {
                    var propertySceneGuid = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneGuid").stringValue;
                    var propertyObjectID = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_objectID").intValue;

                    if (sceneGuid == propertySceneGuid && objectID == propertyObjectID)
                    {
                        return methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_targetMethods");
                    }
                }

                return null;
            }

            private void ToBeDrawPropertyHeightModification(SerializedProperty toBeDrawn)
            {
                if (toBeDrawn.isExpanded)
                {
                    _propertyHeightMultiplier = (toBeDrawn.arraySize) + 2;

                    for (int i = 0; i < toBeDrawn.arraySize; i++)
                    {
                        if (toBeDrawn.GetArrayElementAtIndex(i).isExpanded)
                        {
                            _propertyHeightMultiplier += 5;
                        }
                    }
                }
                else
                {
                    _propertyHeightMultiplier = 1;
                }
            }

            private SerializedProperty FindDataArrayElementForAsset(SerializedProperty methodTargetingDataArray, UnityEngine.Object targetObject)
            {
                for (int i = methodTargetingDataArray.arraySize - 1; i >= 0; i--)
                {
                    var sceneGuid = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneGuid").stringValue;
                    var objectID = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_objectID").intValue;

                    if(sceneGuid == "None" && objectID == targetObject.GetInstanceID())
                    {
                        return methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_targetMethods");
                    }
                }

                return null;
            }
        }
    }
}