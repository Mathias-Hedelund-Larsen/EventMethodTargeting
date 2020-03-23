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

            List<MethodInfo> methodNames = new List<MethodInfo>() { MethodInfo.NoTarget()};

            if (targetProperty.objectReferenceValue)
            {
                if (targetProperty.objectReferenceValue is ScriptableObject)
                {
                    var methods = targetProperty.objectReferenceValue.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).
                        Where(m => m.ReturnType == typeof(void)).ToArray();

                    if (methods.Length > 0)
                    {
                        methodNames.Clear();

                        for (int i = 0; i < methods.Length; i++)
                        {
                            methodNames.Add(new MethodInfo(targetProperty.objectReferenceValue, "", $"{targetProperty.objectReferenceValue.name}.{methods[i].Name}",
                                methods[i].GetParameters().Select(p => p.ParameterType).ToArray()));
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
                            methodNames.Clear();

                            for (int i = 0; i < methods.Length; i++)
                            {
                                methodNames.Add(new MethodInfo(components[componentIndex], components[componentIndex].GetType().ToString(), 
                                    $"{components[componentIndex].GetType().ToString()}.{methods[i].Name}", methods[i].GetParameters().Select(p => p.ParameterType).ToArray()));
                            }
                        }
                    }
                }
            }

            rect.x += rect.width + 5;
            rect.width *= 2;

            var methodInfo = methodNames.Find(m => m.Target == targetProperty.objectReferenceValue && m.MethodName == methodNameProperty.stringValue);
            int methodNameIndex = 0;

            if (methodInfo)
            {
                methodNameIndex = methodNames.IndexOf(methodInfo);
            }

            GenericMenu dropDownMenu = new GenericMenu();

            for (int i = 0; i < methodNames.Count; i++)
            {
                dropDownMenu.AddItem(new GUIContent($"{methodNames[i].ClassName}/{methodNames[i].MethodName}"), false, ChoseMethod, methodNames[i]);
            }

            if (EditorGUI.DropdownButton(rect, new GUIContent(methodInfo.MethodName), FocusType.Keyboard))
            {
                dropDownMenu.ShowAsContext();
            }
            //methodNameProperty.stringValue = methodNames[EditorGUI.Popup(rect, methodNameIndex, methodNames.Select(m => m.MethodName).ToArray())];
        }

        private void ChoseMethod(object methodInfo)
        {
            throw new NotImplementedException();
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
            if(fieldInfo.FieldType == typeof(UnityEvent) || fieldInfo.FieldType.IsSubclassOf(typeof(UnityEvent)) || DoesTakeParameter())
            {
                return true;
            }

            return false;
        }

        private bool DoesTakeParameter()
        {
            if(fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<>)) || fieldInfo.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,>)) ||
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