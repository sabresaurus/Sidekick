using UnityEngine;
using System.Collections;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public abstract class BaseResponse
    {
        int requestID;

        public int RequestID
        {
            get
            {
                return requestID;
            }
        }

        public BaseResponse()
        {

        }

        public BaseResponse(BinaryReader br, int requestID)
        {
            this.requestID = requestID;
        }

        public abstract void Write(BinaryWriter bw);

    }
}
