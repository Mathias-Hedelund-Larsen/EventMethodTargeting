using System;
using UnityEngine;
using UnityEngine.Events;

namespace HephaestusForge
{
    namespace UnityEventMethodTargeting
    {
        [Serializable]
        public class TargetMethodData
        {
#pragma warning disable 0649

            [SerializeField]
            private string _methodName = "";

            [SerializeField]
            private SearchFor _searchFor;

            [SerializeField]
            private PersistentListenerMode _listenerMode = PersistentListenerMode.Int;

            [SerializeField]
            private string _limitationEnumType;

            [SerializeField]
            private string _onType = "";

            [SerializeField]
            private UnityEventValueLimit _limit = 0;

            [SerializeField]
            private UnityEngine.Object[] _valueObjects = new UnityEngine.Object[0];

            [SerializeField]
            private int[] _valueInts = new int[0];

            [SerializeField]
            private float[] _valueFloats = new float[0];

            [SerializeField]
            private string[] _valueStrings = new string[0];

            [SerializeField]
            private bool[] _valueBools = new bool[0];

#pragma warning restore 0649

            public string MethodName { get => _methodName; }
            public PersistentListenerMode ListenerMode { get => _listenerMode;  }
            public Type Ontype { get => Type.GetType(_onType); }
            public SearchFor SearchFor { get => _searchFor; }
            public UnityEventValueLimit Limit { get => _limit; }

            public object[] Limitation
            {
                get
                {
                    switch (_listenerMode)
                    {
                        case PersistentListenerMode.Object:
                            return _valueObjects;

                        case PersistentListenerMode.Int:
                                
                            object[] valueInts = new object[_valueInts.Length];

                            if (_limit == UnityEventValueLimit.Enum)
                            {
                                Type enumType = Type.GetType(_limitationEnumType);
                                var enumValuesArray = Enum.GetValues(enumType);
                                valueInts = new object[enumValuesArray.Length];

                                for (int i = 0; i < enumValuesArray.Length; i++) 
                                {
                                    valueInts[i] = (int)enumValuesArray.GetValue(i);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < valueInts.Length; i++)
                                {
                                    valueInts[i] = _valueInts[i];
                                }
                            }
                            
                            return valueInts;

                        case PersistentListenerMode.Float:
                            object[] valueFloats = new object[_valueFloats.Length];

                            for (int i = 0; i < valueFloats.Length; i++)
                            {
                                valueFloats[i] = _valueFloats[i];
                            }

                            return valueFloats;

                        case PersistentListenerMode.String:
                            
                            object[] valueStrings = new object[_valueStrings.Length];

                            if(_limit == UnityEventValueLimit.Enum)
                            {
                                Type enumType = Type.GetType(_limitationEnumType);
                                var enumNamesArray = Enum.GetNames(enumType);
                                valueStrings = new object[enumNamesArray.Length];

                                for (int i = 0; i < valueStrings.Length; i++)
                                {
                                    valueStrings[i] = (string)enumNamesArray.GetValue(i);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < _valueStrings.Length; i++)
                                {
                                    valueStrings[i] = _valueStrings[i];
                                }
                            }

                            return valueStrings;

                        case PersistentListenerMode.Bool:
                            object[] valueBools = new object[_valueBools.Length];

                            for (int i = 0; i < valueBools.Length; i++)
                            {
                                valueBools[i] = _valueBools[i];
                            }

                            return valueBools;
                    }

                    return new object[0];
                }
            }
        }
    }
}