using UnityEngine;
using System.Collections;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class InvokeMethodResponse : BaseResponse
    {
        public InvokeMethodResponse()
        {

        }
        public InvokeMethodResponse(BinaryReader br)
            : base(br)
        {
            
        }

        public override void Write(BinaryWriter bw)
        {

        }
    }
}
