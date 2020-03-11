using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.UI.Slider;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        //[CustomPropertyDrawer(typeof(SliderEvent), true)]

        public class SliderEventPropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.PropertyField(position, property, true);
            }
        }
    }
}
