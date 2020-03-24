using System;
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
    public class EventMethodTargetAttributePropertyDrawer : PropertyDrawer
    {
        private float _xOffset = 5;
        private string _propertyName;
        private string _propertyPath;
        private string _callPropertyPath;
        private Dictionary<string, string> _initializedGuid = new Dictionary<string, string>();
        private Dictionary<string, ScriptableObject[]> _limiters = new Dictionary<string, ScriptableObject[]>();
        private Dictionary<string, Tuple<int, string, ReorderableList>> _initialized = new Dictionary<string, Tuple<int, string, ReorderableList>>();

        private static SerializedObject _eventMethod;
        private static Dictionary<Type, List<Enum>> _availableEnums;

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
            list.draggable = false;
            list.elementHeight = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 5;

            if(_availableEnums == null)
            {
                _availableEnums = new Dictionary<Type, List<Enum>>();
                GetEnumsInAssemblies();
            }

            if (_eventMethod == null)
            {
                _eventMethod = new SerializedObject(
                    AssetDatabase.LoadAssetAtPath<EventMethodDataHandler>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EventMethodDataHandler")[0])));
            }

            return list;
        }

        private void GetEnumsInAssemblies()
        {
            var assemblyDefinitions = AssetDatabase.FindAssets("t:asmdef");
            List<UnityEngine.Object> assemblyObjects = new List<UnityEngine.Object>();

            for (int i = 0; i < assemblyDefinitions.Length; i++)
            {
                assemblyObjects.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(assemblyDefinitions[i])));
            }

            for (int i = assemblyObjects.Count - 1; i >= 0; i--)
            {
                if (assemblyObjects[i].name.Contains("Editor") || assemblyObjects[i].name.Contains("Tests") || assemblyObjects[i].name.Contains("Analytics"))
                {
                    assemblyObjects.RemoveAt(i);
                }
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("Assembly-CSharp") && !a.FullName.Contains("Editor") ||
                assemblyObjects.Any(ao => ao.name.Contains(a.FullName)) || a.FullName.Contains("UnityEngine.CoreModule")).ToArray();

            for (int i = 0; i < assemblies.Length; i++)
            {
                var assemblyClasses = assemblies[i].GetTypes();

                for (int t = 0; t < assemblyClasses.Length; t++)
                {
                    if(assemblyClasses[t].Assembly.FullName.Contains("UnityEngine.CoreModule") && !assemblyClasses[t].FullName.Contains("KeyCode"))
                    {
                        continue;
                    }

                    if (assemblyClasses[t].IsEnum && assemblyClasses[t].GetEnumUnderlyingType() == typeof(int))
                    {
                        var enumValues = new List<Enum>();
                        var values = Enum.GetValues(assemblyClasses[t]);

                        for (int v = 0; v < values.Length; v++)
                        {
                            enumValues.Add((Enum)values.GetValue(v));
                        }

                        _availableEnums.Add(assemblyClasses[t], enumValues);
                    }
                }
            }
        }

        private void DrawHeader(Rect rect)
        {
            GUI.enabled = false;
            EditorGUI.LabelField(rect, _propertyName);
            GUI.enabled = true;
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var persistantCallProperty = _initialized[_propertyPath].Item3.serializedProperty.GetArrayElementAtIndex(index);
            _callPropertyPath = persistantCallProperty.propertyPath;

            if (!_limiters.ContainsKey(_callPropertyPath))
            {
                _limiters.Add(_callPropertyPath, new ScriptableObject[0]);
            }

            if (!_initializedGuid.ContainsKey(_callPropertyPath))
            {
                var arrayProperty = _eventMethod.FindProperty("_methodTargetingData");

                var eventMethodData = arrayProperty.FindInArray((sProp) =>
                {
                    return sProp.FindPropertyRelative("_sceneGuid").stringValue == _initialized[_propertyPath].Item2 &&
                        sProp.FindPropertyRelative("_objectID").intValue == _initialized[_propertyPath].Item1 && sProp.FindPropertyRelative("_propertyPath").stringValue ==
                        $"{_callPropertyPath}";
                }, out int value);

                if(value == -1)
                {
                    _initializedGuid.Add(_callPropertyPath, Guid.NewGuid().ToString());

                    var data = _eventMethod.FindProperty("_methodTargetingData");
                    data.arraySize++;

                    eventMethodData = data.GetArrayElementAtIndex(data.arraySize - 1);

                    eventMethodData.FindPropertyRelative("_objectID").intValue = _initialized[_propertyPath].Item1;
                    eventMethodData.FindPropertyRelative("_sceneGuid").stringValue = _initialized[_propertyPath].Item2;
                    eventMethodData.FindPropertyRelative("_propertyPath").stringValue = $"{_callPropertyPath}";
                    eventMethodData.FindPropertyRelative("_guid").stringValue = _initializedGuid[_callPropertyPath];

                    _eventMethod.ApplyModifiedProperties();
                }
                else
                {
                    _initializedGuid.Add(_callPropertyPath, eventMethodData.FindPropertyRelative("_guid").stringValue);
                }
            }

            var targetProperty = persistantCallProperty.FindPropertyRelative("m_Target");
            var methodNameProperty = persistantCallProperty.FindPropertyRelative("m_MethodName");
            var listenerModeProperty = persistantCallProperty.FindPropertyRelative("m_Mode");
            var callStateProperty = persistantCallProperty.FindPropertyRelative("m_CallState");
            var argumentsProperty = persistantCallProperty.FindPropertyRelative("m_Arguments");

            rect.height = EditorGUIUtility.singleLineHeight;

            DrawTopLine(rect, targetProperty, methodNameProperty, callStateProperty , listenerModeProperty);            

            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;

            DrawBottomLine(rect, targetProperty, argumentsProperty, listenerModeProperty, methodNameProperty);
        }

        private void DrawBottomLine(Rect rect, SerializedProperty targetProperty, SerializedProperty argumentsProperty, SerializedProperty listenerModeProperty, 
            SerializedProperty methodName)
        {
            rect.width = rect.width / 3 - 5;

            rect.x += _xOffset;

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(rect, targetProperty, new GUIContent(""));

            if (EditorGUI.EndChangeCheck())
            {
                listenerModeProperty.intValue = (int)PersistentListenerMode.Void;
                argumentsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = null;
                methodName.stringValue = "No target.";
            }

            rect.x += rect.width + 5;
            rect.width *= 2;

            if (targetProperty.objectReferenceValue)
            {
                PersistentListenerMode mode = (PersistentListenerMode)listenerModeProperty.intValue;
                var arrayProperty = _eventMethod.FindProperty("_methodTargetingData");
                var eventMethodData = arrayProperty.FindInArray((sProp) => sProp.FindPropertyRelative("_guid").stringValue == _initializedGuid[_callPropertyPath], out int index);

                EditorGUI.BeginChangeCheck();

                switch (mode)
                {
                    case PersistentListenerMode.Object:
                        DrawChoseLimit(rect, eventMethodData, argumentsProperty, targetProperty, methodName, mode);
                        break;
                    case PersistentListenerMode.Int:
                        IntOrStringDraw(rect, eventMethodData, argumentsProperty, mode);
                        break;
                    case PersistentListenerMode.Float:
                        DrawChoseLimit(rect, eventMethodData, argumentsProperty, targetProperty, methodName, mode);
                        break;
                    case PersistentListenerMode.String:
                        IntOrStringDraw(rect, eventMethodData, argumentsProperty, mode);
                        break;
                    case PersistentListenerMode.Bool:
                        EditorGUI.PropertyField(rect, argumentsProperty.FindPropertyRelative("m_BoolArgument"), new GUIContent(""));
                        break;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    targetProperty.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                listenerModeProperty.intValue = (int)PersistentListenerMode.Void;
            }
        }

        private void DrawChoseLimit(Rect rect, SerializedProperty eventMethodData, SerializedProperty argumentsProperty, SerializedProperty target, SerializedProperty methodName,
            PersistentListenerMode mode)
        {
            if (_limiters[_callPropertyPath].Length == 0)
            {
                switch (mode)
                {
                    case PersistentListenerMode.Object:
                        var objectLimiters = AssetDatabase.FindAssets("t:ObjectLimiter");

                        _limiters[_callPropertyPath] = new ScriptableObject[objectLimiters.Length];

                        for (int i = 0; i < objectLimiters.Length; i++)
                        {
                            _limiters[_callPropertyPath][i] = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(objectLimiters[i]));
                        }
                        break;
                    case PersistentListenerMode.Float:
                        var floatLimiters = AssetDatabase.FindAssets("t:FloatLimiter");

                        _limiters[_callPropertyPath] = new ScriptableObject[floatLimiters.Length];

                        for (int i = 0; i < floatLimiters.Length; i++)
                        {
                            _limiters[_callPropertyPath][i] = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(floatLimiters[i]));
                        }
                        break;
                }
            }

            if(_limiters[_callPropertyPath].Length > 0)
            {
                var width = rect.width;
                rect.width = 20;

                var limit = eventMethodData.FindPropertyRelative("_limit");

                if (EditorGUI.DropdownButton(rect, new GUIContent(GetTexture()), FocusType.Keyboard, new GUIStyle() { fixedWidth = 50, border = new RectOffset(1, 1, 1, 1) }))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Unlimited"), limit.intValue == (int)UnityEventValueLimit.Unlimited, () => SetLimitation(limit, UnityEventValueLimit.Unlimited));
                    menu.AddItem(new GUIContent("Array limit"), limit.intValue == (int)UnityEventValueLimit.Array, () => SetLimitation(limit, UnityEventValueLimit.Array));

                    menu.ShowAsContext();
                }

                rect.x += 20;
                rect.width = width - 20;

                if (limit.intValue == (int)UnityEventValueLimit.Array)
                {
                    SerializedProperty[] fieldsProperty = new SerializedProperty[_limiters[_callPropertyPath].Length];

                    for (int i = 0; i < _limiters[_callPropertyPath].Length; i++)
                    {
                        fieldsProperty[i] = new SerializedObject(_limiters[_callPropertyPath][i]).FindProperty("_fields");
                    }

                    DrawArrayLimiterPropertyField(rect, eventMethodData, argumentsProperty, mode, fieldsProperty, target, methodName);
                }
                else
                {
                    EditorGUI.BeginChangeCheck();

                    switch (mode)
                    {
                        case PersistentListenerMode.Object:
                            var methodParameterType = target.objectReferenceValue.GetType().GetMethod(methodName.stringValue, BindingFlags.Instance | BindingFlags.Public
                                | BindingFlags.NonPublic).GetParameters()[0].ParameterType;

                            var targetObj = EditorGUI.ObjectField(rect, new GUIContent(""), argumentsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue,
                                methodParameterType, !target.objectReferenceValue.IsAsset());

                            argumentsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = targetObj;

                            if (targetObj)
                            {
                                argumentsProperty.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = targetObj.GetType().AssemblyQualifiedName;
                            }

                            break;

                        case PersistentListenerMode.Float:
                            EditorGUI.PropertyField(rect, argumentsProperty.FindPropertyRelative("m_FloatArgument"), new GUIContent(""));
                            break;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(argumentsProperty.serializedObject.targetObject);
                        argumentsProperty.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                switch (mode)
                {
                    case PersistentListenerMode.Object:
                        var methodParameterType = target.objectReferenceValue.GetType().GetMethod(methodName.stringValue, BindingFlags.Instance | BindingFlags.Public
                            | BindingFlags.NonPublic).GetParameters()[0].ParameterType;

                        var targetObj = EditorGUI.ObjectField(rect, new GUIContent(""), argumentsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue,
                                methodParameterType, !target.objectReferenceValue.IsAsset());

                        argumentsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = targetObj;

                        if (targetObj)
                        {
                            argumentsProperty.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = targetObj.GetType().AssemblyQualifiedName;
                        }

                        break;

                    case PersistentListenerMode.Float:
                        EditorGUI.PropertyField(rect, argumentsProperty.FindPropertyRelative("m_FloatArgument"), new GUIContent(""));
                        break;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(argumentsProperty.serializedObject.targetObject);
                    argumentsProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void IntOrStringDraw(Rect rect, SerializedProperty eventMethodData, SerializedProperty argumentsProperty, PersistentListenerMode mode)
        {
            var width = rect.width;
            rect.width = 20;            

            var limit = eventMethodData.FindPropertyRelative("_limit");

            if (_limiters[_callPropertyPath].Length == 0)
            {
                if (mode == PersistentListenerMode.Int)
                {
                    var intLimiters = AssetDatabase.FindAssets("t:IntLimiter");

                    _limiters[_callPropertyPath] = new ScriptableObject[intLimiters.Length];

                    for (int i = 0; i < intLimiters.Length; i++)
                    {
                        _limiters[_callPropertyPath][i] = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(intLimiters[i]));
                    }
                }
                else
                {
                    var stringLimiters = AssetDatabase.FindAssets("t:StringLimiter");

                    _limiters[_callPropertyPath] = new ScriptableObject[stringLimiters.Length];

                    for (int i = 0; i < stringLimiters.Length; i++)
                    {
                        _limiters[_callPropertyPath][i] = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(stringLimiters[i]));
                    }
                }
            }

            if (EditorGUI.DropdownButton(rect, new GUIContent(GetTexture()), FocusType.Keyboard, new GUIStyle() { fixedWidth = 50, border = new RectOffset(1, 1, 1, 1) }))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Unlimited"), limit.intValue == (int)UnityEventValueLimit.Unlimited, () => SetLimitation(limit, UnityEventValueLimit.Unlimited));
                menu.AddItem(new GUIContent("Enum limit"), limit.intValue == (int)UnityEventValueLimit.Enum, () => SetLimitation(limit, UnityEventValueLimit.Enum));

                if (_limiters[_callPropertyPath].Length > 0)
                {
                    menu.AddItem(new GUIContent("Array limit"), limit.intValue == (int)UnityEventValueLimit.Array, () => SetLimitation(limit, UnityEventValueLimit.Array));
                }

                menu.ShowAsContext();
            }

            rect.x += 20;
            rect.width = width - 20;

            if ((UnityEventValueLimit)limit.intValue == UnityEventValueLimit.Enum)
            {
                DrawEnumPropertyField(rect, eventMethodData, argumentsProperty, mode);
            }
            else if((UnityEventValueLimit)limit.intValue == UnityEventValueLimit.Array)
            {
                SerializedProperty[] fieldsProperty = new SerializedProperty[_limiters[_callPropertyPath].Length];

                for (int i = 0; i < _limiters[_callPropertyPath].Length; i++)
                {
                    fieldsProperty[i] = new SerializedObject(_limiters[_callPropertyPath][i]).FindProperty("_fields");
                }

                DrawArrayLimiterPropertyField(rect, eventMethodData, argumentsProperty, mode, fieldsProperty);
            }
            else
            {
                if(mode == PersistentListenerMode.Int)
                {
                    EditorGUI.PropertyField(rect, argumentsProperty.FindPropertyRelative("m_IntArgument"), new GUIContent(""));
                }
                else
                {
                    EditorGUI.PropertyField(rect, argumentsProperty.FindPropertyRelative("m_StringArgument"), new GUIContent(""));
                }
            }
        }

        private void DrawArrayLimiterPropertyField(Rect rect, SerializedProperty eventMethodData, SerializedProperty argumentsProperty, PersistentListenerMode mode,
            SerializedProperty[] limits, SerializedProperty target = null, SerializedProperty methodName = null)
        {
            var limiterProperty = eventMethodData.FindPropertyRelative("_limiter");

            if (limiterProperty.objectReferenceValue)
            {
                if (!limits.Any(l => l.serializedObject.targetObject == limiterProperty.objectReferenceValue))
                {
                    limiterProperty.objectReferenceValue = null;
                }
            }

            List<LimitsInfo> limitsInfos = new List<LimitsInfo>() { LimitsInfo.NoTarget() };
            var fieldNameProperty = eventMethodData.FindPropertyRelative("_enumTypeValue");

            for (int i = 0; i < limits.Length; i++)
            {
                for (int t = 0; t < limits[i].arraySize; t++)
                {
                    var limitations = limits[i].GetArrayElementAtIndex(t);

                    if (mode != PersistentListenerMode.Object)
                    {
                        limitsInfos.Add(new LimitsInfo(limitations.serializedObject.targetObject, limitations.FindPropertyRelative("_fieldName").stringValue,
                            limitations.FindPropertyRelative("_fieldValue").GetCurrentValue(), limiterProperty, mode, fieldNameProperty, argumentsProperty));
                    }
                    else
                    {
                        object currentVal = limitations.FindPropertyRelative("_fieldValue").GetCurrentValue();
                        var parameterType = target.objectReferenceValue.GetType().GetMethod(methodName.stringValue, BindingFlags.Instance | BindingFlags.Public
                            | BindingFlags.NonPublic).GetParameters()[0].ParameterType;                        

                        if (currentVal.GetType() == parameterType)
                        {
                            limitsInfos.Add(new LimitsInfo(limitations.serializedObject.targetObject, limitations.FindPropertyRelative("_fieldName").stringValue,
                                currentVal, limiterProperty, mode, fieldNameProperty, argumentsProperty));
                        }
                        else if (currentVal is GameObject)
                        {
                            var components = (currentVal as GameObject).GetComponents(parameterType).ToList();

                            components.AddRange(components[0].GetComponentsInChildren(parameterType));

                            for (int c = 0; c < components.Count; c++)
                            {
                                limitsInfos.Add(new LimitsInfo(limitations.serializedObject.targetObject, 
                                    $"{limitations.FindPropertyRelative("_fieldName").stringValue}.{components[i].name}[{i}]",
                                    components[i], limiterProperty, mode, fieldNameProperty, argumentsProperty));
                            }
                        }
                    }
                }
            }

            GenericMenu dropDownMenu = new GenericMenu();

            for (int i = 0; i < limitsInfos.Count; i++)
            {
                var instance = limitsInfos[i];

                if (instance.Limiter)
                {
                    dropDownMenu.AddItem(new GUIContent($"{instance.Limiter.name}/{instance.TargetField}"), false, SetLimits, instance);
                }
            }


            if (!limitsInfos.Any(lI => lI.TargetField == fieldNameProperty.stringValue))
            {
                fieldNameProperty.stringValue = limitsInfos[0].TargetField;
            }

            if (EditorGUI.DropdownButton(rect, new GUIContent($"{(fieldNameProperty.stringValue == "No target." ? "" : $"{limiterProperty.objectReferenceValue.name}.")}" +
                $"{fieldNameProperty.stringValue}"), FocusType.Keyboard))
            {
                dropDownMenu.ShowAsContext();
            }

        }

        private void SetLimits(object limits)
        {
            LimitsInfo info = (LimitsInfo)limits;

            info.FieldNameProperty.stringValue = info.TargetField;
            info.LimiterProperty.objectReferenceValue = info.Limiter;

            switch (info.ListenerMode)
            {
                case PersistentListenerMode.Object:
                    info.ArgumentsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = (UnityEngine.Object)info.FieldValue;
                    break;
                case PersistentListenerMode.Int:
                    info.ArgumentsProperty.FindPropertyRelative("m_IntArgument").intValue = (int)info.FieldValue;
                    break;
                case PersistentListenerMode.Float:
                    info.ArgumentsProperty.FindPropertyRelative("m_FloatArgument").floatValue = (float)info.FieldValue;
                    break;
                case PersistentListenerMode.String:
                    info.ArgumentsProperty.FindPropertyRelative("m_StringArgument").stringValue = (string)info.FieldValue;
                    break;
            }

            info.FieldNameProperty.serializedObject.ApplyModifiedProperties();
            info.ArgumentsProperty.serializedObject.ApplyModifiedProperties();
        }

        private void DrawEnumPropertyField(Rect rect, SerializedProperty eventMethodData, SerializedProperty argumentsProperty, PersistentListenerMode mode)
        {
            var enumTypeValueProperty = eventMethodData.FindPropertyRelative("_enumTypeValue");
            var enumAssemblyProperty = eventMethodData.FindPropertyRelative("_enumAssembly");

            if (string.IsNullOrEmpty(enumTypeValueProperty.stringValue) && string.IsNullOrEmpty(enumAssemblyProperty.stringValue))
            {
                var enumType = _availableEnums.Keys.ToArray()[0];
                enumTypeValueProperty.stringValue = $"{enumType.Name}.{_availableEnums[enumType][0]}";
                enumAssemblyProperty.stringValue = enumType.Assembly.FullName;
            }

            if (EditorGUI.DropdownButton(rect, new GUIContent(enumTypeValueProperty.stringValue), FocusType.Keyboard))
            {
                GenericMenu dropDownMenu = new GenericMenu();

                for (int i = 0; i < 31; i++)  //user defined layers start with layer 8 and unity supports 31 layers
                {
                    var layerName = LayerMask.LayerToName(i); //get the name of the layer

                    if (layerName.Length > 0) //only add the layer if it has been named
                    {
                        dropDownMenu.AddItem(new GUIContent($"UnityEngine.Layers/{layerName}"), false, ChoseLayerVal,
                            new Tuple<int, PersistentListenerMode, SerializedProperty, SerializedProperty, SerializedProperty>(i, mode,
                            argumentsProperty, enumTypeValueProperty, enumAssemblyProperty));
                    }
                }

                if (mode == PersistentListenerMode.String)
                {
                    var tags = InternalEditorUtility.tags;

                    for (int i = 0; i < tags.Length; i++)
                    {
                        dropDownMenu.AddItem(new GUIContent($"UnityEngine.Tag/{tags[i]}"), false, ChoseTagVal,
                                new Tuple<int, PersistentListenerMode, SerializedProperty, SerializedProperty, SerializedProperty>(i, mode,
                                argumentsProperty, enumTypeValueProperty, enumAssemblyProperty));
                    }
                }

                foreach (var item in _availableEnums)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        dropDownMenu.AddItem(new GUIContent($"{item.Key.Name}/{item.Value[i].ToString()}"), false, ChoseEnumVal, 
                            new Tuple<Type, Enum, PersistentListenerMode, SerializedProperty, SerializedProperty, SerializedProperty>
                            (item.Key, item.Value[i], mode, argumentsProperty, enumTypeValueProperty, enumAssemblyProperty));
                    }
                }

                dropDownMenu.ShowAsContext();
            }
        }

        private void ChoseTagVal(object tuple)
        {
            var data = (Tuple<int, PersistentListenerMode, SerializedProperty, SerializedProperty, SerializedProperty>)tuple;

            data.Item3.FindPropertyRelative("m_StringArgument").stringValue = InternalEditorUtility.tags[data.Item1];
            data.Item4.stringValue = $"Tag.{InternalEditorUtility.tags[data.Item1]}";

            data.Item5.stringValue = "UnityEngine";

            data.Item3.serializedObject.ApplyModifiedProperties();
            data.Item4.serializedObject.ApplyModifiedProperties();
        }

        private void ChoseLayerVal(object tuple)
        {
            var data = (Tuple<int, PersistentListenerMode, SerializedProperty, SerializedProperty, SerializedProperty>)tuple;

            if(data.Item2 == PersistentListenerMode.Int)
            {
                data.Item3.FindPropertyRelative("m_IntArgument").intValue = data.Item1;
                data.Item4.stringValue = $"Layer.{LayerMask.LayerToName(data.Item1)}";
            }
            else
            {
                data.Item3.FindPropertyRelative("m_StringArgument").stringValue = LayerMask.LayerToName(data.Item1);
                data.Item4.stringValue = $"Layer.{LayerMask.LayerToName(data.Item1)}";
            }

            data.Item5.stringValue = "UnityEngine";

            data.Item3.serializedObject.ApplyModifiedProperties();
            data.Item4.serializedObject.ApplyModifiedProperties();
        }

        private void ChoseEnumVal(object tuple)
        {
            var data = (Tuple<Type, Enum, PersistentListenerMode, SerializedProperty, SerializedProperty, SerializedProperty>)tuple;

            if(data.Item3 == PersistentListenerMode.Int)
            {
                data.Item4.FindPropertyRelative("m_IntArgument").intValue = (int)Enum.ToObject(data.Item1, data.Item2);
            }
            else
            {
                data.Item4.FindPropertyRelative("m_StringArgument").stringValue = Enum.ToObject(data.Item1, data.Item2).ToString();
            }

            data.Item5.stringValue = $"{data.Item1.Name}.{data.Item2}";
            data.Item6.stringValue = data.Item1.Assembly.FullName;

            data.Item4.serializedObject.ApplyModifiedProperties();
            data.Item5.serializedObject.ApplyModifiedProperties();
        }

        private void SetLimitation(SerializedProperty limit, UnityEventValueLimit value)
        {
            limit.intValue = (int)value;
            _eventMethod.ApplyModifiedProperties();
        }

        private Texture GetTexture()
        {
            return (Texture)EditorGUIUtility.Load("icons/d__Popup.png");
        }

        private void DrawTopLine(Rect rect, SerializedProperty targetProperty, SerializedProperty methodNameProperty, SerializedProperty callStateProperty, 
            SerializedProperty listenerModeProperty)
        {
            rect.width = rect.width / 3 - 5;
            rect.x += _xOffset;

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
                                            $"{(methods[i].IsPublic ? "Public." : "NonPublic.")}{methods[i].Name}", methods[i].GetParameters()){ IsDynamic = true });
                                    }
                                }
                            }

                            if (parameters.Length == 0 || parameters.Length == 1 && (acceptedParameterTypes.Contains(parameters[0].ParameterType) || 
                                parameters[0].ParameterType.IsSubclassOf(typeof(UnityEngine.Object))))
                            {
                                persistentMethods.Add(new MethodInfo(targetProperty.objectReferenceValue, $"{targetProperty.objectReferenceValue.GetType().ToString()}",
                                    $"{(methods[i].IsPublic ? "Public." : "NonPublic.")}{methods[i].Name}", methods[i].GetParameters()));
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
                                                $"{(methods[i].IsPublic ? "Public." : "NonPublic.")}{methods[i].Name}", methods[i].GetParameters()){ IsDynamic = true });
                                        }
                                    }
                                }

                                if (parameters.Length == 0 || parameters.Length == 1 && (acceptedParameterTypes.Contains(parameters[0].ParameterType) ||
                                    parameters[0].ParameterType.IsSubclassOf(typeof(UnityEngine.Object))))
                                {
                                    persistentMethods.Add(new MethodInfo(components[componentIndex], components[componentIndex].GetType().ToString(),
                                        $"{(methods[i].IsPublic ? "Public." : "NonPublic.")}{methods[i].Name}", methods[i].GetParameters()));
                                }
                            }
                        }
                    }
                }
            }

            rect.x += rect.width + 5;
            rect.width *= 2;

            var methodInfo = persistentMethods.Find(m => m.Target == targetProperty.objectReferenceValue && m.MethodName.Split('.')[1] == methodNameProperty.stringValue);

            if(listenerModeProperty.intValue == (int)PersistentListenerMode.EventDefined)
            {
                methodInfo = dynamicMethods.Find(m => m.Target == targetProperty.objectReferenceValue && m.MethodName.Split('.')[1] == methodNameProperty.stringValue);
            }

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
                dropDownMenu.AddItem(new GUIContent($"{dynamicMethods[i].ClassName}/Dynamic/{dynamicMethods[i].MethodName}"), false, ChosenMethod, instance);
            }

            for (int i = 0; i < persistentMethods.Count; i++)
            {
                var instance = persistentMethods[i];

                if (instance.MethodName != "No target.")
                {
                    instance.TargetProperty = targetProperty;
                    instance.MethodNameProperty = methodNameProperty;
                    instance.ListenerModeProperty = listenerModeProperty;
                    string submenu = dynamicMethods.Count > 0 ? "Persistant/" : string.Empty;
                    dropDownMenu.AddItem(new GUIContent($"{persistentMethods[i].ClassName}/{submenu}{persistentMethods[i].MethodName}"), false, ChosenMethod, instance);
                }
            }

            GUI.enabled = (persistentMethods.Count > 1 || dynamicMethods.Count > 0);

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
            _limiters[_callPropertyPath] = new ScriptableObject[0];

            MethodInfo info = (MethodInfo)methodInfo;

            info.TargetProperty.objectReferenceValue = info.Target;
            info.MethodNameProperty.stringValue = info.MethodName.Split('.')[1];

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
                else if (info.Arguments[0].ParameterType == typeof(UnityEngine.Object) || info.Arguments[0].ParameterType.IsSubclassOf(typeof(UnityEngine.Object)))
                {
                    info.ListenerModeProperty.intValue = (int)PersistentListenerMode.Object;
                }
            }

            EditorUtility.SetDirty(info.ListenerModeProperty.serializedObject.targetObject);
            info.ListenerModeProperty.serializedObject.ApplyModifiedProperties();
        }

        private void OnAddClicked(Rect buttonRect, ReorderableList list)
        {
            list.serializedProperty.arraySize++;

            list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize -1).
                FindPropertyRelative("m_CallState").intValue = (int)UnityEventCallState.RuntimeOnly;

            list.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private void OnRemoveClicked(ReorderableList list)
        {            
            list.serializedProperty.DeleteArrayElementAtIndex(list.index);
            list.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _propertyName = property.displayName;
            _propertyPath = property.propertyPath;

            float propertyHeight = EditorGUIUtility.singleLineHeight;

            if (FieldTypeIsUnityEvent())
            {
                propertyHeight = 80;

                if (!_initialized.ContainsKey(_propertyPath))
                {
                    property.serializedObject.targetObject.GetSceneGuidAndObjectID(out string sceneGuid, out int objectID);
                    _initialized.Add(_propertyPath, new Tuple<int, string, ReorderableList>(objectID, sceneGuid, Init(property)));
                }

                int count = _initialized[_propertyPath].Item3.count - 1;

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
                    property.serializedObject.targetObject.GetSceneGuidAndObjectID(out string sceneGuid, out int objectID);
                    _initialized.Add(_propertyPath, new Tuple<int, string, ReorderableList>(objectID, sceneGuid, Init(property)));
                }

                _initialized[_propertyPath].Item3.DoList(position);
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
            _availableEnums = null;
            _eventMethod = null;
        }
    }
}