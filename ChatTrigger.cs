using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TownCrier
{
    public class ChatTrigger
    {
        public Regex Pattern;
        public int Color;
        public string WebhookName;
        public string MessageFormat;
        public bool Enabled;

        public ChatTrigger(string pattern, string webhookName, string message, bool enabled)
        {
            try
            {
                Pattern = new Regex(MessageUtil.SubstituteVariables(pattern), RegexOptions.Compiled);
                WebhookName = webhookName;
                MessageFormat = message;
                Enabled = enabled;
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error creating new Chat Trigger: " + ex.Message);
                Util.LogError(ex);
            }

            // TODO
            Color = -1;
        }

        public bool Match(Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            if (Pattern.IsMatch(e.Text) && (Color == -1 ? true : e.Color == Color))
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
                Util.LogError(ex);

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
