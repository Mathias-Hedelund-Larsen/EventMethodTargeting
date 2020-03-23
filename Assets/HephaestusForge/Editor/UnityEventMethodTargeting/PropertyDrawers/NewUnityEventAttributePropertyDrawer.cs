using System;
using System.Collections;
using System.Collections.Generic;
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
            list.elementHeight = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;            

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

            DrawTopLine(rect, targetProperty, methodNameProperty, callStateProperty);            

            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(rect, targetProperty, new GUIContent(""));
        }

        private void DrawTopLine(Rect rect, SerializedProperty targetProperty, SerializedProperty methodNameProperty, SerializedProperty callStateProperty)
        {
            rect.width = rect.width / 3 - 5;

            EditorGUI.PropertyField(rect, callStateProperty, new GUIContent(""));

            List<string> methodNames = new List<string>() { "No target" };

            if (targetProperty.objectReferenceValue)
            {
                if (targetProperty.objectReferenceValue is ScriptableObject)
                {
                    var methods = targetProperty.objectReferenceValue.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (methods.Length > 0)
                    {
                        methodNames.Clear();

                        for (int i = 0; i < methods.Length; i++)
                        {
                            methodNames.Add($"{targetProperty.objectReferenceValue.name}.{methods[i].Name}");
                        }
                    }
                }
                else if (targetProperty.objectReferenceValue is GameObject)
                {
                    var components = (targetProperty.objectReferenceValue as GameObject).GetComponents<Component>();

                    for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                    {
                        var methods = components[componentIndex].GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        if (methods.Length > 0)
                        {
                            methodNames.Clear();

                            for (int i = 0; i < methods.Length; i++)
                            {
                                methodNames.Add($"{components[componentIndex].GetType().ToString()}.{methods[i].Name}");
                            }
                        }
                    }
                }
            }

            rect.x += rect.width + 5;
            rect.width *= 2;

            int methodNameIndex = methodNames.IndexOf(methodNameProperty.stringValue);

            if (methodNameIndex < 0) methodNameIndex = 0;

            methodNameProperty.stringValue = methodNames[EditorGUI.Popup(rect, methodNameIndex, methodNames.ToArray())];
        }

        private void OnAddClicked(Rect buttonRect, ReorderableList list)
        {
            list.serializedProperty.arraySize++;

            EditorUtility.SetDirty(list.serializedProperty.serializedObject.targetObject);
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
                propertyHeight = 70;

                if (!_initialized.ContainsKey(_propertyPath))
                {
                    _initialized.Add(_propertyPath, Init(property));
                }

                int count = _initialized[_propertyPath].count - 1;

                count = count < 0 ? 0 : count;

                propertyHeight += count * (EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing);
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
            if(fieldInfo.FieldType == typeof(UnityEvent) || fieldInfo.FieldType.IsSubclassOf(typeof(UnityEvent)) ||
                    fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,>)) ||
                    fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,,>)))
            {
                return true;
            }

            return false;
        }

        private void OnLostInspectorFocus()
        {

        }
    }
}