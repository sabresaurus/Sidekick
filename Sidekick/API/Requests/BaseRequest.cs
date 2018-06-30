using Sabresaurus.Sidekick.Responses;
using System.IO;

namespace Sabresaurus.Sidekick.Requests
{
    public abstract class BaseRequest
    {
        public virtual void Write(BinaryWriter bw)
        {
            
        }

        public abstract BaseResponse GenerateResponse();
    }
}