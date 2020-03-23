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
        private string _enumType;

        [SerializeField]
        private UnityEngine.Object _targetOfEvent;

        [SerializeField]
        private string _methodName;
    }
}