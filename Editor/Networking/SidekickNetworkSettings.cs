using System;

namespace Sabresaurus.Sidekick
{
    [Serializable]
    public class SidekickNetworkSettings
    {
#if SIDEKICK_DEBUG
        public bool LocalDevMode = false;
#endif
        public bool AutoRefreshRemote = false;

        public InspectionConnection InspectionConnection = InspectionConnection.LocalEditor;
    }
}