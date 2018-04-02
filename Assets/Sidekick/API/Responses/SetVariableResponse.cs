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
        public SetVariableResponse(BinaryReader br)
            : base(br)
        {
        }

        public override void Write(BinaryWriter bw)
        {
            
        }
    }
}
