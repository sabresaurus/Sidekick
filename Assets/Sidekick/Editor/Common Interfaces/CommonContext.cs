using UnityEngine;
using System.Collections;

namespace Sabresaurus.Sidekick
{
    [System.Serializable]
    public class CommonContext
    {
        SidekickSettings settings = new SidekickSettings();
        SelectionManager selectionManager;
        APIManager apiManager;

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

        public void OnEnable()
        {
            if (selectionManager == null)
            {
                selectionManager = new SelectionManager();
            }
            if (apiManager == null)
            {
                apiManager = new APIManager();
            }

            selectionManager.OnEnable(this);
            apiManager.OnEnable(this);
        }

        public void OnDisable()
        {
            selectionManager.OnDisable();
            apiManager.OnDisable();
        }

    }
}