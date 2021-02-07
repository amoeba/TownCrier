using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private string Pattern { get; set; }

        public WebhookRequest(Webhook webhook, string message)
        {
            Hook = webhook;
            MessageTemplate = message;
            EventMessage = null;
            Pattern = null;
        }

        public WebhookRequest(Webhook webhook, string messageTemplate, string eventMessage, string pattern)
        {
            Hook = webhook;
            MessageTemplate = messageTemplate;
            EventMessage = StripTags(eventMessage);
            Pattern = pattern;
        }

        public void Send()
        {

            Utilities.LogMessage("WebhookRequest.Send()");

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
            Utilities.LogMessage("WebhookRequest.SendGET");

            string url = Hook.URLFormatString;
            url = SubstituteAt(url, true);
            url = SubstituteEventVariable(url, true);
            url = Utilities.SubstituteVariables(url, true);
            url = SubstituteBackreferences(url, true);

            Utilities.LogMessage("  Url is " + url);

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

                                Utilities.WriteToChat("Error encountered when sending webhook:");
                                Utilities.WriteToChat(string.Format("URL: '{0}'", url));
                                Utilities.WriteToChat(string.Format("Error: '{0}'", error));
                                Utilities.WriteToChat("Double-check your URL, Method, and Payload values.");

                                Utilities.LogMessage(error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.LogError(ex);
                    }
                });

                t.Start();
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        private void SendPOST()
        {
            Utilities.LogMessage("WebhookRequest.SendPOST");
            
            string url = Hook.URLFormatString;
            url = SubstituteAt(url, true);
            url = SubstituteEventVariable(url, true);
            url = Utilities.SubstituteVariables(url, true);
            url = SubstituteBackreferences(url, true);


            Utilities.LogMessage("  Url is " + url);

            try
            {
                Thread t = new Thread(() =>
                {
                    try
                    {
                        // Non-cleverly parse the payload string, do substitutions on all values, and serialize that
                        Dictionary<string, string> deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(Hook.PayloadFormatString);
                        Dictionary<string, string> payload_out = new Dictionary<string, string>();

                        string valuetmp;

                        foreach (KeyValuePair<string, string> pair in deserialized)
                        {
                            valuetmp = pair.Value;
                            valuetmp = SubstituteAt(valuetmp, false);
                            valuetmp = SubstituteEventVariable(valuetmp, false);
                            valuetmp = Utilities.SubstituteVariables(valuetmp, false);

                            if (EventMessage != null && Pattern != null)
                            {
                                valuetmp = SubstituteBackreferences(valuetmp, false);
                            }

                            payload_out.Add(pair.Key, valuetmp);
                        }

                        Utilities.LogMessage("  Payload is " + JsonConvert.SerializeObject(payload_out));

                        Uri uri = new Uri(url);
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                        req.Method = Hook.Method;
                        req.ContentType = "application/json";

                        using (var streamWriter = new StreamWriter(req.GetRequestStream()))
                        {
                            streamWriter.Write(JsonConvert.SerializeObject(payload_out));
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

                                Utilities.WriteToChat("Error encountered when sending webhook:");
                                Utilities.WriteToChat(string.Format("URL: '{0}'", url));
                                Utilities.WriteToChat(string.Format("Payload: '{0}'", Hook.PayloadFormatString));
                                Utilities.WriteToChat(string.Format("Error: '{0}'", error));
                                Utilities.WriteToChat("Double-check your URL, Method, and Payload values.");

                                Utilities.LogMessage(error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Utilities.LogError(ex);
                    }
                });

                t.Start();
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }
        }

        public string SubstituteAt(string target, bool escape)
        {
            return target.Replace("@", Utilities.MaybeURLEncode(MessageTemplate, escape));
        }

        private string SubstituteEventVariable(string target, bool escape)
        {
            string modified = target;

            if (modified.Contains("$EVENT") && (EventMessage != null || EventMessage.Length > 0))
            {
                modified = modified.Replace("$EVENT", Utilities.MaybeURLEncode(EventMessage, escape));
            }

            return modified;
        }
        public string SubstituteBackreferences(string target, bool escape)
        {
            if (Pattern == null)
            {
                return target;
            }

            Regex r = new Regex(Pattern);
            Match m = r.Match(EventMessage);

            if (!m.Success)
            {
                return target;
            }

            GroupCollection groups = m.Groups;

            if (groups == null || groups.Count <= 1)
            {
                return target;
            }

            string result = target;

            // Replace groups by name
            foreach (var name in r.GetGroupNames())
            {
                result = result.Replace("$" + name, Utilities.MaybeURLEncode(groups[name].Value, escape));
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
                Utilities.LogError(ex);

                return message;
            }
        }
    }
}
