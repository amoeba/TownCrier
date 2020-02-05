using System;
using System.Text;

namespace TownCrier
{
    public class EventTrigger
    {
        public string Event;
        public string WebhookName;
        public string MessageFormat;
        public bool Enabled;

        public EventTrigger(string evt, string webhookName, string messageFormat, bool enabled)
        {
            Event = evt;
            WebhookName = webhookName;
            MessageFormat = messageFormat;
            Enabled = enabled;
        }

        public override string ToString()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("EventTrigger: On '");
                sb.Append(Event);
                sb.Append("', trigger webhook '");
                sb.Append(WebhookName);
                sb.Append("' with message: '");
                sb.Append(MessageFormat);
                sb.Append("'. Currently ");
                sb.Append(Enabled ? "Enabled" : "Disabled");
                sb.Append(".");

                return sb.ToString();

            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return "Failed to print EventTrigger.";
            }
        }

        public void Enable()
        {
            Enabled = true;
        }

        public void Disable()
        {
            Enabled = false;
        }
    }
}
