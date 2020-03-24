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
        public SerializedProperty LimiterProperty { get; }
        public PersistentListenerMode ListenerMode { get; }
        public SerializedProperty FieldNameProperty { get; }
        public SerializedProperty ArgumentsProperty { get; }

        public LimitsInfo(Object limiter, string targetField, object fieldValue, SerializedProperty limiterProperty, PersistentListenerMode listenerMode,
            SerializedProperty fieldNameProperty, SerializedProperty argumentsProperty)
        {
            Limiter = limiter;
            TargetField = targetField;
            FieldValue = fieldValue;
            LimiterProperty = limiterProperty;
            ListenerMode = listenerMode;
            FieldNameProperty = fieldNameProperty;
            ArgumentsProperty = argumentsProperty;
        }

        public static LimitsInfo NoTarget()
        {
            return new LimitsInfo(null, "No target.", null, null, PersistentListenerMode.Void, null, null);
        }
    }
}
