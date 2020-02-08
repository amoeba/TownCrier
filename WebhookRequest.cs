using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace TownCrier
{
    internal class WebhookRequest
    {
        private Webhook Hook { get; set; }
        private string MessageTemplate { get; set; }
        private string EventMessage { get; set; }
        private Regex Pattern { get; set; }

        public WebhookRequest(Webhook webhook, string message)
        {
            Hook = webhook;
            MessageTemplate = message;
            EventMessage = null;
            Pattern = null;
        }

        public WebhookRequest(Webhook webhook, string messageTemplate, string eventMessage)
        {
            Hook = webhook;
            MessageTemplate = messageTemplate;
            EventMessage = StripTags(eventMessage);
            Pattern = null;
        }

        public WebhookRequest(Webhook webhook, string eventMessage, Regex pattern)
        {
            Hook = webhook;
            MessageTemplate = null;
            EventMessage = StripTags(eventMessage);
            Pattern = pattern;
        }

        public WebhookRequest(Webhook webhook, string messageTemplate, string eventMessage, Regex pattern)
        {
            Hook = webhook;
            MessageTemplate = messageTemplate;
            EventMessage = StripTags(eventMessage);
            Pattern = pattern;
        }

        public void Send()
        {

            Util.LogMessage("WebhookRequest.Send()");

            switch (Hook.Method)
            {
                case "GET":
                    SendGET();

                    break;
                case "POST":
                    SendPOST();

                    break;
                default:
                    break;
            }
        }

        private void SendGET()
        {
            Util.LogMessage("WebhookRequest.SendGET");

            string url = Hook.URLFormatString;
            url = SubstituteAt(url, true);
            url = SubstituteVariables(url, true);
            url = SubstituteBackreferences(url, true);

            Util.LogMessage("  Url is " + url);

            try
            {
                Thread t = new Thread(() =>
                {
                    try
                    {
                        Uri uri = new Uri(url);
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                        req.Method = Hook.Method;


                        var httpResponse = (HttpWebResponse)req.GetResponse();

                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            // Throwaway
                            var responseText = streamReader.ReadToEnd();
                        }
                    }
                    catch (WebException wex)
                    {
                        if (wex.Response == null)
                        {
                            return;
                        }

                        using (var errorResponse = (HttpWebResponse)wex.Response)
                        {
                            using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                            {
                                string error = reader.ReadToEnd();

                                Util.WriteToChat("Error encountered when sending webhook:");
                                Util.WriteToChat(string.Format("URL: '{0}'", url));
                                Util.WriteToChat(string.Format("Error: '{0}'", error));
                                Util.WriteToChat("Double-check your URL, Method, and Payload values.");

                                Util.LogMessage(error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Util.LogError(ex);
                    }
                });

                t.Start();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void SendPOST()
        {
            Util.LogMessage("WebhookRequest.SendPOST");
            
            string url = Hook.URLFormatString;
            url = SubstituteAt(url, true);
            url = SubstituteVariables(url, true);
            url = SubstituteBackreferences(url, true);


            Util.LogMessage("  Url is " + url);

            string payload = Hook.PayloadFormatString;
            payload = SubstituteAt(payload, false);
            payload = SubstituteVariables(payload, false);

            if (EventMessage != null && Pattern != null)
            {
                Util.WriteToChat("Substituting backreferences...");
                payload = SubstituteBackreferences(payload, false);
            } else
            {
                Util.WriteToChat("Not substituting backreferences.");
            }

            Util.LogMessage("  Payload is " + payload);

            try
            {
                Thread t = new Thread(() =>
                {
                    try
                    {
                        Uri uri = new Uri(url);
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                        req.Method = Hook.Method;
                        req.ContentType = "application/json";

                        using (var streamWriter = new StreamWriter(req.GetRequestStream()))
                        {
                            streamWriter.Write(payload);
                            streamWriter.Flush();
                        }

                        var httpResponse = (HttpWebResponse)req.GetResponse();

                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            // Throwaway
                            var responseText = streamReader.ReadToEnd();
                        }
                    }
                    catch (WebException wex)
                    {
                        if (wex.Response == null)
                        {
                            return;
                        }

                        using (var errorResponse = (HttpWebResponse)wex.Response)
                        {
                            using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                            {
                                string error = reader.ReadToEnd();

                                Util.WriteToChat("Error encountered when sending webhook:");
                                Util.WriteToChat(string.Format("URL: '{0}'", url));
                                Util.WriteToChat(string.Format("Payload: '{0}'", payload));
                                Util.WriteToChat(string.Format("Error: '{0}'", error));
                                Util.WriteToChat("Double-check your URL, Method, and Payload values.");

                                Util.LogMessage(error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Util.LogError(ex);
                    }
                });

                t.Start();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        public string SubstituteAt(string target, bool escape)
        {
            return target.Replace("@", MaybeEscape(MessageTemplate, escape));
        }

        public string SubstituteVariables(string target, bool escape)
        {
            string modified = target;

            try
            {
                if (modified.Contains("$EVENT") && (EventMessage != null || EventMessage.Length > 0))
                {
                    modified = modified.Replace("$EVENT", MaybeEscape(EventMessage, escape));
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

        public string SubstituteBackreferences(string target, bool escape)
        {
            if (Pattern == null)
            {
                Util.WriteToChat("Pattern is null, returning");
                return target;
            }

            Match m = Pattern.Match(EventMessage);

            if (!m.Success)
            {
                Util.WriteToChat("Is not a success");

                return target;
            }

            GroupCollection groups = m.Groups;

            if (groups == null || groups.Count <= 1)
            {
                return target;
            }

            string result = target;

            for (int i = 1; i < groups.Count; i++)
            {
                result = result.Replace("$" + i.ToString(), MaybeEscape(groups[i].Value, true));
            }

            return result;
        }
       
        public string StripTags(string message)
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

        public string MaybeEscape(string message, bool escape)
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


    }
}
