using UnityEngine;
using System.Collections;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class InvokeMethodResponse : BaseResponse
    {
        WrappedVariable returnedVariable;

        public WrappedVariable ReturnedVariable
        {
            get
            {
                return returnedVariable;
            }
        }

        public InvokeMethodResponse(WrappedVariable returnedVariable)
        {
            this.returnedVariable = returnedVariable;
        }

        public InvokeMethodResponse(BinaryReader br)
            : base(br)
        {
            returnedVariable = new WrappedVariable(br);
        }

        public override void Write(BinaryWriter bw)
        {
            returnedVariable.Write(bw);
        }
    }
}
