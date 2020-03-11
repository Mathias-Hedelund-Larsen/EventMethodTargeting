using FoldergeistAssets.UnityEventMethodTargeting.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using static UnityEditor.SceneManagement.EditorSceneManager;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        [CustomPropertyDrawer(typeof(EventMethodTargetAttribute))]

        public class EventMethodTargetAttributePropertyDrawer : PropertyDrawer
        {
            private SerializedObject _target;
            private static int _propertyHeightMultiplier = 1;

            private readonly Dictionary<SearchFor, Func<TargetMethodData, GameObject, UnityEngine.Object>> _searchForSwitch =
                new Dictionary<SearchFor, Func<TargetMethodData, GameObject, UnityEngine.Object>>()
{
        {
            SearchFor.Asset, (methodTarget, go) =>
            {
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"t:{methodTarget.Ontype}")[0]), methodTarget.Ontype);
            }
        },
        {
            SearchFor.ComponentOnPrefab, (methodTarget, go) =>
            {
                var prefabGUIDs = AssetDatabase.FindAssets($"t:Prefab");
                UnityEngine.Object returned = null;

                for (int i = 0; i < prefabGUIDs.Length; i++)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(prefabGUIDs[i]));

                    if (obj.GetComponent(methodTarget.Ontype))
                    {
                        returned = obj.GetComponent(methodTarget.Ontype);
                        break;
                    }
                }

                return returned;
            }
        },
        {
            SearchFor.ComponentInHierarchy, (methodTarget, go) =>
            {
                return go.GetComponentInChildren(methodTarget.Ontype, true);
            }
        },
        {
            SearchFor.ComponentOnGameObject, (methodTarget, go) =>
            {
                return go.GetComponent(methodTarget.Ontype);
            }
        },
        {
            SearchFor.ComponentInScene, (methodTarget, go) =>
            {
                UnityEngine.Object returned = null;

                var rootObjects = go.scene.GetRootGameObjects();

                for (int i = 0; i < rootObjects.Length; i++)
                {
                    if (rootObjects[i].GetComponent(methodTarget.Ontype))
                    {
                        returned = rootObjects[i].GetComponent(methodTarget.Ontype);
                        break;
                    }
                    else if(rootObjects[i].GetComponentInChildren(methodTarget.Ontype))
                    {
                        returned = rootObjects[i].GetComponentInChildren(methodTarget.Ontype, true);
                        break;
                    }
                }

                return returned;
            }
        }
                };

            private readonly Dictionary<PersistentListenerMode, Func<object, FieldInfo>> _argumentField =
                new Dictionary<PersistentListenerMode, Func<object, FieldInfo>>
                {
            {
                PersistentListenerMode.EventDefined, (obj) => {return null; }
            },
            {
                PersistentListenerMode.Void, (obj) => {return null; }
            },
            {
                PersistentListenerMode.Int, (obj) =>
                {
                    return obj.GetType().GetField("m_IntArgument", BindingFlags.Instance | BindingFlags.NonPublic);
                }
            },
            {
                PersistentListenerMode.Object, (obj) =>
                {
                    return obj.GetType().GetField("m_ObjectArgument", BindingFlags.Instance | BindingFlags.NonPublic);
                }
            },
            {
                PersistentListenerMode.Float, (obj) =>
                {
                    return obj.GetType().GetField("m_FloatArgument", BindingFlags.Instance | BindingFlags.NonPublic);
                }
            },
            {
                PersistentListenerMode.String, (obj) =>
                {
                    return obj.GetType().GetField("m_StringArgument", BindingFlags.Instance | BindingFlags.NonPublic);
                }
            },
            {
                PersistentListenerMode.Bool, (obj) =>
                {
                    return obj.GetType().GetField("m_BoolArgument", BindingFlags.Instance | BindingFlags.NonPublic);
                }
            }
                };

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (fieldInfo.FieldType == typeof(UnityEvent) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<>)) ||
                    fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,>)) ||
                    fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,,>)))
                {
                    return new UnityEventDrawer().GetPropertyHeight(property, label) + (EditorGUIUtility.singleLineHeight + 5) * _propertyHeightMultiplier;
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
                    if (fieldInfo.FieldType != typeof(UIEventChild))
                    {
                        AddDataToEventMethodTargeting(position, property, label, property.serializedObject.targetObject);
                        position.y += (EditorGUIUtility.singleLineHeight + 5) * _propertyHeightMultiplier;

                        EditorGUI.BeginChangeCheck();

                        new UnityEventDrawer().OnGUI(position, property, label);

                        if (EditorGUI.EndChangeCheck())
                        {
                            _target = new SerializedObject(property.serializedObject.targetObject);
                            EditorApplication.delayCall += SetTargetMethodOnEventDelayed;
                        }
                    }
                    else
                    {
                        DrawEventTargeting(position, property, label);
                    }
                }
                else
                {
                    Debug.LogError("Only add the EventMethodTargetAttribute to UnityEvents also the generic ones");
                }
            }

            private void AddDataToEventMethodTargeting(Rect position, SerializedProperty property, GUIContent label, UnityEngine.Object targetObject)
            {
                var eventMethodTargetingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                       AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodTargetingData")[0]));

                SerializedObject eventMethodTargeting = new SerializedObject(eventMethodTargetingAsset);
                var methodTargetingDataArray = eventMethodTargeting.FindProperty("_methodTargetingData");

                if (AssetDatabase.Contains(targetObject))
                {
                    bool isDataContained = CheckIfDataContained(methodTargetingDataArray, "None", targetObject.GetInstanceID());

                    if (!isDataContained)
                    {
                        methodTargetingDataArray.arraySize++;

                        var eventMethodData = methodTargetingDataArray.GetArrayElementAtIndex(methodTargetingDataArray.arraySize - 1);

                        eventMethodData.FindPropertyRelative("_sceneGuid").stringValue = "None";
                        eventMethodData.FindPropertyRelative("_objectID").intValue = targetObject.GetInstanceID();

                        EditorUtility.SetDirty(eventMethodTargetingAsset);
                        eventMethodTargeting.ApplyModifiedProperties();

                    }

                    DrawEventTargeting(position, property, label);
                }
                else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);
                }
                else
                {
                    SceneSavedCallback sceneSaved = null;
                    var gameObject = ((MonoBehaviour)targetObject).gameObject;

                    SerializedObject serializedObject = new SerializedObject(targetObject);
                    PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
                    inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                    SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                    int localId = localIdProp.intValue;

                    bool isDataContained = CheckIfDataContained(methodTargetingDataArray, AssetDatabase.AssetPathToGUID(gameObject.scene.path), localId);

                    if (!isDataContained)
                    {
                        sceneSaved = (scene) =>
                        {
                            EditorSceneManager.sceneSaved -= sceneSaved;

                            methodTargetingDataArray.arraySize++;
                            var eventMethodData = methodTargetingDataArray.GetArrayElementAtIndex(methodTargetingDataArray.arraySize - 1);

                            eventMethodData.FindPropertyRelative("_sceneGuid").stringValue = AssetDatabase.AssetPathToGUID(gameObject.scene.path);
                            eventMethodData.FindPropertyRelative("_objectID").intValue = localId;

                            EditorUtility.SetDirty(eventMethodTargetingAsset);
                            eventMethodTargeting.ApplyModifiedProperties();
                            DrawEventTargeting(position, property, label);
                        };

                        EditorSceneManager.sceneSaved += sceneSaved;

                        EditorSceneManager.MarkSceneDirty(gameObject.scene);
                        EditorSceneManager.SaveScene(gameObject.scene);
                    }
                    else
                    {
                        DrawEventTargeting(position, property, label);
                    }
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

            private bool CheckIfDataContained(SerializedProperty methodTargetingDataArray, string guid, int id)
            {
                for (int i = 0; i < methodTargetingDataArray.arraySize; i++)
                {
                    var propertySceneGuid = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneGuid").stringValue;
                    var propertyObjectID = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_objectID").intValue;

                    if (propertySceneGuid == guid && propertyObjectID == id)
                    {
                        return true;
                    }
                }

                return false;
            }

            private void SetTargetMethodOnEventDelayed()
            {
                EditorApplication.delayCall -= SetTargetMethodOnEventDelayed;

                if (_target != null && _target.targetObject)
                {
                    var eventMethodTargetingAsset = AssetDatabase.LoadAssetAtPath<EventMethodTargetingData>(
                          AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodTargetingData")[0]));

                    var methodTargetingData = (EventMethodData[])typeof(EventMethodTargetingData).GetField("_methodTargetingData", BindingFlags.Instance | BindingFlags.NonPublic).
                        GetValue(eventMethodTargetingAsset);

                    var guidField = typeof(EventMethodData).GetField("_sceneGuid", BindingFlags.Instance | BindingFlags.NonPublic);
                    var objectIDField = typeof(EventMethodData).GetField("_objectID", BindingFlags.Instance | BindingFlags.NonPublic);
                    var targetMethodsField = typeof(EventMethodData).GetField("_targetMethods", BindingFlags.Instance | BindingFlags.NonPublic);

                    var target = _target.targetObject;

                    string serializedObjectGuid = "";
                    int serializedObjectID = 0;

                    if (_target.targetObject is ScriptableObject || AssetDatabase.Contains(_target.targetObject as Component))
                    {
                        serializedObjectGuid = "None";
                        serializedObjectID = target.GetInstanceID();
                    }
                    else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);

                        serializedObjectGuid = "None";
                        serializedObjectID = prefab.GetComponent(target.GetType()).GetInstanceID();
                    }
                    else
                    {
                        SerializedObject serializedObject = new SerializedObject(target);
                        PropertyInfo inspectorModeInfo = typeof(SerializedObject).
                            GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

                        inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                        SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                        serializedObjectGuid = AssetDatabase.AssetPathToGUID((target as Component).gameObject.scene.path);
                        serializedObjectID = localIdProp.intValue;
                    }

                    List<TargetMethodData> targetMethods = new List<TargetMethodData>();

                    for (int i = 0; i < methodTargetingData.Length; i++)
                    {
                        string guid = (string)guidField.GetValue(methodTargetingData[i]);
                        int objectID = (int)objectIDField.GetValue(methodTargetingData[i]);

                        if (guid == serializedObjectGuid && objectID == serializedObjectID)
                        {
                            targetMethods.AddRange((TargetMethodData[])targetMethodsField.GetValue(methodTargetingData[i]));
                        }
                    }

                    for (int i = 0; i < targetMethods.Count; i++)
                    {
                        var methodTarget = targetMethods[i];

                        string targetMethod = methodTarget.MethodName;

                        var persistentCallGroupField = typeof(UnityEventBase).GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
                        var callsField = persistentCallGroupField.FieldType.GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);

                        var persistantCallType = callsField.FieldType.GetGenericArguments()[0];
                        var targetObjectField = persistantCallType.GetField("m_Target", BindingFlags.NonPublic | BindingFlags.Instance);
                        var methodNameField = persistantCallType.GetField("m_MethodName", BindingFlags.NonPublic | BindingFlags.Instance);
                        var modeField = persistantCallType.GetField("m_Mode", BindingFlags.NonPublic | BindingFlags.Instance);

                        var persistentCallGroupInstance = persistentCallGroupField.GetValue(fieldInfo.GetValue(_target.targetObject) as UnityEventBase);

                        IList calls = (IList)callsField.GetValue(persistentCallGroupInstance);

                        if (calls.Count > i)
                        {
                            var callElement = calls[i];

                            var objectTargetForEvent = targetObjectField.GetValue(callElement);

                            if (objectTargetForEvent == null || objectTargetForEvent.ToString() == "null" || objectTargetForEvent.GetType() == methodTarget.Ontype)
                            {
                                var initialTarget = _searchForSwitch[methodTarget.SearchFor].Invoke(methodTarget, (_target.targetObject as Component).gameObject);

                                if (initialTarget)
                                {
                                    modeField.SetValue(callElement, methodTarget.ListenerMode);
                                    targetObjectField.SetValue(callElement, initialTarget);
                                    methodNameField.SetValue(callElement, targetMethod);
                                }
                                else
                                {
                                    Debug.LogError($"Couldnt find asset of type {methodTarget.Ontype}");
                                }
                            }

                            var argumentsField = persistantCallType.GetField("m_Arguments", BindingFlags.NonPublic | BindingFlags.Instance);
                            var arguments = argumentsField.GetValue(callElement);

                            FieldInfo argumentField = _argumentField[methodTarget.ListenerMode].Invoke(arguments);

                            if (argumentField != null)
                            {
                                var fieldValue = argumentField.GetValue(arguments);

                                var limitation = methodTarget.Limitation;

                                if (limitation.Length > 0 && !limitation.Contains(fieldValue))
                                {
                                    argumentField.SetValue(arguments, methodTarget.Limitation[0]);

                                    Debug.LogWarning("You cant change the value outside the limitations of the PrivateMethodTarget");
                                }
                            }
                        }
                    }

                    _target.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}