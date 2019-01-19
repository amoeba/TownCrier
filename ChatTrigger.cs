using System;
using System.Text;

namespace TownCrier
{
    class ChatTrigger
    {
        public string Pattern;
        public Webhook Webhook;
        public string MessageFormat;
        public bool Enabled;

        public ChatTrigger(string pattern, Webhook webhook, string message, bool enabled)
        {
            Pattern = pattern;
            Webhook = webhook;
            MessageFormat = message;
            Enabled = enabled;
        }

        public bool Match(Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            // Match the message and the color (but only match color if
            // we set a Color to match)
            if (e.Text.Contains(Pattern))
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
                sb.Append(Pattern);
                sb.Append("', trigger webhook '");
                sb.Append(Webhook.Name);
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

        public string ToSetting()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("chattrigger\t");
                sb.Append(Pattern);
                sb.Append("\t");
                sb.Append(Webhook.Name);
                sb.Append("\t");
                sb.Append(MessageFormat);
                sb.Append("\t");
                sb.Append(Enabled);

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
