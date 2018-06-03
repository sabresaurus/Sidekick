#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

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
                // If no instance reference cached, first of all try to get any instance Unity knows about
                if (instance == null)
                {
                    instance = Resources.FindObjectsOfTypeAll<BridgingContext>().FirstOrDefault();
                }

                // That didn't work, so let's make a new one!
                if (instance == null)
                {
                    instance = CreateInstance<BridgingContext>();
                    if (containerStaticCopy != null)
                    {
                        instance.container = containerStaticCopy;
                    }
                }

                // We should definitely have a valid instance to return
                return instance;
            }
        }

        private void OnEnable()
        {
            Assert.IsTrue(instance == null || instance == this);
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