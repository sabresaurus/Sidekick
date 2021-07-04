#if SIDEKICK_DEBUG
using Sabresaurus.Sidekick.Responses;
using System.Text;

namespace Sabresaurus.Sidekick
{
    public static class ResponseDebug
    {
        public static string GetDebugStringForResponse(BaseResponse response)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(response.GetType().Name);

            return stringBuilder.ToString();
        }
    }
}
#endif