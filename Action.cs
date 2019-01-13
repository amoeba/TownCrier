using System;
using System.Text;

namespace TownCrier
{
    class Action
    {
        public int Event;
        public string WebhookName;
        public bool Enabled;

        public Action(int evt, string webhookName, bool enabled)
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

                sb.Append("Action: On '");
                sb.Append(Enum.GetName(typeof(PluginCore.EVENT), Event));
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

                return "Failed to print Action";
            }
        }

        public string ToSetting()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("action\t");
                sb.Append(Event.ToString());
                sb.Append("\t");
                sb.Append(WebhookName);
                sb.Append("\t");
                sb.Append(Enabled);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return "Failed to print Action";
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
