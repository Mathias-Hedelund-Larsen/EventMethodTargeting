using System;
using System.Collections;
using System.Collections.Generic;
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

            var list = new ReorderableList(property.serializedObject, property.FindPropertyRelative("m_PersistentCalls").FindPropertyRelative("m_Calls"));

            list.drawHeaderCallback = DrawHeader;
            list.drawElementCallback = DrawListElement;
            list.onAddDropdownCallback = OnAddClicked;
            list.onRemoveCallback = OnRemoveClicked;

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

            rect.width = rect.width / 3 - 5;

            EditorGUI.PropertyField(rect, callStateProperty);
        }

        private void OnAddClicked(Rect buttonRect, ReorderableList list)
        { 
        }

        private void OnRemoveClicked(ReorderableList list)
        {
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _propertyName = property.displayName;
            _propertyPath = property.propertyPath;
           
            if (FieldTypeIsUnityEvent())
            {
                if (!_initialized.ContainsKey(_propertyPath))
                {
                    _initialized.Add(_propertyPath, Init(property));
                }
            }

            return 0;
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

                _initialized[_propertyPath].DoLayoutList();
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