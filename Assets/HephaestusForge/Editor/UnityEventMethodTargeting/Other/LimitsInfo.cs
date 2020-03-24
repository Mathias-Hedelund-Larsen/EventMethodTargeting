using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public sealed class LimitsInfo 
    {        
        public Object Limiter { get; }
        public string TargetField { get; }
        public System.Object FieldValue { get; }
        public PersistentListenerMode ListenerMode { get; }
        public SerializedProperty ArgumentsProperty { get; }

        public LimitsInfo(Object limiter, string targetField, System.Object fieldValue, PersistentListenerMode listenerMode, SerializedProperty argumentsProperty)
        {
            Limiter = limiter;
            TargetField = targetField;
            FieldValue = fieldValue;
            ListenerMode = listenerMode;
            ArgumentsProperty = argumentsProperty;
        }

        public static LimitsInfo NoTarget()
        {
            return new LimitsInfo(null, "No target.", null, PersistentListenerMode.Void, null);
        }
    }
}
