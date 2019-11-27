namespace TownCrier
{
    public class WebhookMessage
    {
        public string MessageFormatString;
        public string EventMessage;

        public WebhookMessage(string message)
        {
            MessageFormatString = message;
            EventMessage = "";
        }

        public WebhookMessage(string message, string eventMessage)
        {
            MessageFormatString = message;
            EventMessage = MessageUtil.StripTags(eventMessage); // Remove tags like <Tell>
        }

        public override string ToString()
        {
            return MessageFormatString;
        }

        public string ToQueryStringValue()
        {
            return MessageUtil.SubstituteVariables(MessageFormatString, EventMessage, true);
        }

        public string ToJSONStringValue()
        {
            return MessageUtil.SubstituteVariables(MessageFormatString, EventMessage, false).Replace("\"", "\\\"");
        }
    }
}
