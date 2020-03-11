using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections;
using UnityEngine.Events;
using UnityEditorInternal;
using System.Collections.Generic;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        [CustomPropertyDrawer(typeof(UnityEngine.UI.Button.ButtonClickedEvent), true)]

        public class ButtonEventPropertyDrawer : PropertyDrawer
        {
            private SerializedObject _target;

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
                return new UnityEventDrawer().GetPropertyHeight(property, label) + 5;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                position.y += EditorGUIUtility.singleLineHeight / 2;

                EditorGUI.BeginChangeCheck();

                new UnityEventDrawer().OnGUI(position, property, label);

                if (property.serializedObject.targetObject is Component && (property.serializedObject.targetObject as Component).GetComponentInChildren<EventMethodTargetOnUIChild>() &&
                    EditorGUI.EndChangeCheck())
                {
                    _target = property.serializedObject;
                    EditorApplication.delayCall += SetPrivateMethodOnEvent;
                }
            }

            private void SetPrivateMethodOnEvent()
            {
                EditorApplication.delayCall -= SetPrivateMethodOnEvent;
                var methodTargets = (_target.targetObject as Component).GetComponentInChildren<EventMethodTargetOnUIChild>();

                for (int i = 0; i < methodTargets.TargetMethods.Length; i++)
                {
                    var methodTarget = methodTargets.TargetMethods[i];

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