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

        public string ToSetting()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("webhook\t");
                sb.Append(Name);
                sb.Append("\t");
                sb.Append(URLFormatString);
                sb.Append("\t");
                sb.Append(Method);
                sb.Append("\t");
                sb.Append(PayloadFormatString);

                Util.LogMessage("Webhook as JSON is `" + ToJSON() + "`");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return "";
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
                Util.WriteToChat("Plugin directory is " + Util.GetPluginDirectory());

                Util.EnsurePathExists(Util.GetPluginDirectory());

                // Stop if the path doesn't exist
                if (!File.Exists(Util.GetPluginDirectory()))
                {
                    Util.WriteToChat("Couldn't create plugin directory.");

                    //return;
                }
                
                string path = string.Format(@"{0}\Webhooks.json", Util.GetPluginDirectory());
                Util.WriteToChat("Webhook.json path is " + path);

                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
                }

                Util.WriteToChat("Done saving webhook" + path);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
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
