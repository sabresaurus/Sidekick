using System;

namespace Sabresaurus.Sidekick
{
    readonly struct SelectionInfo : IEquatable<SelectionInfo>
    {
        // Note we need to be able to differentiate selecting a RuntimeType from wanting to see static methods on the type it represents 
        public readonly Type Type;
        public readonly object Object;

        public SelectionInfo(Type type)
        {
            Type = type;
            Object = null;
        }

        public SelectionInfo(object o)
        {
            Object = o;
            Type = null;
        }

        public bool IsEmpty
        {
            get
            {
                if (Type != null) return false;
                
                // For UnityEngine.Object the backing object may be null so need to call the overriden equals operator to check that
                if (Object != null && !(Object is UnityEngine.Object castUnityObject && castUnityObject == null)) return false;
                
                return true;
            }
        }

        public string GetDisplayName()
        {
            if (Type != null)
            {
                return Type.Name;
            }

            if (Object != null)
            {
                return Object.ToString();
            }

            return "Unknown";
        }
            

        public bool Equals(SelectionInfo other)
        {
            return Type == other.Type && Equals(Object, other.Object);
        }

        public override bool Equals(object obj)
        {
            return obj is SelectionInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (Object != null ? Object.GetHashCode() : 0);
            }
        }
    }
}