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
                    // Read size here
                    br.ReadInt32();

                    int requestId = br.ReadInt32();

                    if (requestId == -1) // Error?
                    {
                        throw new Exception(br.ReadString());
                    }
                    else
                    {
                        string requestType = br.ReadString();

#if DEBUG_RESPONSES
                        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, requestType + "Response.bytes"), input);
#endif
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
                            throw new NotSupportedException("RequestType name must end in Request for automated substitution: " + requestType);
                        }
                    }
                }
            }
        }
    }
}
