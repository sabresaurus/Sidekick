using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.UI;

namespace Sabresaurus.Sidekick
{
    /// <summary>
    /// Helper to show a Sidekick remote's status on device using UGUI Text
    /// </summary>
    public class SidekickStatusDisplay : MonoBehaviour
    {
        [SerializeField]
        Text text;

        bool wasConnected = false;

        private void Start()
        {
            if (text != null)
            {
                text.text = "Not Connected";
            }
        }

        void Update()
        {
            if (text != null)
            {
                PlayerConnection playerConnection = PlayerConnection.instance;

                if (playerConnection.isConnected != wasConnected)
                {
                    if (playerConnection.isConnected)
                    {
                        text.text = "Connected";
                    }
                    else
                    {
                        text.text = "Not Connected";
                    }
                    wasConnected = playerConnection.isConnected;
                }
            }
        }
    }
}