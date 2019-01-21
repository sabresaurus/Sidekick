using System;
using System.IO;
using System.Text;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public static class SidekickRequestProcessor
    {
        public static byte[] Process(byte[] input)
        {
            try
            {
                BaseResponse response = null;

                int requestId;
                string requestType;

                using (MemoryStream msIn = new MemoryStream(input))
                {
                    using (BinaryReader br = new BinaryReader(msIn))
                    {
                        requestId = br.ReadInt32();
                        requestType = br.ReadString();

                        Type type = typeof(BaseRequest).Assembly.GetType("Sabresaurus.Sidekick.Requests." + requestType);

                        if (type != null && typeof(BaseRequest).IsAssignableFrom(type))
                        {
                            BaseRequest request = (BaseRequest)Activator.CreateInstance(type, br);

                            response = request.GenerateResponse();
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }

                byte[] responseBytes;
                using (MemoryStream msOut = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(msOut))
                    {
                        bw.Write(requestId);
                        bw.Write(requestType);
                        response.Write(bw);
                    }
                    responseBytes = msOut.ToArray();
                }

                byte[] wrappedBytes; // Including size prefix
                using (MemoryStream msOut = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(msOut))
                    {
                        // Size of the following (and itself)
                        bw.Write(sizeof(int) + responseBytes.Length);
                        bw.Write(responseBytes);
                    }
                    wrappedBytes = msOut.ToArray();
                }
                return wrappedBytes;
            }
            catch(Exception e)
            {
                byte[] responseBytes;
                using (MemoryStream msOut = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(msOut))
                    {
                        bw.Write(-1);
                        bw.Write(e.ToString());
                    }
                    responseBytes = msOut.ToArray();
                }

                byte[] wrappedBytes; // Including size prefix
                using (MemoryStream msOut = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(msOut))
                    {
                        // Size of the following (and itself)
                        bw.Write(sizeof(int) + responseBytes.Length);
                        bw.Write(responseBytes);
                    }
                    wrappedBytes = msOut.ToArray();
                }
                return wrappedBytes;
            }
        }
    }
}
