#if UNITY_EDITOR
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class BridgingContext : ScriptableObject
    {
        [System.Serializable]
        public class Container
        {
            SidekickSettings settings = new SidekickSettings();
            SelectionManager selectionManager = new SelectionManager();
            APIManager apiManager = new APIManager();

            public SelectionManager SelectionManager
            {
                get
                {
                    return selectionManager;
                }
            }

            public APIManager APIManager
            {
                get
                {
                    return apiManager;
                }
            }

            public SidekickSettings Settings
            {
                get
                {
                    return settings;
                }
            }
        }

        public void RefreshCallbacks()
        {
            container.SelectionManager.RefreshCallbacks();
        }

        #region Bridging
        static Container containerStaticCopy = null;

        public Container container = new Container();

        private static BridgingContext instance = null;

        public static BridgingContext Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CreateInstance<BridgingContext>();
                    if (containerStaticCopy != null)
                    {
                        instance.container = containerStaticCopy;
                    }
                }
                return instance;
            }
        }

        private void OnEnable()
        {
            instance = this;

            RefreshCallbacks();
        }

        private void OnDisable()
        {
            containerStaticCopy = container;
        }
        #endregion
    }
}
#endif