using UnityEngine;

namespace HephaestusForge.UnityEventMethodTargeting
{
    public abstract class LimiterBase<T> : ScriptableObject 
    {
        [SerializeField]
        private T[] _fields;
    }
}