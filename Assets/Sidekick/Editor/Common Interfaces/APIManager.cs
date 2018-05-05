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
    public class APIManager : ICommonContextComponent
    {
        [NonSerialized] CommonContext commonContext;
        int lastRequestID = 0;

        public Action<BaseResponse> ResponseReceived;

        public void OnEnable(CommonContext commonContext)
		{
            this.commonContext = commonContext;


		}

        public void OnDisable()
        {
			
        }

        public int SendToPlayers(BaseRequest request)
        {
            lastRequestID++;
            if(commonContext.Settings.InspectionConnection == InspectionConnection.LocalEditor)
            {
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

                if (commonContext.Settings.LocalDevMode)
                {
                    byte[] testResponse = SidekickRequestProcessor.Process(bytes);
                    MessageEventArgs messageEvent = new MessageEventArgs();
                    messageEvent.data = testResponse;
                    BaseResponse response = SidekickResponseProcessor.Process(testResponse);
                    ResponseReceived(response);
                }
                else
                {
                    EditorConnection.instance.Send(RuntimeSidekick.kMsgSendEditorToPlayer, bytes);
                }
            }
			return lastRequestID;
        }
	}
}