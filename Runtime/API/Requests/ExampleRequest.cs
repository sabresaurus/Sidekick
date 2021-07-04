using UnityEngine;
using Sabresaurus.Sidekick.Responses;
using System.IO;

namespace Sabresaurus.Sidekick.Requests
{
    public class ExampleRequest : BaseRequest
    {
        private string message;

        public ExampleRequest(string message)
        {
            this.message = message;
        }

        public ExampleRequest(BinaryReader br)
        {
            this.message = br.ReadString();
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);

            bw.Write(message);
        }

        public override BaseResponse GenerateResponse()
        {
            // Do a simple change to the message and send it back
            return new ExampleResponse($"{message} received from build on {Application.platform}");
        }
    }
}