using UnityEngine;
using System;

namespace HephaestusForge.UnityEventMethodTargeting
{
    [AttributeUsage(AttributeTargets.Field)]
    public class EventMethodTargetAttribute : PropertyAttribute
    {
    }
}