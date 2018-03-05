using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.UI;

namespace Sabresaurus.Sidekick
{
    public class RuntimeSidekick : MonoBehaviour
    {
        public static readonly Guid kMsgSendEditorToPlayer = new Guid("8bc8811663b74007ab8f4868ad9f7cab");
        public static readonly Guid kMsgSendPlayerToEditor = new Guid("b05c5854acec4554bcef23fabe79959e");

        [SerializeField]
        Text text;

        bool wasConnected = false;

        private void Start()
        {
            text.text = "Not Connected";
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
            text.text = "Connected";
            PlayerConnection.instance.Register(kMsgSendEditorToPlayer, OnMessageReceived);
        }

        private void OnDisconnected()
        {
            text.text = "Not Connected";
            PlayerConnection.instance.Unregister(kMsgSendEditorToPlayer, OnMessageReceived);
        }

        private void OnMessageReceived(MessageEventArgs args)
        {
            string decodedText = Encoding.ASCII.GetString(args.data);

            string response = SidekickRequestProcessor.Process(decodedText);
            PlayerConnection.instance.Send(kMsgSendPlayerToEditor, Encoding.ASCII.GetBytes(response));
        }

        public void Send()
        {
            Debug.Log("Sending from client");
            string message = "HelloFromPlayer";

            message += Application.platform.ToString();
            PlayerConnection.instance.Send(kMsgSendPlayerToEditor, Encoding.ASCII.GetBytes(message));
        }
    }
}