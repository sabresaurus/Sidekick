using System;
using System.IO;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public static class SidekickRequestProcessor
    {
        public static byte[] Process(byte[] input)
        {
            BaseResponse response = null;

            int requestId;
            string action;

            using (MemoryStream msIn = new MemoryStream(input))
            {
                using (BinaryReader br = new BinaryReader(msIn))
                {
                    requestId = br.ReadInt32();
                    action = br.ReadString();

                    if (action == typeof(GetHierarchyRequest).Name)
                    {
                        response = new GetHierarchyRequest(br).GenerateResponse();
                    }
                    else if (action == typeof(GetGameObjectRequest).Name)
                    {
                        response = new GetGameObjectRequest(br).GenerateResponse();
                    }
                    else if (action == typeof(SetVariableRequest).Name)
                    {
                        response = new SetVariableRequest(br).GenerateResponse();
                    }
                    else if (action == typeof(InvokeMethodRequest).Name)
                    {
                        response = new InvokeMethodRequest(br).GenerateResponse();
                    }
                    else if (action == typeof(GetUnityObjectsRequest).Name)
                    {
                        response = new GetUnityObjectsRequest(br).GenerateResponse();
                    }
                    else
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }

            byte[] bytes;
            using (MemoryStream msOut = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(msOut))
                {
                    bw.Write(requestId);
                    bw.Write(action);
                    response.Write(bw);
                }
                bytes = msOut.ToArray();
            }
            return bytes;
        }
    }
}
