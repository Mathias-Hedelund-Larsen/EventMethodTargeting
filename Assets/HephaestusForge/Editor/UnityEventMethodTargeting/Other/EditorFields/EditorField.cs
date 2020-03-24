using UnityEngine;

namespace HephaestusForge.UnityEventMethodTargeting.Internal
{
    [System.Serializable]
    public abstract class EditorField<T>
    {
        [SerializeField]
        private string _fieldName;

        [SerializeField]
        private T _fieldValue;
    }
}