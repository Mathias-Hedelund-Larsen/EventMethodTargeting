using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        [CustomPropertyDrawer(typeof(TargetMethodData))]

        public class TargetMethodDataPropertyDrawer : PropertyDrawer
        {
            private readonly Dictionary<UnityEventValueLimit, Action<Rect, SerializedProperty, PersistentListenerMode>> _drawLimitedField =
                new Dictionary<UnityEventValueLimit, Action<Rect, SerializedProperty, PersistentListenerMode>>()
                {
            {
                UnityEventValueLimit.None, (pos, property, listenerMode) =>
                {
                    GUI.enabled = false;
                    EditorGUI.TextField(pos, "Limitation", "None");
                    GUI.enabled = true;
                }
            },
            {
                UnityEventValueLimit.SingleValue, (pos, property, listenerMode) =>
                {
                    switch (listenerMode)
                    {
                        case PersistentListenerMode.Object:

                            var valueObject =  property.FindPropertyRelative("_valueObjects");
                            valueObject.arraySize = valueObject.arraySize == 0 ? 1 : valueObject.arraySize;
                            valueObject.GetArrayElementAtIndex(0).objectReferenceValue = EditorGUI.ObjectField(pos, "Limitation",
                                valueObject.GetArrayElementAtIndex(0).objectReferenceValue, typeof(UnityEngine.Object), true);

                        break;
                        case PersistentListenerMode.Int:

                            var valueInt = property.FindPropertyRelative("_valueInts");
                            valueInt.arraySize = valueInt.arraySize == 0 ? 1 : valueInt.arraySize;
                            valueInt.GetArrayElementAtIndex(0).intValue = EditorGUI.IntField(pos, "Limitation", valueInt.GetArrayElementAtIndex(0).intValue);

                        break;
                        case PersistentListenerMode.Float:

                            var valueFloat = property.FindPropertyRelative("_valueFloats");
                            valueFloat.arraySize = valueFloat.arraySize == 0 ? 1 : valueFloat.arraySize;
                            valueFloat.GetArrayElementAtIndex(0).floatValue = EditorGUI.FloatField(pos, "Limitation", valueFloat.GetArrayElementAtIndex(0).floatValue);

                        break;
                        case PersistentListenerMode.String:

                            var valueString = property.FindPropertyRelative("_valueStrings");
                            valueString.arraySize = valueString.arraySize == 0 ? 1 : valueString.arraySize;
                            valueString.GetArrayElementAtIndex(0).stringValue = EditorGUI.TextField(pos, "Limitation", valueString.GetArrayElementAtIndex(0).stringValue);

                        break;
                        case PersistentListenerMode.Bool:

                            var valueBool = property.FindPropertyRelative("_valueBools");
                            valueBool.arraySize = valueBool.arraySize == 0 ? 1 : valueBool.arraySize;
                            valueBool.GetArrayElementAtIndex(0).boolValue = EditorGUI.Toggle(pos, "Limitation", valueBool.GetArrayElementAtIndex(0).boolValue);

                        break;
                    }
                }
            },
            {
                UnityEventValueLimit.Array, (pos, property, listenerMode) =>
                {
                    switch (listenerMode)
                    {
                        case PersistentListenerMode.Object:

                            var valueObject =  property.FindPropertyRelative("_valueObjects");
                            valueObject.arraySize = EditorGUI.IntField(pos, "Limitation-Size", valueObject.arraySize);

                            for (int i = 0; i < valueObject.arraySize; i++)
                            {
                                pos.y += EditorGUIUtility.singleLineHeight + 5;
                                 valueObject.GetArrayElementAtIndex(i).objectReferenceValue = EditorGUI.ObjectField(pos, "LimitVal",
                                    valueObject.GetArrayElementAtIndex(i).objectReferenceValue, typeof(UnityEngine.Object), true);
                            }

                        break;
                        case PersistentListenerMode.Int:

                            var valueInt = property.FindPropertyRelative("_valueInts");
                            valueInt.arraySize = EditorGUI.IntField(pos, "Limitation-Size", valueInt.arraySize);

                            for (int i = 0; i < valueInt.arraySize; i++)
                            {
                                pos.y += EditorGUIUtility.singleLineHeight + 5;
                                valueInt.GetArrayElementAtIndex(i).intValue = EditorGUI.IntField(pos, "LimitVal", valueInt.GetArrayElementAtIndex(i).intValue);
                            }

                        break;
                        case PersistentListenerMode.Float:

                            var valueFloat = property.FindPropertyRelative("_valueFloats");
                            valueFloat.arraySize = EditorGUI.IntField(pos, "Limitation-Size", valueFloat.arraySize);

                            for (int i = 0; i < valueFloat.arraySize; i++)
                            {
                                pos.y += EditorGUIUtility.singleLineHeight + 5;
                                valueFloat.GetArrayElementAtIndex(i).floatValue = EditorGUI.FloatField(pos, "LimitVal", valueFloat.GetArrayElementAtIndex(i).floatValue);
                            }

                        break;
                        case PersistentListenerMode.String:

                            var valueString = property.FindPropertyRelative("_valueStrings");
                            valueString.arraySize = EditorGUI.IntField(pos, "Limitation-Size", valueString.arraySize);

                            for (int i = 0; i < valueString.arraySize; i++)
                            {
                                pos.y += EditorGUIUtility.singleLineHeight + 5;
                                valueString.GetArrayElementAtIndex(i).stringValue = EditorGUI.TextField(pos, "LimitVal", valueString.GetArrayElementAtIndex(i).stringValue);
                            }

                        break;
                        case PersistentListenerMode.Bool:

                            GUI.enabled = false;
                            EditorGUI.TextField(pos, "Limitation", "None: Doesnt make sense to limit bool to multiple values");
                            GUI.enabled = true;

                        break;
                    }
                }
            },
            {
                UnityEventValueLimit.Enum, (pos, property, listenerMode) =>
                {
                    List<string> typeNames = new List<string>();
                    List<string> displayTypeNames = new List<string>();

                    var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.FullName.Contains("Editor") && !a.FullName.Contains("firstpass")).
                    Where(a => a.FullName.Contains("Assembly-CSharp") || a.FullName.Contains("FoldergeistAssets")).ToList();

                    for (int i = 0; i < assemblies.Count; i++)
                    {
                        var assemblyClasses = assemblies[i].GetTypes();

                        for (int t = 0; t < assemblyClasses.Length; t++)
                        {
                            if (assemblyClasses[t].IsEnum)
                            {
                                string[] display = assemblyClasses[t].ToString().Split('.');

                                displayTypeNames.Add(display[display.Length - 1]);
                                typeNames.Add($"{assemblyClasses[t].FullName}, {assemblies[i].FullName.Split(',')[0]}");
                            }
                        }
                    }

                    var limitationEnumType = property.FindPropertyRelative("_limitationEnumType");

                    int index = typeNames.Contains(limitationEnumType.stringValue) ? typeNames.IndexOf(limitationEnumType.stringValue) : 0;

                    index = EditorGUI.Popup(pos, "LimitByEnum", index, displayTypeNames.ToArray());

                    limitationEnumType.stringValue = typeNames[index];
                }
            }
                };

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (property.isExpanded)
                {
                    int propertyHeight = 8;
                    var limit = (UnityEventValueLimit)property.FindPropertyRelative("_limit").enumValueIndex;

                    if(limit == UnityEventValueLimit.Array)
                    {
                        var listenerMode = (PersistentListenerMode)property.FindPropertyRelative("_listenerMode").enumValueIndex;

                        switch (listenerMode)
                        {
                            case PersistentListenerMode.Object:
                                propertyHeight += property.FindPropertyRelative("_valueObjects").arraySize;
                                break;
                            case PersistentListenerMode.Int:
                                propertyHeight += property.FindPropertyRelative("_valueInts").arraySize;
                                break;
                            case PersistentListenerMode.Float:
                                propertyHeight += property.FindPropertyRelative("_valueFloats").arraySize;
                                break;
                            case PersistentListenerMode.String:
                                propertyHeight += property.FindPropertyRelative("_valueStrings").arraySize;
                                break;
                        }
                    }

                    return EditorGUIUtility.singleLineHeight * propertyHeight + (EditorGUIUtility.singleLineHeight / 3.3333334f) * (propertyHeight - 8);
                }
                else
                {
                    return EditorGUIUtility.singleLineHeight;
                }
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (property.isExpanded)
                {
                    position.height = EditorGUIUtility.singleLineHeight;

                    EditorGUI.PropertyField(position, property);

                    position.y += EditorGUIUtility.singleLineHeight + 5;

                    List<string> typeNames = new List<string>();
                    List<string> displayTypeNames = new List<string>();
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.FullName.Contains("Editor") && !a.FullName.Contains("firstpass")).
                        Where(a => a.FullName.Contains("Assembly-CSharp") || a.FullName.Contains("FoldergeistAssets")).ToList();

                    var searchFor = property.FindPropertyRelative("_searchFor");

                    Type targetSubclassParent = typeof(Component);

                    if ((SearchFor)searchFor.enumValueIndex == SearchFor.Asset)
                    {
                        targetSubclassParent = typeof(ScriptableObject);
                    }

                    for (int i = 0; i < assemblies.Count; i++)
                    {
                        var assemblyClasses = assemblies[i].GetTypes();

                        for (int t = 0; t < assemblyClasses.Length; t++)
                        {
                            if (!assemblyClasses[t].IsGenericType && !assemblyClasses[t].IsAbstract && assemblyClasses[t].IsSubclassOf(targetSubclassParent))
                            {
                                string[] display = assemblyClasses[t].ToString().Split('.');

                                displayTypeNames.Add(display[display.Length - 1]);
                                typeNames.Add($"{assemblyClasses[t].FullName}, {assemblies[i].FullName.Split(',')[0]}");
                            }
                        }
                    }

                    List<string> availableSearchForValues = new List<string>();

                    foreach (SearchFor item in Enum.GetValues(typeof(SearchFor)))
                    {
                        if (item == SearchFor.ComponentInScene)
                        {
                            if (property.serializedObject.targetObject is Component)
                            {
                                var editingGO = (property.serializedObject.targetObject as Component).transform.parent.gameObject;

                                var sceneAssetGuids = AssetDatabase.FindAssets($"t:SceneAsset");
                                List<SceneAsset> sceneAssets = new List<SceneAsset>();

                                for (int i = 0; i < sceneAssetGuids.Length; i++)
                                {
                                    sceneAssets.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneAssetGuids[i])));
                                }

                                if (editingGO.scene != null && editingGO.scene.name != null && editingGO.gameObject.scene.name != string.Empty &&
                                    sceneAssets.Any(s => s.name == editingGO.gameObject.scene.name))
                                {
                                    availableSearchForValues.Add(item.ToString());
                                }
                            }
                        }
                        else
                        {
                            availableSearchForValues.Add(item.ToString());
                        }
                    }

                    searchFor.enumValueIndex = EditorGUI.Popup(position, "SearchFor", searchFor.enumValueIndex, availableSearchForValues.ToArray());
                    position.y += EditorGUIUtility.singleLineHeight + 5;

                    var listenerMode = property.FindPropertyRelative("_listenerMode");

                    EditorGUI.PropertyField(position, listenerMode, new GUIContent("ListenerMode"));

                    if ((PersistentListenerMode)listenerMode.enumValueIndex == PersistentListenerMode.Void)
                    {
                        property.FindPropertyRelative("_limit").enumValueIndex = (int)UnityEventValueLimit.None;
                    }
                    else if ((PersistentListenerMode)listenerMode.enumValueIndex != PersistentListenerMode.Int &&
                        (PersistentListenerMode)listenerMode.enumValueIndex != PersistentListenerMode.String)
                    {
                        if ((UnityEventValueLimit)property.FindPropertyRelative("_limit").enumValueIndex == UnityEventValueLimit.Enum)
                        {
                            property.FindPropertyRelative("_limit").enumValueIndex = 0;
                        }
                    }

                    position.y += EditorGUIUtility.singleLineHeight + 5;

                    var onTypeProperty = property.FindPropertyRelative("_onType");
                    int index = typeNames.Contains(onTypeProperty.stringValue) ? typeNames.IndexOf(onTypeProperty.stringValue) : 0;

                    index = EditorGUI.Popup(position, "OnType", index, displayTypeNames.ToArray());

                    if (typeNames.Count > index)
                    {
                        onTypeProperty.stringValue = typeNames[index];


                        position.y += EditorGUIUtility.singleLineHeight + 5;

                        var type = Type.GetType(onTypeProperty.stringValue);

                        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        List<string> availableMethods = new List<string>();

                        for (int i = 0; i < methods.Length; i++)
                        {
                            string accessibility = methods[i].IsPublic ? "Public" : "NonPublic";

                            switch ((PersistentListenerMode)listenerMode.enumValueIndex)
                            {
                                case PersistentListenerMode.Void:

                                    if (methods[i].GetParameters().Count() == 0 && methods[i].ReturnType == typeof(void))
                                    {
                                        availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                    }

                                    break;
                                case PersistentListenerMode.Object:

                                    if (methods[i].GetParameters().Count() == 1 && methods[i].GetParameters().Any(p => p.ParameterType.IsSubclassOf(typeof(UnityEngine.Object)) ||
                                        p.ParameterType == typeof(UnityEngine.Object)) && methods[i].ReturnType == typeof(void))
                                    {
                                        availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                    }

                                    break;
                                case PersistentListenerMode.Int:

                                    if (methods[i].GetParameters().Count() == 1 && methods[i].GetParameters().Any(p => p.ParameterType == typeof(int)) && 
                                        methods[i].ReturnType == typeof(void))
                                    {
                                        availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                    }

                                    break;
                                case PersistentListenerMode.Float:

                                    if (methods[i].GetParameters().Count() == 1 && methods[i].GetParameters().Any(p => p.ParameterType == typeof(float)) && 
                                        methods[i].ReturnType == typeof(void))
                                    {
                                        availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                    }

                                    break;
                                case PersistentListenerMode.String:

                                    if (methods[i].GetParameters().Count() == 1 && methods[i].GetParameters().Any(p => p.ParameterType == typeof(string)) && 
                                        methods[i].ReturnType == typeof(void))
                                    {
                                        availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                    }

                                    break;
                                case PersistentListenerMode.Bool:

                                    if (methods[i].GetParameters().Count() == 1 && methods[i].GetParameters().Any(p => p.ParameterType == typeof(bool)) && methods[i].ReturnType == typeof(void))
                                    {
                                        availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                    }

                                    break;
                                case PersistentListenerMode.EventDefined:

                                    var parentPath = property.propertyPath.Split('.')[0];
                                    string indexWithEncaptionlation;
                                    string indexPure;
                                    
                                    GetSubStringBetweenChars(property.propertyPath, '[', ']', out indexWithEncaptionlation, out indexPure);

                                    var methodTargetingDataProperty = property.serializedObject.FindProperty(parentPath);

                                    var eventMethodDataProperty = methodTargetingDataProperty.GetArrayElementAtIndex(int.Parse(indexPure));
                                    var sceneGuidProperty = eventMethodDataProperty.FindPropertyRelative("_sceneGuid");
                                    var objectIDProperty = eventMethodDataProperty.FindPropertyRelative("_objectID");

                                    List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

                                    if (sceneGuidProperty.stringValue != "None")
                                    {
                                        FindTargets(sceneGuidProperty, objectIDProperty, objects);
                                    }
                                    else
                                    {
                                        var obj = (UnityEngine.Object)typeof(UnityEngine.Object).
                                            GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static).
                                            Invoke(null, new object[] { objectIDProperty.intValue });

                                        if(obj is EventMethodTargetOnUIChild)
                                        {
                                            objects.AddRange((obj as EventMethodTargetOnUIChild).GetComponentsInParent<Selectable>());
                                        }
                                        else 
                                        {
                                            objects.Add(obj);
                                        }
                                    }

                                    for (int t = 0; t < objects.Count; t++)
                                    {
                                        var fields = objects[t].GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f =>
                                            f.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<>))).ToArray();

                                        for (int y = 0; y < fields.Length; y++)
                                        {
                                            if (methods[i].GetParameters().Count() == 1 && methods[i].GetParameters().
                                                Any(p => p.ParameterType == fields[y].FieldType.ParentTrueGeneric(typeof(UnityEvent<>)).GetGenericArguments()[0]) && 
                                                methods[i].ReturnType == typeof(void))
                                            {
                                                availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                            }
                                        }

                                        fields = objects[t].GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f =>
                                            f.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,>))).ToArray();

                                        for (int y = 0; y < fields.Length; y++)
                                        {
                                            var methodParameterTypes = methods[i].GetParameters().Select(p => p.ParameterType).ToArray();
                                            var eventParameterTypes = fields[y].FieldType.ParentTrueGeneric(typeof(UnityEvent<,>)).GetGenericArguments();

                                            if (methodParameterTypes.Length == eventParameterTypes.Length && methodParameterTypes.SequenceEqual(eventParameterTypes) && 
                                                methods[i].ReturnType == typeof(void))
                                            {
                                                availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                            }
                                        }

                                        fields = objects[t].GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f =>
                                           f.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,>))).ToArray();

                                        for (int y = 0; y < fields.Length; y++)
                                        {
                                            var methodParameterTypes = methods[i].GetParameters().Select(p => p.ParameterType).ToArray();
                                            var eventParameterTypes = fields[y].FieldType.ParentTrueGeneric(typeof(UnityEvent<,,>)).GetGenericArguments();

                                            if (methodParameterTypes.Length == eventParameterTypes.Length && methodParameterTypes.SequenceEqual(eventParameterTypes) && 
                                                methods[i].ReturnType == typeof(void))
                                            {
                                                availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                            }
                                        }

                                        fields = objects[t].GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f =>
                                           f.FieldType.IsSubclassOfRawGeneric(typeof(UnityEvent<,,,>))).ToArray();

                                        for (int y = 0; y < fields.Length; y++)
                                        {
                                            var methodParameterTypes = methods[i].GetParameters().Select(p => p.ParameterType).ToArray();
                                            var eventParameterTypes = fields[y].FieldType.ParentTrueGeneric(typeof(UnityEvent<,,,>)).GetGenericArguments();

                                            if (methodParameterTypes.Length == eventParameterTypes.Length && methodParameterTypes.SequenceEqual(eventParameterTypes) && 
                                                methods[i].ReturnType == typeof(void))
                                            {
                                                availableMethods.Add($"{accessibility}.{methods[i].Name}");
                                            }
                                        }
                                    }

                                    break;
                            }
                        }

                        var methodName = property.FindPropertyRelative("_methodName");

                        int methodNameIndex = availableMethods.Contains(methodName.stringValue) ? availableMethods.IndexOf(methodName.stringValue) : 0;

                        if (availableMethods.Count > 0)
                        {
                            methodNameIndex = EditorGUI.Popup(position, "MethodName", methodNameIndex, availableMethods.ToArray());
                            methodName.stringValue = availableMethods[methodNameIndex].Split('.')[1];
                        }
                        else
                        {
                            GUI.enabled = false;
                            EditorGUI.TextField(position, "No valid method");
                            GUI.enabled = true;
                        }
                    }

                    position.y += EditorGUIUtility.singleLineHeight + 5;

                    var limit = property.FindPropertyRelative("_limit");

                    List<string> availableLimitValues = new List<string>();

                    foreach (UnityEventValueLimit item in Enum.GetValues(typeof(UnityEventValueLimit)))
                    {
                        if ((PersistentListenerMode)listenerMode.enumValueIndex == PersistentListenerMode.Void)
                        {
                            if (item == UnityEventValueLimit.None)
                            {
                                availableLimitValues.Add(item.ToString());
                                break;
                            }
                        }
                        else if (item == UnityEventValueLimit.Enum)
                        {
                            if ((PersistentListenerMode)listenerMode.enumValueIndex == PersistentListenerMode.Int ||
                                (PersistentListenerMode)listenerMode.enumValueIndex == PersistentListenerMode.String)
                            {
                                availableLimitValues.Add(item.ToString());
                            }
                        }
                        else
                        {
                            availableLimitValues.Add(item.ToString());
                        }
                    }

                    position.x += 15;

                    var fullWidth = position.width;

                    position.width = 10;

                    if (EditorGUI.DropdownButton(position, new GUIContent(GetTexture()), FocusType.Keyboard, new GUIStyle() { fixedWidth = 50, border = new RectOffset(1, 1, 1, 1) }))
                    {
                        GenericMenu menu = new GenericMenu();

                        for (int i = 0; i < availableLimitValues.Count; i++)
                        {
                            var enumValue = (UnityEventValueLimit)Enum.Parse(typeof(UnityEventValueLimit), availableLimitValues[i]);

                            menu.AddItem(new GUIContent(availableLimitValues[i]), limit.enumValueIndex == (int)enumValue, () => SetProperty(limit, enumValue));
                        }

                        menu.ShowAsContext();
                    }

                    position.x += 3;

                    position.width = fullWidth - 20;

                    _drawLimitedField[(UnityEventValueLimit)limit.enumValueIndex].Invoke(position, property, (PersistentListenerMode)listenerMode.enumValueIndex);
                }
                else
                {
                    EditorGUI.PropertyField(position, property);
                }
            }

            private void FindTargets(SerializedProperty sceneGuidProperty, SerializedProperty objectIDProperty, List<UnityEngine.Object> objects)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuidProperty.stringValue);

                for (int sceneIndex = 0; sceneIndex < EditorSceneManager.sceneCount; sceneIndex++)
                {
                    if (EditorSceneManager.GetSceneAt(sceneIndex).path == scenePath)
                    {
                        var sceneObjects = EditorSceneManager.GetSceneAt(sceneIndex).GetRootGameObjects();

                        for (int sceneObjectIndex = 0; sceneObjectIndex < sceneObjects.Length; sceneObjectIndex++)
                        {
                            var components = sceneObjects[sceneObjectIndex].GetComponents<MonoBehaviour>().ToList();
                            components.AddRange(sceneObjects[sceneObjectIndex].GetComponentsInChildren<MonoBehaviour>());

                            for (int componentIndex = 0; componentIndex < components.Count; componentIndex++)
                            {
                                SerializedObject serializedObject = new SerializedObject(components[componentIndex]);
                                PropertyInfo inspectorModeInfo = typeof(SerializedObject).
                                    GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

                                inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                                SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                                int localId = localIdProp.intValue;

                                if (localId == objectIDProperty.intValue)
                                {
                                    if (components[componentIndex] is EventMethodTargetOnUIChild)
                                    {
                                        objects.Add(components[componentIndex].GetComponentInParent<Selectable>());
                                    }
                                    else
                                    {
                                        objects.Add(components[componentIndex]);
                                    }
                                    break;
                                }
                            }
                        }

                        break;
                    }
                }
            }

            private void SetProperty(SerializedProperty property, UnityEventValueLimit value)
            {
                if (property.enumValueIndex != (int)value)
                {
                    property.enumValueIndex = (int)value;

                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            private Texture GetTexture()
            {
                return (Texture)EditorGUIUtility.Load("icons/d__Popup.png");
            }

            private void GetSubStringBetweenChars(string origin, char start, char end, out string fullMatch, out string insideEncapsulation)
            {
                var match = Regex.Match(origin, string.Format(@"\{0}(.*?)\{1}", start, end));
                fullMatch = match.Groups[0].Value;
                insideEncapsulation = match.Groups[1].Value;
            }

            private void GetSubStringsBetweenChars(string origin, char start, char end, out string[] fullMatch, out string[] insideEncapsulation)
            {
                var matches = Regex.Matches(origin, string.Format(@"\{0}(.*?)\{1}", start, end));
                fullMatch = new string[matches.Count];
                insideEncapsulation = new string[matches.Count];

                for (int i = 0; i < matches.Count; i++)
                {
                    fullMatch[i] = matches[i].Groups[0].Value;

                    insideEncapsulation[i] = matches[i].Groups[1].Value;
                }
            }
        }
    }
}
