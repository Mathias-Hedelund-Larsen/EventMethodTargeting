using System;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public static class TypeExtensions
    {
        public static bool IsSubclassOfRawGeneric(this Type source, Type generic)
        {
            while (source != null && source != typeof(object))
            {
                var current = source.IsGenericType ? source.GetGenericTypeDefinition() : source;

                if (generic == current)
                {
                    return true;
                }

                source = source.BaseType;
            }

            return false;
        }

        public static Type ParentTrueGeneric(this Type source, Type generic)
        {
            while (source != null && source != typeof(object))
            {
                var current = source.IsGenericType ? source.GetGenericTypeDefinition() : source;

                if (generic == current)
                {
                    return source;
                }

                source = source.BaseType;
            }

            return null;
        }
    }
}
