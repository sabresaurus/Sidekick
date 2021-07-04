using System.IO;

namespace Sabresaurus.Sidekick.Responses
{
    public class ExampleResponse : BaseResponse
    {
        private string message;

        public string Message => message;

        public ExampleResponse(string message)
        {
            this.message = message;
        }

        public ExampleResponse(BinaryReader br, int requestID)
            : base(br, requestID)
        {
            message = br.ReadString();
        }


		public override void Write(BinaryWriter bw)
		{
            bw.Write(message);
		}
	}
}
