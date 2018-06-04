using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace Sabresaurus.Sidekick
{
    public enum InspectionConnection : byte { LocalEditor, RemotePlayer }

    public class RuntimeSidekickBridge : MonoBehaviour
    {
        public static readonly Guid SEND_EDITOR_TO_PLAYER = new Guid("8bc8811663b74007ab8f4868ad9f7cab");
        public static readonly Guid SEND_PLAYER_TO_EDITOR = new Guid("b05c5854acec4554bcef23fabe79959e");

        bool wasConnected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            // Can't wrap the whole method - see https://stackoverflow.com/questions/44655667/
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
            Debug.Log("Initializing Sidekick by auto-instantiating RuntimeSidekickBridge");
            GameObject newGameObject = new GameObject("RuntimeSidekickBridge", typeof(RuntimeSidekickBridge));
            DontDestroyOnLoad(newGameObject);
#endif
        }
        void Update()
        {
            PlayerConnection playerConnection = PlayerConnection.instance;

            if (playerConnection.isConnected != wasConnected)
            {
                if (playerConnection.isConnected)
                {
                    OnConnected();
                }
                else
                {
                    OnDisconnected();
                }
                wasConnected = playerConnection.isConnected;
            }
        }

        private void OnConnected()
        {
			PlayerConnection.instance.Register(SEND_EDITOR_TO_PLAYER, OnMessageReceived);
        }

        private void OnDisconnected()
        {
			PlayerConnection.instance.Unregister(SEND_EDITOR_TO_PLAYER, OnMessageReceived);
        }

        private void OnMessageReceived(MessageEventArgs args)
        {
            byte[] response = SidekickRequestProcessor.Process(args.data);
            PlayerConnection.instance.Send(SEND_PLAYER_TO_EDITOR, response);
        }

        public void Send()
        {
            Debug.Log("Sending from client");
            string message = "HelloFromPlayer";

            message += Application.platform.ToString();
            PlayerConnection.instance.Send(SEND_PLAYER_TO_EDITOR, Encoding.ASCII.GetBytes(message));
        }
    }
}