using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace TownCrier
{
    public class Webhook
    {
        public string Name;
        public string Method;
        public string URLFormatString; // Contains an @ somewhere
        public string PayloadFormatString; // Or contains an @ somewhere

        public Webhook(string name, string url, string method, string payload)
        {
            Name = name;
            URLFormatString = url;
            Method = method;
            PayloadFormatString = payload;
        }

        public override string ToString()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Webhook: Name: '");
                sb.Append(Name);
                sb.Append("', URL: '");
                sb.Append(URLFormatString.ToString());
                sb.Append(" (");
                sb.Append(Method);
                sb.Append(") with payload '");
                sb.Append(PayloadFormatString);
                sb.Append("'.");

                return sb.ToString();

            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return "Failed to print Webhook";
            }
        }

        public string ToJSON()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public void Save()
        {
            try
            {
                Util.EnsurePathExists(String.Format(@"{0}\{1}", Globals.PluginDirectory, "Webhooks"));
                string path = String.Format(@"{0}\{1}\{2}.json", Globals.PluginDirectory, "Webhooks", Name);

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        public Uri URI(WebhookMessage message)
        {
            try
            {
                return new Uri(URLFormatString.Replace("@", message.ToQueryStringValue()));
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return new Uri(URLFormatString);
            }
        }

        private string Payload(WebhookMessage message)
        {
            return PayloadFormatString.Replace("@", message.ToJSONStringValue());
        }

        public void Send(WebhookMessage message)
        {
            try
            {
                Thread t = new Thread(() =>
                {
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URI(message));
                        req.Method = Method;

                        if (Method == "POST" && PayloadFormatString != "")
                        {
                            req.ContentType = "application/json";

                            using (var streamWriter = new StreamWriter(req.GetRequestStream()))
                            {
                                streamWriter.Write(Payload(message));
                                streamWriter.Flush();
                            }
                        }

                        var httpResponse = (HttpWebResponse)req.GetResponse();

                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            var responseText = streamReader.ReadToEnd();
                        }
                    }
                    catch (WebException wex)
                    {
                        if (wex.Response != null)
                        {
                            using (var errorResponse = (HttpWebResponse)wex.Response)
                            {
                                using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                                {
                                    string error = reader.ReadToEnd();

                                    Util.WriteToChat("Error encountered when sending webhook:");
                                    Util.WriteToChat(error);
                                    Util.WriteToChat("Double-check your URL, Method, and Payload values.");

                                    Util.LogError(new Exception(error));
                                }
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
    }
}
