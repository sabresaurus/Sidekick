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
                    string requestId = br.ReadString();
                    if (EnumHelper.TryParse(br.ReadString(), out apiRequest))
                    {
                        if(apiRequest == APIRequest.GetHierarchy)
                        {
                            return new GetHierarchyResponse(br);
                        }
                        else if (apiRequest == APIRequest.GetGameObject)
                        {
                            return new GetGameObjectResponse(br);
                        }
                        else if(apiRequest == APIRequest.SetVariable)
                        {
                            return null;
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
