using UnityEngine;
using System;

namespace HephaestusForge
{
    namespace UnityEventMethodTargeting
    {
        [AttributeUsage(AttributeTargets.Field)]
        public class EventMethodTargetAttribute : PropertyAttribute
        {
        }
    }
}