using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sabresaurus.Sidekick
{
    [Serializable]
    public class MethodSetup
    {
        public string MethodName = "";

        public object[] Values = new object[0];
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