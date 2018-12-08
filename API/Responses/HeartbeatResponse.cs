using UnityEngine;
using System.Collections;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class HeartbeatResponse : BaseResponse
    {
        public HeartbeatResponse()
        {

        }

        public HeartbeatResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
            
        }

        public override void Write(BinaryWriter bw)
        {
            
        }
    }
}