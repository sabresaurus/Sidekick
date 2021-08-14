using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    [Serializable]
    public class MethodSetup : ISerializationCallbackReceiver
    {
        public string MethodIdentifier = "";

        public object[] Values = new object[0];

        public Type[] GenericArguments = new Type[0];

        private string[] GenericArgumentsString;

        public void OnBeforeSerialize()
        {
            // Only simple types can be serialised, so convert the type to an assembly-qualified type name
            GenericArgumentsString = new string[GenericArguments.Length];
            for (int i = 0; i < GenericArguments.Length; i++)
            {
                if (GenericArguments[i] != null)
                {
                    GenericArgumentsString[i] = GenericArguments[i].FullName + ", " + GenericArguments[i].Assembly.FullName;
                }
            }
        }

        public void OnAfterDeserialize()
        {
            GenericArguments = new Type[GenericArgumentsString.Length];
            for (int i = 0; i < GenericArgumentsString.Length; i++)
            {
                if (!string.IsNullOrEmpty(GenericArgumentsString[i]))
                {
                    // Convert the assembly-qualified type name back to a proper type
                    GenericArguments[i] = Type.GetType(GenericArgumentsString[i]);
                }
            }
        }
    }

    /// <summary>
    /// This class allows various utilities to store data that will persist through recompiles
    /// </summary>
    [Serializable]
    public class PersistentData
    {
        // MethodPane
        public List<MethodSetup> ExpandedMethods = new List<MethodSetup>();
        public HashSet<string> ExpandedFields = new HashSet<string>();
    }
}