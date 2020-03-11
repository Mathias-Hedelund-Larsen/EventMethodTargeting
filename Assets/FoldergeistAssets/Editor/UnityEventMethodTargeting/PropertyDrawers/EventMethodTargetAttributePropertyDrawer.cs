using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        [CustomPropertyDrawer(typeof(EventMethodTargetAttribute))]

        public class EventMethodTargetAttributePropertyDrawer : PropertyDrawer
        {
            private SerializedObject _target;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (fieldInfo.FieldType.IsSubclassOf(typeof(UnityEventBase)))
                {
                    return new UnityEventDrawer().GetPropertyHeight(property, label);
                }
                else
                {
                    return EditorGUIUtility.singleLineHeight;
                }
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (property.serializedObject.targetObject is ScriptableObject)
                {
                    if (fieldInfo.FieldType.IsSubclassOf(typeof(UnityEventBase)))
                    {
                        position.y += EditorGUIUtility.singleLineHeight / 2;

                        EditorGUI.BeginChangeCheck();
                        var eventProperty = new UnityEventDrawer();
                        eventProperty.OnGUI(position, property, label);

                        if (EditorGUI.EndChangeCheck())
                        {
                            _target = property.serializedObject;
                            EditorApplication.delayCall += SetPrivateMethodOnEvent;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Only use PrivateMethodTargetAttribute on UnityEvents and inherited classes");
                        EditorGUI.PropertyField(position, property);
                    }
                }
                else
                {
                    Debug.LogWarning("Only use PrivateMethodTargetAttribute in ScriptableObject classes use PrivateMethodTarget on Child gameobject for Component/GameObjects");

                    if (fieldInfo.FieldType.IsSubclassOf(typeof(UnityEventBase)))
                    {
                        position.y += EditorGUIUtility.singleLineHeight / 2;

                        EditorGUI.BeginChangeCheck();
                        var eventProperty = new UnityEventDrawer();
                        eventProperty.OnGUI(position, property, label);
                    }
                    else
                    {
                        Debug.LogWarning("Only use PrivateMethodTargetAttribute on UnityEvents and inherited classes");
                        EditorGUI.PropertyField(position, property);
                    }
                }
            }

            private void SetPrivateMethodOnEvent()
            {
                EditorApplication.delayCall -= SetPrivateMethodOnEvent;
                var castedAttribute = (attribute as EventMethodTargetAttribute);


                _target.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }
    }
}