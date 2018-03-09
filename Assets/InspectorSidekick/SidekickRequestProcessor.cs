using UnityEngine;
using Sabresaurus.Sidekick.Requests;
using System.IO;

/*
   * GetHierarchy
   * GetComponents
   * GetFields
   * SetField
   * 
   *
   */

namespace Sabresaurus.Sidekick
{
    public static class SidekickRequestProcessor
    {
        public static byte[] Process(byte[] input)
        {
            string response;

            string requestId;
            string action;

            using (MemoryStream ms = new MemoryStream(input))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    requestId = br.ReadString();
                    action = br.ReadString();

                    if (action.Equals("GetHierarchy"))
                    {
                        response = new GetHierarchyRequest().Response;
                    }
                    else if (action.Equals("GetGameObject"))
                    {
                        string path = br.ReadString();

                        response = new GetGameObjectRequest(path).Response;
                    }
                    else if (action.Equals("SetColor"))
                    {
                        Color color;
                        if (ColorUtility.TryParseHtmlString(br.ReadString(), out color))
                        {
                            Camera.main.backgroundColor = color;
                            response = "Color applied";
                        }
                        else
                        {
                            response = "Unrecognised color string";
                        }
                    }
                    else
                    {
                        response = "None Matched";
                    }
                }
            }

            byte[] bytes;
            using (MemoryStream msout = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(msout))
                {
                    bw.Write(action);
                    bw.Write(response);
                }
                bytes = msout.ToArray();
            }
            return bytes;
        }
    }
}
