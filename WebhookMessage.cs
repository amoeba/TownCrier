using System;

namespace TownCrier
{
    class WebhookMessage
    {
        public string Message;

        public WebhookMessage(string message)
        {
            Message = message;
        }

        public override string ToString()
        {
            return Message;
        }

        public string ToQueryStringValue()
        {
            try
            {
                return Uri.EscapeUriString(Message);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return Message;
            }
        }

        public string ToJSON(string payload)
        {
            try
            {
                return payload.Replace("@", Message);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return payload;
            }
        }
    }
}
