using UnityEngine;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public abstract class LimiterBase<T> : ScriptableObject 
    {
        public T[] _Fields;
    }
}