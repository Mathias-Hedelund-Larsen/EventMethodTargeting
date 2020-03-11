using UnityEngine;
using System;

namespace FoldergeistAssets
{
    namespace UnityEventMethodTargeting
    {
        [AttributeUsage(AttributeTargets.Field)]
        public class EventMethodTargetAttribute : PropertyAttribute
        {
            public string UniqueID { get; }

            public EventMethodTargetAttribute(string uniqueID)
            {
                UniqueID = uniqueID;
            }
        }
    }
}