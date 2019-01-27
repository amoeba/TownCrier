using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TownCrier
{
    class ChatTrigger
    {
        public Regex Pattern;
        public int Color;
        public Webhook Webhook;
        public string MessageFormat;
        public bool Enabled;

        public ChatTrigger(string pattern, Webhook webhook, string message, bool enabled)
        {
            try
            {
                Pattern = new Regex(MessageUtil.SubstituteVariables(pattern), RegexOptions.Compiled);
                Webhook = webhook;
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
