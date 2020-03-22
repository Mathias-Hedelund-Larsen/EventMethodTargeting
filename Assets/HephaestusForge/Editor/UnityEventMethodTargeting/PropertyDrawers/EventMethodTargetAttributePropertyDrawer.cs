using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using static UnityEditor.SceneManagement.EditorSceneManager;

namespace HephaestusForge.UnityEventMethodTargeting
{
    [CustomPropertyDrawer(typeof(EventMethodTargetAttribute))]
    public class EventMethodTargetAttributePropertyDrawer : PropertyDrawer
    {
        private static SerializedObject _eventMethodTargeting;
        private static EventMethodTargetingData _eventMethodTargetingAsset;

        private float _extraHeight;
        private SerializedObject _target;
        private UnityEventDrawer _eventDrawer = new UnityEventDrawer();
        private TargetMethodDataPropertyDrawer _targetMethodDataPropertyDrawer = new TargetMethodDataPropertyDrawer();
        private FieldInfo _guidField = typeof(EventMethodData).GetField("_sceneGuid", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo _objectIDField = typeof(EventMethodData).GetField("_objectID", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo _targetMethodsField = typeof(EventMethodData).GetField("_targetMethods", BindingFlags.Instance | BindingFlags.NonPublic);
        private FieldInfo _methodTargetingDataField = typeof(EventMethodTargetingData).GetField("_methodTargetingData", BindingFlags.Instance | BindingFlags.NonPublic);

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

        private string _propertyPath;
        private List<string> _initialized = new List<string>();

        private void Init(SerializedProperty property)
        {
            if (!_eventMethodTargetingAsset)
            {
                _eventMethodTargetingAsset = AssetDatabase.LoadAssetAtPath<EventMethodTargetingData>(
                      AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodTargetingData")[0]));

                _eventMethodTargeting = new SerializedObject(_eventMethodTargetingAsset);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _propertyPath = property.propertyPath;
            _target = property.serializedObject.targetObject;

            if (!_initialized.Contains(_propertyPath))
            {
                _initialized.Add(_propertyPath);

                Init(property);
            }

            if (fieldInfo.FieldType == typeof(UnityEvent) || fieldInfo.FieldType.IsSubclassOf(typeof(UnityEvent)) ||
                fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,>)) ||
                fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,,>)))
            {
                _extraHeight = 0;

                var methodTargetingDataArray = _eventMethodTargeting.FindProperty("_methodTargetingData");

                if (methodTargetingDataArray.isExpanded)
                {
                    if (property.serializedObject.targetObject is ScriptableObject ||
                        AssetDatabase.Contains((property.serializedObject.targetObject as MonoBehaviour).transform.root.gameObject))
                    {
                        var effectHeight = FindDataArrayElementForAsset(methodTargetingDataArray, property.serializedObject.targetObject);

                        if (effectHeight != null && effectHeight.isExpanded)
                        {
                            _extraHeight += (EditorGUIUtility.singleLineHeight + 5);

                            for (int i = 0; i < effectHeight.arraySize; i++)
                            {
                                _extraHeight += _targetMethodDataPropertyDrawer.GetPropertyHeight(effectHeight.GetArrayElementAtIndex(i), label);
                            }
                        }
                    }
                    else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);

                        SerializedProperty effectHeight = null;

                        var components = prefab.GetComponents(property.serializedObject.targetObject.GetType()).ToList();

                        components.AddRange(prefab.GetComponentsInChildren(property.serializedObject.targetObject.GetType()));

                        for (int i = 0; i < components.Count; i++)
                        {
                            effectHeight = FindDataArrayElementForAsset(methodTargetingDataArray, components[i]);

                            if (effectHeight != null && effectHeight.isExpanded)
                            {
                                _extraHeight += (EditorGUIUtility.singleLineHeight + 5);

                                for (int t = 0; t < effectHeight.arraySize; t++)
                                {
                                    _extraHeight += _targetMethodDataPropertyDrawer.GetPropertyHeight(effectHeight.GetArrayElementAtIndex(t), label);
                                }

                                break;
                            }
                        }
                    }
                    else
                    {
                        int localId = GetLocalFileID(property);

                        var sceneGuid = AssetDatabase.AssetPathToGUID((property.serializedObject.targetObject as MonoBehaviour).gameObject.scene.path);

                        SerializedProperty effectHeight = FindPropertyFromSceneAssetWithID(methodTargetingDataArray, sceneGuid, localId);

                        if (effectHeight != null && effectHeight.isExpanded)
                        {
                            _extraHeight += (EditorGUIUtility.singleLineHeight + 5);

                            for (int i = 0; i < effectHeight.arraySize; i++)
                            {
                                _extraHeight += _targetMethodDataPropertyDrawer.GetPropertyHeight(effectHeight.GetArrayElementAtIndex(i), label);
                            }
                        }
                    }
                }

                return _eventDrawer.GetPropertyHeight(property, label) + (EditorGUIUtility.singleLineHeight + 5) + _extraHeight;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight + 5;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (fieldInfo.FieldType == typeof(UnityEvent) || fieldInfo.FieldType.IsSubclassOf(typeof(UnityEvent)) ||
                fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,>)) ||
                fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,,>)))
            {
                AddDataToEventMethodTargeting(position, property, label, property.serializedObject.targetObject);
                position.y += (EditorGUIUtility.singleLineHeight + 5) + _extraHeight;

                EditorGUI.BeginChangeCheck();

                _eventDrawer.OnGUI(position, property, label);

                if (EditorGUI.EndChangeCheck())
                {
                    _target = new SerializedObject(property.serializedObject.targetObject);
                    EditorApplication.delayCall += SetTargetMethodOnEventDelayed;
                }

            }
            else
            {
                Debug.LogError("Only add the EventMethodTargetAttribute to UnityEvents also the generic ones");
            }
        }

        private void GetSubStringBetweenChars(string origin, char start, char end, out string fullMatch, out string insideEncapsulation)
        {
            var match = Regex.Match(origin, string.Format(@"\{0}(.*?)\{1}", start, end));
            fullMatch = match.Groups[0].Value;
            insideEncapsulation = match.Groups[1].Value;
        }

        private void AddDataToEventMethodTargeting(Rect position, SerializedProperty property, GUIContent label, UnityEngine.Object targetObject)
        {
            if (!_eventMethodTargetingAsset)
            {
                _eventMethodTargetingAsset = AssetDatabase.LoadAssetAtPath<EventMethodTargetingData>(
                      AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodTargetingData")[0]));
            }

            SerializedObject eventMethodTargeting = new SerializedObject(_eventMethodTargetingAsset);
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

                    EditorUtility.SetDirty(_eventMethodTargetingAsset);
                    eventMethodTargeting.ApplyModifiedProperties();
                }

                DrawEventTargeting(position, property, label);
            }
            else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);
                var component = prefab.GetComponent(targetObject.GetType());

                if (!component)
                {
                    component = prefab.GetComponentInChildren(targetObject.GetType());
                }

                if (component)
                {
                    bool isDataContained = CheckIfDataContained(methodTargetingDataArray, "None", component.GetInstanceID());

                    if (!isDataContained)
                    {
                        methodTargetingDataArray.arraySize++;

                        var eventMethodData = methodTargetingDataArray.GetArrayElementAtIndex(methodTargetingDataArray.arraySize - 1);

                        eventMethodData.FindPropertyRelative("_sceneGuid").stringValue = "None";
                        eventMethodData.FindPropertyRelative("_objectID").intValue = targetObject.GetInstanceID();

                        EditorUtility.SetDirty(_eventMethodTargetingAsset);
                        eventMethodTargeting.ApplyModifiedProperties();
                    }

                    DrawEventTargeting(position, property, label);
                }
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

                        EditorUtility.SetDirty(_eventMethodTargetingAsset);
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
            if (!_eventMethodTargetingAsset)
            {
                _eventMethodTargetingAsset = AssetDatabase.LoadAssetAtPath<EventMethodTargetingData>(
                      AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodTargetingData")[0]));
            }

            if (_eventMethodTargeting == null)
            {
                _eventMethodTargeting = new SerializedObject(_eventMethodTargetingAsset);
            }

            var methodTargetingDataArray = _eventMethodTargeting.FindProperty("_methodTargetingData");

            if (property.serializedObject.targetObject is ScriptableObject ||
                AssetDatabase.Contains((property.serializedObject.targetObject as MonoBehaviour).transform.root.gameObject))
            {
                SerializedProperty toBeDrawn = FindDataArrayElementForAsset(methodTargetingDataArray, property.serializedObject.targetObject);

                EditorGUI.BeginChangeCheck();

                if (toBeDrawn != null)
                {
                    EditorGUI.PropertyField(position, toBeDrawn, new GUIContent("TargetMethods"), true);
                }
                else
                {
                    GUI.enabled = false;
                    EditorGUI.TextField(position, "Initializing: Please re-inspect the asset.");
                    GUI.enabled = true;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_eventMethodTargetingAsset);
                }
            }
            else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath);
                SerializedProperty toBeDrawn = null;

                var components = prefab.GetComponents(property.serializedObject.targetObject.GetType()).ToList();

                components.AddRange(prefab.GetComponentsInChildren(property.serializedObject.targetObject.GetType()));

                for (int i = 0; i < components.Count; i++)
                {
                    toBeDrawn = FindDataArrayElementForAsset(methodTargetingDataArray, components[i]);

                    if (toBeDrawn != null)
                    {
                        break;
                    }
                }

                EditorGUI.BeginChangeCheck();

                if (toBeDrawn != null)
                {
                    EditorGUI.PropertyField(position, toBeDrawn, new GUIContent("TargetMethods"), true);
                }
                else
                {
                    GUI.enabled = false;
                    EditorGUI.TextField(position, "Initializing: Please re-inspect the GameObject.");
                    GUI.enabled = true;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_eventMethodTargetingAsset);
                }
            }
            else
            {
                int localId = GetLocalFileID(property);

                var sceneGuid = AssetDatabase.AssetPathToGUID((property.serializedObject.targetObject as MonoBehaviour).gameObject.scene.path);

                SerializedProperty toBeDrawn = FindPropertyFromSceneAssetWithID(methodTargetingDataArray, sceneGuid, localId);

                EditorGUI.BeginChangeCheck();

                if (toBeDrawn != null)
                {
                    EditorGUI.PropertyField(position, toBeDrawn, new GUIContent("TargetMethods"), true);
                }
                else
                {
                    GUI.enabled = false;
                    EditorGUI.TextField(position, "Initializing: Please re-inspect the GameObject.");
                    GUI.enabled = true;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_eventMethodTargetingAsset);
                }
            }
        }

        private int GetLocalFileID(SerializedProperty property)
        {
            SerializedObject serializedObject = new SerializedObject(property.serializedObject.targetObject);
            PropertyInfo inspectorModeInfo = typeof(SerializedObject).
                GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

            SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

            return localIdProp.intValue;
        }

        private SerializedProperty FindPropertyFromSceneAssetWithID(SerializedProperty methodTargetingDataArray, string sceneGuid, int objectID)
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

        private SerializedProperty FindDataArrayElementForAsset(SerializedProperty methodTargetingDataArray, UnityEngine.Object targetObject)
        {
            for (int i = methodTargetingDataArray.arraySize - 1; i >= 0; i--)
            {
                var sceneGuid = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneGuid").stringValue;
                var objectID = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_objectID").intValue;

                if (sceneGuid == "None" && objectID == targetObject.GetInstanceID())
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
                if (!_eventMethodTargetingAsset)
                {
                    _eventMethodTargetingAsset = AssetDatabase.LoadAssetAtPath<EventMethodTargetingData>(
                          AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodTargetingData")[0]));
                }

                var methodTargetingData = (EventMethodData[])_methodTargetingDataField.GetValue(_eventMethodTargetingAsset);

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
                    string guid = (string)_guidField.GetValue(methodTargetingData[i]);
                    int objectID = (int)_objectIDField.GetValue(methodTargetingData[i]);

                    if (guid == serializedObjectGuid && objectID == serializedObjectID)
                    {
                        targetMethods.AddRange((TargetMethodData[])_targetMethodsField.GetValue(methodTargetingData[i]));
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

        ~EventMethodTargetAttributePropertyDrawer()
        {
            Debug.Log("Closing property drawer and saving assets");

            _eventMethodTargeting.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            _eventMethodTargeting = null;
            _eventMethodTargetingAsset = null;
        }
    }
}