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
        private TargetMethodData[] _targetMethods;
    }
}