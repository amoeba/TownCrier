using System;
using System.Text;

namespace TownCrier
{
    class Action
    {
        public int Event { get; set; }
        public string WebhookName { get; set; }
        public bool Enabled { get; set; }

        public Action(int evt, string webhookName)
        {
            Event = evt;
            WebhookName = webhookName;
            Enabled = true;
        }

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

        public void Parse(string action)
        {
            try
            {
                string[] parts = action.Split('\t');

                if (parts.Length != 3)
                {
                    throw (new Exception("Failed to parse serialized Action of '" + action + "'"));
                }

                Enabled = bool.Parse(parts[0]);
                Event = int.Parse(parts[1]);
                WebhookName = parts[2];
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
