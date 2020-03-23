using System;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public sealed class MethodInfo 
    {        
        public int ObjectID { get; }
        public string ClassName { get; }
        public string MethodName { get; }
        public Type[] ArgumentsType { get; }

        public MethodInfo(int objectID, string className, string methodName, Type[] argumentsType)
        {
            ObjectID = objectID;
            ClassName = className;
            MethodName = methodName;
            ArgumentsType = argumentsType;
        }
    }
}