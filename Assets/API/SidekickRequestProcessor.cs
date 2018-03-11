using System;
using System.IO;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    enum APIRequest
    {
        GetHierarchy,
        GetGameObject,
    }

    public static class SidekickRequestProcessor
    {
        public static byte[] Process(byte[] input)
        {
            BaseResponse response = null;

            string requestId;
            APIRequest apiRequest;

            using (MemoryStream msIn = new MemoryStream(input))
            {
                using (BinaryReader br = new BinaryReader(msIn))
                {
                    requestId = br.ReadString();
                    if(EnumHelper.TryParse(br.ReadString(), out apiRequest))
                    {
						if (apiRequest == APIRequest.GetHierarchy)
						{
                            response = new GetHierarchyRequest().UncastResponse;
						}
						else if (apiRequest == APIRequest.GetGameObject)
						{
							string path = br.ReadString();
							
                            response = new GetGameObjectRequest(path).UncastResponse;
						}
                    }
                }
            }

            byte[] bytes;
            using (MemoryStream msOut = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(msOut))
                {
                    bw.Write(requestId);
                    bw.Write(apiRequest.ToString());
                    response.Write(bw);
                }
                bytes = msOut.ToArray();
            }
            return bytes;
        }
    }
}
