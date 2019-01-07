using System;

namespace TownCrier
{
    class WebhookMessage
    {
        public string Message { get; }

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
            return Uri.EscapeUriString(Message);
        }

        public string ToJSON(string payload)
        {
            return payload.Replace("@", Message);
        }
    }
}
