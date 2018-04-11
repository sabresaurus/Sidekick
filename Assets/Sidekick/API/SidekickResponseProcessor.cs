using UnityEngine;
using System.IO;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;

namespace Sabresaurus.Sidekick
{
    public static class SidekickResponseProcessor
    {
        public static BaseResponse Process(byte[] input)
        {
            using (MemoryStream ms = new MemoryStream(input))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    int requestId = br.ReadInt32();
                    string action = br.ReadString();
                    if(action == typeof(GetHierarchyRequest).Name)
                    {
                        return new GetHierarchyResponse(br, requestId);
                    }
                    else if (action == typeof(GetGameObjectRequest).Name)
                    {
                        return new GetGameObjectResponse(br, requestId);
                    }
                    else if (action == typeof(SetVariableRequest).Name)
                    {
                        return new SetVariableResponse(br, requestId);
                    }
                    else if (action == typeof(InvokeMethodRequest).Name)
                    {
                        return new InvokeMethodResponse(br, requestId);
                    }
                    else if (action == typeof(GetUnityObjectsRequest).Name)
                    {
                        return new GetUnityObjectsResponse(br, requestId);
                    }
                    else
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
        }
    }
}
