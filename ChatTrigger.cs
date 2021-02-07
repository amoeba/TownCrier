using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TownCrier
{
    public class ChatTrigger
    {
        public string Pattern { get; set; }
        public string WebhookName { get; set; }
        public string MessageFormat { get; set; }
        public bool Enabled { get; set; }

        public ChatTrigger(string pattern, string webhookName, string message, bool enabled)
        {
            try
            {
                Pattern = pattern;
                WebhookName = webhookName;
                MessageFormat = message;
                Enabled = enabled;
            }
            catch (Exception ex)
            {
                Utilities.WriteToChat("Error creating new Chat Trigger: " + ex.Message);
                Utilities.LogError(ex);
            }
        }

        public bool Match(Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            if (e == null)
            {
                return false;
            }

            string finalPattern = Utilities.SubstituteVariables(Pattern, false);
            Regex r = new Regex(finalPattern);

            if (r.IsMatch(e.Text))
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("EventTrigger: On '");
                sb.Append(Pattern.ToString());
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
                Utilities.LogError(ex);

                return "Failed to print ChatTrigger.";
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
