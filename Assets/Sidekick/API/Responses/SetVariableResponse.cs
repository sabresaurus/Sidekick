using UnityEngine;
using System.Collections;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
	public class SetVariableResponse : BaseResponse
    {
        public SetVariableResponse()
        {

        }
        public SetVariableResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
        }

        public override void Write(BinaryWriter bw)
        {
            
        }
    }
}
