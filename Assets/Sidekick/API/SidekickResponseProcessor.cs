using UnityEngine;
using System.IO;
using Sabresaurus.Sidekick.Responses;

namespace Sabresaurus.Sidekick
{
    public static class SidekickResponseProcessor
    {
        public static BaseResponse Process(byte[] input)
        {
            APIRequest apiRequest;
            using (MemoryStream ms = new MemoryStream(input))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    int requestId = br.ReadInt32();
                    if (EnumHelper.TryParse(br.ReadString(), out apiRequest))
                    {
                        if(apiRequest == APIRequest.GetHierarchy)
                        {
                            return new GetHierarchyResponse(br, requestId);
                        }
                        else if (apiRequest == APIRequest.GetGameObject)
                        {
                            return new GetGameObjectResponse(br, requestId);
                        }
                        else if (apiRequest == APIRequest.SetVariable)
                        {
                            return new SetVariableResponse(br, requestId);
                        }
                        else if (apiRequest == APIRequest.InvokeMethod)
                        {
                            return new InvokeMethodResponse(br, requestId);
                        }
                        else if (apiRequest == APIRequest.GetUnityObjects)
                        {
                            return new GetUnityObjectsResponse(br, requestId);
                        }
                        else
                        {
                            throw new System.NotImplementedException();
                        }
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
