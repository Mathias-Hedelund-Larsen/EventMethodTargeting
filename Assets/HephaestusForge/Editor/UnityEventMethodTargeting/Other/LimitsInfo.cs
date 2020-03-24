using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public sealed class LimitsInfo 
    {
        public Object Limiter { get; }
        public string TargetField { get; }
        public object FieldValue { get; }
        public SerializedProperty FieldName { get; }
        public PersistentListenerMode ListenerMode { get; }
        public SerializedProperty ArgumentsProperty { get; }

        public LimitsInfo(Object limiter, string targetField, object fieldValue, SerializedProperty fieldName, PersistentListenerMode listenerMode, 
            SerializedProperty argumentsProperty)
        {
            Limiter = limiter;
            TargetField = targetField;
            FieldValue = fieldValue;
            FieldName = fieldName;
            ListenerMode = listenerMode;
            ArgumentsProperty = argumentsProperty;
        }

        public static LimitsInfo NoTarget()
        {
            return new LimitsInfo(null, "No target.", null, null, PersistentListenerMode.Void, null);
        }
    }
}
