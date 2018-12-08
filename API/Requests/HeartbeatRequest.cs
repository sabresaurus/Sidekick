using Sabresaurus.Sidekick.Responses;
using System.IO;

namespace Sabresaurus.Sidekick.Requests
{
    /// <summary>
    /// Tells a connected client that we're still interested and they shouldn't
    /// disconnect us
    /// </summary>
    public class HeartbeatRequest : BaseRequest
    {
        public HeartbeatRequest()
        {
        }

        public HeartbeatRequest(BinaryReader binaryReader)
        {
            
        }

        public override void Write(BinaryWriter bw)
        {
            base.Write(bw);
        }

        public override BaseResponse GenerateResponse()
        {
            return new HeartbeatResponse();
        }
    }
}