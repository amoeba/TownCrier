using System;
using System.Text;

namespace TownCrier
{
    class EventTrigger
    {
        public string Event;
        public string WebhookName;
        public bool Enabled;

        public EventTrigger(string evt, string webhookName, bool enabled)
        {
            Event = evt;
            WebhookName = webhookName;
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

        public string ToSetting()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("eventtrigger\t");
                sb.Append(Event);
                sb.Append("\t");
                sb.Append(WebhookName);
                sb.Append("\t");
                sb.Append(Enabled);

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
