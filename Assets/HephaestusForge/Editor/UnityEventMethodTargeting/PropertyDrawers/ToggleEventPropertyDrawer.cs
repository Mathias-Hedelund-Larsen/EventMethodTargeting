﻿using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HephaestusForge.UnityEventMethodTargeting
{
    [CustomPropertyDrawer(typeof(Toggle.ToggleEvent), true)]
    public class ToggleEventPropertyDrawer : PropertyDrawer
    {
        private EventMethodTargetAttributePropertyDrawer _eventMethodDrawer = new EventMethodTargetAttributePropertyDrawer();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (_eventMethodDrawer.fieldInfo == null)
            {
                var fieldInfoProperty = typeof(PropertyDrawer).GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var fieldInfoAttribute = typeof(PropertyDrawer).GetField("m_Attribute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                fieldInfoProperty.SetValue(_eventMethodDrawer, typeof(Toggle).GetField("onValueChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                fieldInfoAttribute.SetValue(_eventMethodDrawer, new EventMethodTargetAttribute());
            }

            return _eventMethodDrawer.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_eventMethodDrawer.fieldInfo == null)
            {
                var fieldInfoProperty = typeof(PropertyDrawer).GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var fieldInfoAttribute = typeof(PropertyDrawer).GetField("m_Attribute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                fieldInfoProperty.SetValue(_eventMethodDrawer, typeof(Toggle).GetField("onValueChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                fieldInfoAttribute.SetValue(_eventMethodDrawer, new EventMethodTargetAttribute());
            }

            _eventMethodDrawer.OnGUI(position, property, label);
        }
    }
}