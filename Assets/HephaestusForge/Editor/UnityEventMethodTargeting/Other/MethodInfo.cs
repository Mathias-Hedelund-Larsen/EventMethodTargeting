using System;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public sealed class MethodInfo 
    {        
        public string ClassName { get; }
        public string MethodName { get; }
        public Type[] ArgumentsType { get; }
        public UnityEngine.Object Target { get; }

        public MethodInfo(UnityEngine.Object target, string className, string methodName, Type[] argumentsType)
        {
            Target = target;
            ClassName = className;
            MethodName = methodName;
            ArgumentsType = argumentsType;
        }

        public static implicit operator bool(MethodInfo source) => source != null;

        public static MethodInfo NoTarget()
        {
            return new MethodInfo(null, "No target", "No target", new Type[0]);
        }
    }
}