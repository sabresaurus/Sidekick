using UnityEngine;
using System.IO;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;
using System;

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
                    string requestType = br.ReadString();

                    if (requestType.EndsWith("Request", StringComparison.InvariantCulture))
                    {
                        string responseType = requestType.Replace("Request", "Response");
                        Type type = typeof(BaseResponse).Assembly.GetType("Sabresaurus.Sidekick.Responses." + responseType);
                        if (type != null && typeof(BaseResponse).IsAssignableFrom(type))
                        {
                            BaseResponse response = (BaseResponse)Activator.CreateInstance(type, br, requestId);
                            return response;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("RequestType name must end in Request for automated substitution");
                    }
                }
            }
        }
    }
}
