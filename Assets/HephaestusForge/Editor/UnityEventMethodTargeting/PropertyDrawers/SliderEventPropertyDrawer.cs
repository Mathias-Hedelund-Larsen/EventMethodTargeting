using UnityEditor;
using UnityEngine;
using System.Reflection;
using UnityEngine.UI;

namespace HephaestusForge.UnityEventMethodTargeting
{
    [CustomPropertyDrawer(typeof(Slider.SliderEvent), true)]

    public class SliderEventPropertyDrawer : PropertyDrawer
    {
        private NewUnityEventAttributePropertyDrawer _eventMethodDrawer = new NewUnityEventAttributePropertyDrawer();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (_eventMethodDrawer.fieldInfo == null)
            {
                var fieldInfoProperty = typeof(PropertyDrawer).GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var fieldInfoAttribute = typeof(PropertyDrawer).GetField("m_Attribute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                fieldInfoProperty.SetValue(_eventMethodDrawer, typeof(Slider).GetField("m_OnValueChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
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

                fieldInfoProperty.SetValue(_eventMethodDrawer, typeof(Button).GetField("m_OnClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                fieldInfoAttribute.SetValue(_eventMethodDrawer, new EventMethodTargetAttribute());
            }

            _eventMethodDrawer.OnGUI(position, property, label);
        }
    }
}