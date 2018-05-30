using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public class BridgingContext : ScriptableObject
    {
        [System.Serializable]
        public class Container
        {
            public string a;
            public string b;
        }

        #region Bridging
        static Container containerStaticCopy = null;

        public Container container;

        private static BridgingContext instance = null;

        public static BridgingContext Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CreateInstance<BridgingContext>();
                    instance.container = containerStaticCopy;
                }
                return instance;
            }
        }

        private void OnEnable()
        {
            instance = this;
        }

        private void OnDisable()
        {
            containerStaticCopy = container;
        }
        #endregion
    }
}