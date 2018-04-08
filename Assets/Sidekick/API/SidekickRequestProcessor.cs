using System;
using System.IO;
using Sabresaurus.Sidekick.Requests;
using Sabresaurus.Sidekick.Responses;
using UnityEngine;

namespace Sabresaurus.Sidekick
{
    public enum APIRequest
    {
        GetHierarchy,
        GetGameObject,
        SetVariable,
        InvokeMethod,
        GetUnityObjects,
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
                    string action = br.ReadString();
                    //Debug.Log(action);
                    if(EnumHelper.TryParse(action, out apiRequest))
                    {
						if (apiRequest == APIRequest.GetHierarchy)
						{
                            response = new GetHierarchyRequest().UncastResponse;
						}
						else if (apiRequest == APIRequest.GetGameObject)
						{
                            string path = br.ReadString();
                            int flags = br.ReadInt32();
							
                            response = new GetGameObjectRequest(path, (InfoFlags)flags).UncastResponse;
						}
                        else if (apiRequest == APIRequest.SetVariable)
                        {
                            int instanceID = br.ReadInt32();
                            WrappedVariable wrappedVariable = new WrappedVariable(br);

                            response = new SetVariableRequest(instanceID, wrappedVariable).UncastResponse;
                        }
                        else if (apiRequest == APIRequest.InvokeMethod)
                        {
                            int instanceID = br.ReadInt32();
                            string methodName = br.ReadString();
                            int parameterCount = br.ReadInt32();
                            WrappedVariable[] parameters = new WrappedVariable[parameterCount];
                            for (int i = 0; i < parameterCount; i++)
                            {
                                parameters[i] = new WrappedVariable(br);
                            }

                            response = new InvokeMethodRequest(instanceID, methodName, parameters).UncastResponse;
                        }
                        else if (apiRequest == APIRequest.GetUnityObjects)
                        {
                            string typeFullName = br.ReadString();
                            string assemblyName = br.ReadString();

                            response = new GetUnityObjectsRequest(typeFullName, assemblyName).UncastResponse;
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
