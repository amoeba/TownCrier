using System;
using System.Text.RegularExpressions;

namespace TownCrier
{
    class WebhookMessage
    {
        public string MessageFormatString;
        public string EventMessage;

        public WebhookMessage(string message, string eventMessage)
        {
            MessageFormatString = message;
            EventMessage = RemoveTags(eventMessage); // Remove tags like <Tell>
        }

        public override string ToString()
        {
            return MessageFormatString;
        }

        public string ToQueryStringValue()
        {
            return SubstituteVariables(true);
        }

        public string ToJSONStringValue()
        {
            return SubstituteVariables(false).Replace("\"", "'");
        }

        public string SubstituteVariables(bool escape)
        {
            string modified = MessageFormatString;

            try
            {
                if (MessageFormatString == "" && EventMessage == "")
                {
                    return "Empty webhook.";
                }

                // Short circuit to support EventTriggers with no format string
                if (MessageFormatString == "")
                {
                    return EventMessage;
                }

                if (modified.Contains("$EVENT") && EventMessage != "")
                {
                    modified = modified.Replace("$EVENT", MaybeEscape(EventMessage.ToString(), escape));
                }

                if (modified.Contains("$NAME"))
                {
                    modified = modified.Replace("$NAME", MaybeEscape(Globals.Core.CharacterFilter.Name, escape));
                }

                if (modified.Contains("$LEVEL"))
                {
                    modified = modified.Replace("$LEVEL", MaybeEscape(Globals.Core.CharacterFilter.Level.ToString(), escape));
                }

                if (modified.Contains("$UXP"))
                {
                    modified = modified.Replace("$UXP", MaybeEscape(Globals.Core.CharacterFilter.UnassignedXP.ToString(), escape));
                }

                if (modified.Contains("$TXP"))
                {
                    modified = modified.Replace("$TXP", MaybeEscape(Globals.Core.CharacterFilter.TotalXP.ToString(), escape));
                }

                if (modified.Contains("$HEALTH"))
                {
                    modified = modified.Replace("$HEALTH", MaybeEscape(Globals.Core.CharacterFilter.Health.ToString(), escape));
                }

                if (modified.Contains("$STAMINA"))
                {
                    modified = modified.Replace("$STAMINA", MaybeEscape(Globals.Core.CharacterFilter.Stamina.ToString(), escape));
                }

                if (modified.Contains("$MANA"))
                {
                    modified = modified.Replace("$MANA", MaybeEscape(Globals.Core.CharacterFilter.Mana.ToString(), escape));
                }

                if (modified.Contains("$VITAE"))
                {
                    modified = modified.Replace("$VITAE", MaybeEscape(Globals.Core.CharacterFilter.Vitae.ToString() + "%", escape));
                }

                if (modified.Contains("$LOC"))
                {
                    modified = modified.Replace("$LOC", MaybeEscape(new Location(Globals.Host.Actions.Landcell, Globals.Host.Actions.LocationX, Globals.Host.Actions.LocationY).ToString(), escape));
                }

                return modified;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return modified;
            }
        }

        public string MaybeEscape(string str, bool escape)
        {
            if (escape)
            {
                return Uri.EscapeUriString(str);
            }
            else
            {
                return str;
            }
        }

        public string RemoveTags(string message) 
        {
            try
            {
                Regex e = new Regex("<.+>(.+)<.+>", RegexOptions.Compiled);
                Match m = e.Match(message);

                if (!m.Success || m.Captures.Count != 1 || m.Groups.Count != 2)
                {
                    return message;
                }

                return message.Replace(m.Groups[0].Value, m.Groups[1].Value);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return message;
            }
        }
    }
}
