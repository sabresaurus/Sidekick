using System.Collections.Generic;

namespace Sabresaurus.Sidekick
{
    /// <summary>
    /// This class allows various utilities to store data that will persist through recompiles
    /// </summary>
    [System.Serializable]
    public class PersistentData
    {
        // MethodPane
        public List<MethodSetup> ExpandedMethods = new List<MethodSetup>();
    }
}