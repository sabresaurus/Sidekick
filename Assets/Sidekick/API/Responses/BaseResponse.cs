using UnityEngine;
using System.Collections;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public abstract class BaseResponse
    {
        public BaseResponse()
        {

        }

        public BaseResponse(BinaryReader br)
        {

        }

        public abstract void Write(BinaryWriter bw);

    }
}
