using Sabresaurus.Sidekick.Responses;
using System.IO;

namespace Sabresaurus.Sidekick.Requests
{
    public abstract class BaseRequest
    {
        protected BaseResponse uncastResponse;

        public BaseResponse UncastResponse
        {
            get
            {
                return uncastResponse;
            }

            set
            {
                uncastResponse = value;
            }
        }

        public void Write(BinaryWriter bw)
        {
            uncastResponse.Write(bw);
        }
    }
}