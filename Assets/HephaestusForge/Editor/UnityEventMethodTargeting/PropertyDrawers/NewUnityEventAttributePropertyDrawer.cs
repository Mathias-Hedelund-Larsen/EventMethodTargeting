using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public class NewUnityEventAttributePropertyDrawer : PropertyDrawer
    {
        private string _propertyPath;
        private Dictionary<string, ReorderableList> _initialized = new Dictionary<string, ReorderableList>();

        private ReorderableList Init(SerializedProperty property)
        {
            Selection.selectionChanged -= OnLostInspectorFocus;
            Selection.selectionChanged += OnLostInspectorFocus;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _propertyPath = property.propertyPath;

            if (!_initialized.ContainsKey(_propertyPath))
            {
                _initialized.Add(_propertyPath, Init(property));
            }

            if (FieldTypeIsUnityEvent())
            {

            }

            return 0;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _propertyPath = property.propertyPath;

            if (!_initialized.ContainsKey(_propertyPath))
            {
                _initialized.Add(_propertyPath, Init(property));
            }

            if (FieldTypeIsUnityEvent())
            {

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