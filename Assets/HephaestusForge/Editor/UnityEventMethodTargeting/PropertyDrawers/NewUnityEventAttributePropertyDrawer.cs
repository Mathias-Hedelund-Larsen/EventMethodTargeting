using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace HephaestusForge.UnityEventMethodTargeting
{
    [CustomPropertyDrawer(typeof(EventMethodTargetAttribute))]
    public class NewUnityEventAttributePropertyDrawer : PropertyDrawer
    {
        private string _propertyName;
        private string _propertyPath;
        private Dictionary<string, ReorderableList> _initialized = new Dictionary<string, ReorderableList>();

        private ReorderableList Init(SerializedProperty property)
        {            
            Selection.selectionChanged -= OnLostInspectorFocus;
            Selection.selectionChanged += OnLostInspectorFocus;

            var callsProperty = property.FindPropertyRelative("m_PersistentCalls").FindPropertyRelative("m_Calls");

            var list = new ReorderableList(property.serializedObject, callsProperty);

            list.drawHeaderCallback = DrawHeader;
            list.drawElementCallback = DrawListElement;
            list.onAddDropdownCallback = OnAddClicked;
            list.onRemoveCallback = OnRemoveClicked;
            list.elementHeight = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 3;            

            return list;
        }

        private void DrawHeader(Rect rect)
        {
            GUI.enabled = false;
            EditorGUI.LabelField(rect, _propertyName);
            GUI.enabled = true;
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var persistantCallProperty = _initialized[_propertyPath].serializedProperty.GetArrayElementAtIndex(index);

            var targetProperty = persistantCallProperty.FindPropertyRelative("m_Target");
            var methodNameProperty = persistantCallProperty.FindPropertyRelative("m_MethodName");
            var listenerModeProperty = persistantCallProperty.FindPropertyRelative("m_Mode");
            var callStateProperty = persistantCallProperty.FindPropertyRelative("m_CallState");
            var argumentsProperty = persistantCallProperty.FindPropertyRelative("m_Arguments ");

            rect.height = EditorGUIUtility.singleLineHeight;

            DrawTopLine(rect, targetProperty, methodNameProperty, callStateProperty , listenerModeProperty);            

            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;

            DrawBottomLine(rect, targetProperty, argumentsProperty);
        }

        private void DrawBottomLine(Rect rect, SerializedProperty targetProperty, SerializedProperty argumentsProperty)
        {
            rect.width = rect.width / 3 - 5;

            EditorGUI.PropertyField(rect, targetProperty, new GUIContent(""));
        }

        private void DrawTopLine(Rect rect, SerializedProperty targetProperty, SerializedProperty methodNameProperty, SerializedProperty callStateProperty, 
            SerializedProperty listenerModeProperty)
        {
            rect.width = rect.width / 3 - 5;

            EditorGUI.PropertyField(rect, callStateProperty, new GUIContent(""));

            List<Type> acceptedParameterTypes = new List<Type>();
            
            acceptedParameterTypes.Add(typeof(int));
            acceptedParameterTypes.Add(typeof(float));
            acceptedParameterTypes.Add(typeof(bool));
            acceptedParameterTypes.Add(typeof(string));
            acceptedParameterTypes.Add(typeof(UnityEngine.Object));

            List<MethodInfo> dynamicMethods = new List<MethodInfo>();

            List<MethodInfo> persistentMethods = new List<MethodInfo>() { MethodInfo.NoTarget()};

            if (targetProperty.objectReferenceValue)
            {
                if (targetProperty.objectReferenceValue is ScriptableObject || targetProperty.objectReferenceValue is Component)
                {
                    var methods = targetProperty.objectReferenceValue.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).
                        Where(m => m.ReturnType == typeof(void)).ToArray();

                    if (methods.Length > 0)
                    {
                        for (int i = 0; i < methods.Length; i++)
                        {
                            var parameters = methods[i].GetParameters();

                            if (DoesTakeParameter(out int amount))
                            {
                                if(parameters.Length == amount)
                                {
                                    if(IsEventAndMethodParametersEqual(fieldInfo.FieldType, parameters))
                                    {
                                        dynamicMethods.Add(new MethodInfo(targetProperty.objectReferenceValue, $"{targetProperty.objectReferenceValue.GetType().ToString()}",
                                            $"{methods[i].Name}", methods[i].GetParameters()){ IsDynamic = true });
                                    }
                                }
                            }

                            if (parameters.Length == 0 || parameters.Length == 1 && acceptedParameterTypes.Contains(parameters[0].ParameterType))
                            {
                                persistentMethods.Add(new MethodInfo(targetProperty.objectReferenceValue, $"{targetProperty.objectReferenceValue.GetType().ToString()}",
                                    $"{methods[i].Name}", methods[i].GetParameters()));
                            }
                        }
                    }
                }
                else if (targetProperty.objectReferenceValue is GameObject)
                {
                    var components = (targetProperty.objectReferenceValue as GameObject).GetComponents<Component>();

                    for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                    {
                        var methods = components[componentIndex].GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).
                            Where(m => m.ReturnType == typeof(void)).ToArray();

                        if (methods.Length > 0)
                        {
                            for (int i = 0; i < methods.Length; i++)
                            {
                                var parameters = methods[i].GetParameters();

                                if (DoesTakeParameter(out int amount))
                                {
                                    if (parameters.Length == amount)
                                    {
                                        if (IsEventAndMethodParametersEqual(fieldInfo.FieldType, parameters))
                                        {
                                            dynamicMethods.Add(new MethodInfo(components[componentIndex], components[componentIndex].GetType().ToString(),
                                                $"{methods[i].Name}", methods[i].GetParameters()){ IsDynamic = true });
                                        }
                                    }
                                }

                                if (parameters.Length == 0 || parameters.Length == 1 && acceptedParameterTypes.Contains(parameters[0].ParameterType))
                                {
                                    persistentMethods.Add(new MethodInfo(components[componentIndex], components[componentIndex].GetType().ToString(),
                                        $"{methods[i].Name}", methods[i].GetParameters()));
                                }
                            }
                        }
                    }
                }
            }

            rect.x += rect.width + 5;
            rect.width *= 2;

            var methodInfo = persistentMethods.Find(m => m.Target == targetProperty.objectReferenceValue && m.MethodName == methodNameProperty.stringValue);

            if (!methodInfo)
            {
                methodInfo = persistentMethods[0];
            }            

            GenericMenu dropDownMenu = new GenericMenu();

            for (int i = 0; i < dynamicMethods.Count; i++)
            {
                var instance = dynamicMethods[i];
                instance.TargetProperty = targetProperty;
                instance.MethodNameProperty = methodNameProperty;
                instance.ListenerModeProperty = listenerModeProperty;
                dropDownMenu.AddItem(new GUIContent($"{dynamicMethods[i].ClassName}/{dynamicMethods[i].MethodName}"), false, ChosenMethod, instance);
            }

            for (int i = 0; i < persistentMethods.Count; i++)
            {
                var instance = persistentMethods[i];
                instance.TargetProperty = targetProperty;
                instance.MethodNameProperty = methodNameProperty;
                instance.ListenerModeProperty = listenerModeProperty;
                dropDownMenu.AddItem(new GUIContent($"{persistentMethods[i].ClassName}/{persistentMethods[i].MethodName}"), false, ChosenMethod, instance);
            }

            GUI.enabled = methodInfo.MethodName != "No target";

            if (EditorGUI.DropdownButton(rect, new GUIContent(methodInfo.MethodName), FocusType.Keyboard))
            {
                dropDownMenu.ShowAsContext();
            }

            GUI.enabled = true;
        }

        private bool IsEventAndMethodParametersEqual(Type fieldType, ParameterInfo[] parameters)
        {
            if(parameters.Length == 1)
            {
                return parameters.Select(p => p.ParameterType).SequenceEqual(fieldType.ParentTrueGeneric(typeof(UnityEvent<>)).GetGenericArguments());
            }
            else if (parameters.Length == 2)
            {
                return parameters.Select(p => p.ParameterType).SequenceEqual(fieldType.ParentTrueGeneric(typeof(UnityEvent<,>)).GetGenericArguments());
            }
            else if (parameters.Length == 3)
            {
                return parameters.Select(p => p.ParameterType).SequenceEqual(fieldType.ParentTrueGeneric(typeof(UnityEvent<,,>)).GetGenericArguments());
            }
            else
            {
                return parameters.Select(p => p.ParameterType).SequenceEqual(fieldType.ParentTrueGeneric(typeof(UnityEvent<,,,>)).GetGenericArguments());
            }
        }

        private void ChosenMethod(object methodInfo)
        {
            MethodInfo info = (MethodInfo)methodInfo;

            info.TargetProperty.objectReferenceValue = info.Target;
            info.MethodNameProperty.stringValue = info.MethodName;

            if (info.IsDynamic)
            {
                info.ListenerModeProperty.intValue = (int)PersistentListenerMode.EventDefined;
            }
            else
            {
                if(info.Arguments.Length == 0)
                {
                    info.ListenerModeProperty.intValue = (int)PersistentListenerMode.Void;
                }
                else if(info.Arguments[0].ParameterType == typeof(int))
                {
                    info.ListenerModeProperty.intValue = (int)PersistentListenerMode.Int;
                }
                else if (info.Arguments[0].ParameterType == typeof(float))
                {
                    info.ListenerModeProperty.intValue = (int)PersistentListenerMode.Float;
                }
                else if (info.Arguments[0].ParameterType == typeof(bool))
                {
                    info.ListenerModeProperty.intValue = (int)PersistentListenerMode.Bool;
                }
                else if (info.Arguments[0].ParameterType == typeof(string))
                {
                    info.ListenerModeProperty.intValue = (int)PersistentListenerMode.String;
                }
                else if (info.Arguments[0].ParameterType == typeof(UnityEngine.Object))
                {
                    info.ListenerModeProperty.intValue = (int)PersistentListenerMode.Object;
                }
            }

            info.ListenerModeProperty.serializedObject.ApplyModifiedProperties();
        }

        private void OnAddClicked(Rect buttonRect, ReorderableList list)
        {
            list.serializedProperty.arraySize++;

            list.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private void OnRemoveClicked(ReorderableList list)
        {
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _propertyName = property.displayName;
            _propertyPath = property.propertyPath;

            float propertyHeight = EditorGUIUtility.singleLineHeight;

            if (FieldTypeIsUnityEvent())
            {
                propertyHeight = 75;

                if (!_initialized.ContainsKey(_propertyPath))
                {
                    _initialized.Add(_propertyPath, Init(property));
                }

                int count = _initialized[_propertyPath].count - 1;

                count = count < 0 ? 0 : count;

                propertyHeight += count * (EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 5);
            }

            return propertyHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _propertyName = property.displayName;
            _propertyPath = property.propertyPath;            

            if (FieldTypeIsUnityEvent())
            {
                if (!_initialized.ContainsKey(_propertyPath))
                {
                    _initialized.Add(_propertyPath, Init(property));
                }

                _initialized[_propertyPath].DoList(position);
            }
        }

        private bool FieldTypeIsUnityEvent()
        {
            if(fieldInfo.FieldType == typeof(UnityEvent) || fieldInfo.FieldType.IsSubclassOf(typeof(UnityEvent)) || DoesTakeParameter(out int amount))
            {
                return true;
            }

            return false;
        }

        private bool DoesTakeParameter(out int parameterCount)
        {
            if (fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<>)))
            {
                parameterCount = 1;
                return true;
            }
            else if (fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,>)))
            {
                parameterCount = 2;
                return true;
            }
            else if (fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,>)))
            {
                parameterCount = 3;
                return true;
            }
            else if(fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,,>)))
            {
                parameterCount = 4;
                return true;
            }

            parameterCount = 0;
            return false;
        }

        private void OnLostInspectorFocus()
        {

        }
    }
}