using System;
using UnityEngine;

namespace HephaestusForge.UnityEventMethodTargeting
{
    [Serializable]
    public sealed class EventMethodData
    {
        [SerializeField]
        private string _sceneGuid;

        [SerializeField]
        private int _objectID;

        [SerializeField]
        private bool _limitByEnum;

        [SerializeField]
        private string _enumTypeValue;

        [SerializeField]
        private string _enumAssembly;

        [SerializeField]
        private string _propertyPath;

        [SerializeField]
        private string _guid;
    }
}