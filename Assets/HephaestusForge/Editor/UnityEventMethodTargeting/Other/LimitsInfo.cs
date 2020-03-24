using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public sealed class LimitsInfo 
    {        
        public Object Limiter { get; }
        public string TargetField { get; }
        public PersistentListenerMode ListenerMode { get; }
        public SerializedProperty ArgumentsProperty { get; }

        public LimitsInfo(Object limiter, string targetField, PersistentListenerMode listenerMode, SerializedProperty argumentsProperty)
        {
            Limiter = limiter;
            TargetField = targetField;
            ListenerMode = listenerMode;
            ArgumentsProperty = argumentsProperty;
        }

        public static LimitsInfo NoTarget()
        {
            return new LimitsInfo(null, "No target.", PersistentListenerMode.Void, null);
        }
    }
}
