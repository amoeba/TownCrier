using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace TownCrier
{
    class Webhook
    {
        public string Name;
        public Uri BaseURI;
        public string Method;
        public string Payload;

        public Webhook(string name, string url, string method, string payload)
        {
            Name = name;
            BaseURI = new Uri(url);
            Method = method;
            Payload = payload;
        }

        public override string ToString()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Webhook: Name: '");
                sb.Append(Name);
                sb.Append("', URL: '");
                sb.Append(BaseURI.ToString());
                sb.Append(" (");
                sb.Append(Method);
                sb.Append(") with payload '");
                sb.Append(Payload);
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
                sb.Append(BaseURI);
                sb.Append("\t");
                sb.Append(Method);
                sb.Append("\t");
                sb.Append(Payload);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return "";
            }
        }

        public Uri FullURI(WebhookMessage message)
        {
            try
            {
                return new Uri(BaseURI.ToString().Replace("@", message.ToQueryStringValue()));
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
                return BaseURI;
            }
        }

        public void Send(WebhookMessage message)
        {
            try
            {
                Thread t = new Thread(() =>
                {
                    try
                    {
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(FullURI(message));
                        req.Method = Method;

                        if (Payload != null)
                        {
                            req.ContentType = "application/json";

                            using (var streamWriter = new System.IO.StreamWriter(req.GetRequestStream()))
                            {
                                streamWriter.Write(message.ToJSON(Payload));
                                streamWriter.Flush();
                                streamWriter.Close();
                            }
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

                                    Util.WriteToChat("Error sending webhook: " + error);
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
