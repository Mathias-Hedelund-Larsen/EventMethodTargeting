using System;
using System.Reflection;
using UnityEditor;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public sealed class MethodInfo 
    {        
        public string ClassName { get; }
        public string MethodName { get; }
        public ParameterInfo[] Arguments { get; }
        public UnityEngine.Object Target { get; }

        public bool IsDynamic { get; set; }
        public SerializedProperty TargetProperty { get; set; }
        public SerializedProperty MethodNameProperty { get; set; }
        public SerializedProperty ListenerModeProperty { get; set; }

        public MethodInfo(UnityEngine.Object target, string className, string methodName, ParameterInfo[] arguments)
        {
            Target = target;
            ClassName = className;
            MethodName = methodName;
            Arguments = arguments;
        }

        public static implicit operator bool(MethodInfo source) => source != null;

        public static MethodInfo NoTarget()
        {
            return new MethodInfo(null, "No target", "No target", new ParameterInfo[0]);
        }
    }
}