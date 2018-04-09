using UnityEngine;
using System.Collections;
using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class InvokeMethodResponse : BaseResponse
    {
        string methodName;
        WrappedVariable returnedVariable;

        public string MethodName
        {
            get
            {
                return methodName;
            }
        }

        public WrappedVariable ReturnedVariable
        {
            get
            {
                return returnedVariable;
            }
        }

        public InvokeMethodResponse(string methodName, WrappedVariable returnedVariable)
        {
            this.methodName = methodName;
            this.returnedVariable = returnedVariable;
        }

        public InvokeMethodResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
            methodName = br.ReadString();
            returnedVariable = new WrappedVariable(br);
        }

        public override void Write(BinaryWriter bw)
        {
            bw.Write(methodName);
            returnedVariable.Write(bw);
        }
    }
}
