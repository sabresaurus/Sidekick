using System;
using System.IO;
using Sabresaurus.EditorNetworking;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;

namespace Sabresaurus.Sidekick
{
    [System.Serializable]
    public class APIManager
    {
        int lastRequestID = 0;

        public Action<BaseResponse> ResponseReceived;


        public int SendToPlayers(BaseRequest request)
        {
            SidekickNetworkSettings networkSettings = BridgingContext.Instance.container.NetworkSettings;
            lastRequestID++;
            if (networkSettings.InspectionConnection == InspectionConnection.LocalEditor)
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

                    BaseResponse response = SidekickResponseProcessor.Process(testResponse);
                    if (ResponseReceived != null)
                        ResponseReceived(response);
                }
                else
#endif
                {
                    EditorMessaging.SendRequest(bytes);
                }
            }
            return lastRequestID;
        }
    }
}