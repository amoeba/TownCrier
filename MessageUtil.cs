using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TownCrier
{
    class MessageUtil
    {
        public static string MaybeEscape(string message, bool escape)
        {
            try
            {
                if (escape)
                {
                    return Uri.EscapeUriString(message);
                }
                else
                {
                    return message;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return message;
            }
        }

        public static string SubstituteVariables(string message)
        {
            return SubstituteVariables(message, null, false);
        }

        public static string SubstituteVariables(string message, string eventMessage, bool escape)
        {
            string modified = message;

            try
            {
                if (message == "" && eventMessage == "")
                {
                    return "Empty webhook.";
                }

                // Short circuit to support EventTriggers with no format string
                if (message == "")
                {
                    return eventMessage;
                }

                if (modified.Contains("$EVENT") && eventMessage != "")
                {
                    modified = modified.Replace("$EVENT", MaybeEscape(eventMessage, escape));
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
                    modified = modified.Replace("$UXP", MaybeEscape(string.Format("{0:#,##0}", Globals.Core.CharacterFilter.UnassignedXP), escape));

                }

                if (modified.Contains("$TXP"))
                {
                    modified = modified.Replace("$TXP", MaybeEscape(string.Format("{0:#,##0}", Globals.Core.CharacterFilter.TotalXP), escape));
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

                if (modified.Contains("$DATETIME"))
                {
                    DateTime now = DateTime.Now;

                    modified = modified.Replace(
                        "$DATETIME",
                        MaybeEscape(
                            String.Format("{0} {1}",
                                now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern),
                                now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern)),
                            escape));
                }

                if (modified.Contains("$DATE"))
                {
                    modified = modified.Replace(
                        "$DATE", 
                        MaybeEscape(DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern), escape));
                }

                if (modified.Contains("$TIME"))
                {
                    modified = modified.Replace(
                        "$TIME",
                        MaybeEscape(DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern), escape));
                }

                return modified;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return modified;
            }
        }

        public static string StripTags(string message)
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
