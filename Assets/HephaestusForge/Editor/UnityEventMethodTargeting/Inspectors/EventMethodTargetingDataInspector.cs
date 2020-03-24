using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HephaestusForge.UnityEventMethodTargeting
{
    [CustomEditor(typeof(EventMethodDataHandler))]
    public class EventMethodTargetingDataInspector : Editor
    {
        private MonoScript _script;
        private SerializedObject _target;
        private List<Object> _references = new List<Object>();

        private void OnEnable()
        {
            _script = target.GetScript();
            _target = new SerializedObject(target);

            var methodTargetingDataArray = _target.FindProperty("_methodTargetingData");
            List<int> indexesToClear = new List<int>();

            for (int i = methodTargetingDataArray.arraySize - 1; i >= 0; i--)
            {
                var sceneGuid = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneGuid").stringValue;
                var objectID = methodTargetingDataArray.GetArrayElementAtIndex(i).FindPropertyRelative("_objectID").intValue;

                Object obj = UnityEditorObjectExtensions.GetObjectByInstanceID(objectID, sceneGuid);

                if (!obj)
                {
                    indexesToClear.Add(i);
                }
                else
                {
                    _references.Add(obj);
                }
            }

            if (indexesToClear.Count > 0)
            {
                for (int i = 0; i < indexesToClear.Count; i++)
                {
                    methodTargetingDataArray.DeleteArrayElementAtIndex(indexesToClear[i]);
                }

                _target.ApplyModifiedProperties();

                AssetDatabase.SaveAssets();
            }
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", _script, typeof(MonoScript), false);
            EditorGUILayout.LabelField("MethodTargetingData");
            EditorGUI.indentLevel += 1;
            EditorGUILayout.IntField("Size", _references.Count);

            for (int i = 0; i < _references.Count; i++)
            {
                EditorGUILayout.ObjectField("Reference", _references[i], typeof(Object), false);
            }

            GUI.enabled = true;
        }
    }
}