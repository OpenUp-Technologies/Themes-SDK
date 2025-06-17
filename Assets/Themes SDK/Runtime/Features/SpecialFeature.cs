using System;
using System.Linq;
using UnityEngine;

namespace OpenUp.Environment.Features
{
    public class SpecialFeature : MonoBehaviour
    {
        public enum FeatureValueType : int
        {
            Bool,
            String,
            Vector3,
            Float,
            Int
        }
        
        [Serializable]
        public struct FeatureValue
        {
            public string name;
            public FeatureValueType type;
            
            public bool boolValue;
            public string stringValue;
            public Vector3 vector3Value;
            public float floatValue;
            public int intValue;

            public object Value => type switch
            {
                FeatureValueType.Bool => boolValue,
                FeatureValueType.String => stringValue,
                FeatureValueType.Vector3 => vector3Value,
                FeatureValueType.Float => floatValue,
                FeatureValueType.Int => intValue,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        [Tooltip("Used to add special features to a theme without requiring a new build. Generally used as an escape hatch to add one-off features")]
        [SerializeField] private string featureName;

        [Tooltip("Additional values you want to add to this feature.")]
        [SerializeField] private FeatureValue[] values;

        public string FeatureName => featureName;

        public bool TryGetValue<T>(string valueName, out T valueT)
        {
            valueT = default(T);
            FeatureValue value = values.FirstOrDefault(f => f.name == valueName);

            if (value.name == null)
                return false;

            if (value.Value is not T t)
                return false;

            valueT = t;

            return true;
        } 
    }
}