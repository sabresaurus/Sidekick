#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace Sabresaurus.Sidekick
{
    [System.Serializable]
    public class APIManager
    {
        int lastRequestID = 0;

        public Action<BaseResponse> ResponseReceived;


        public int SendToPlayers(BaseRequest request)
        {
            SidekickSettings settings = BridgingContext.Instance.container.Settings;
            lastRequestID++;
            if (settings.InspectionConnection == InspectionConnection.LocalEditor)
            {
                if (ResponseReceived != null)
                    ResponseReceived(request.GenerateResponse());
            }
            else
            {
                byte[] bytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write(lastRequestID);

                        bw.Write(request.GetType().Name);
                        request.Write(bw);
                    }
                    bytes = ms.ToArray();
                }
#if SIDEKICK_DEBUG
                if (settings.LocalDevMode)
                {
                    byte[] testResponse = SidekickRequestProcessor.Process(bytes);
                    MessageEventArgs messageEvent = new MessageEventArgs();
                    messageEvent.data = testResponse;
                    BaseResponse response = SidekickResponseProcessor.Process(testResponse);
                    if (ResponseReceived != null)
                        ResponseReceived(response);
                }
                else
#endif
                {
                    EditorConnection.instance.Send(RuntimeSidekickBridge.SEND_EDITOR_TO_PLAYER, bytes);
                }
            }
            return lastRequestID;
        }
    }
}
#endif