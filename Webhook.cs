using System;
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
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(FullURI(message));
                    req.Method = Method;

                    if (Payload != null)
                    {
                        req.ContentType = "application/json";

                        using (var streamWriter = new System.IO.StreamWriter(req.GetRequestStream()))
                        {
                            streamWriter.Write(message.ToJSON(Payload));
                            streamWriter.Flush();
                        }
                    }

                    req.BeginGetResponse(new AsyncCallback((IAsyncResult result) =>
                    {
                        WebRequest request = (WebRequest)result.AsyncState;
                        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result);
                        response.Close();
                    }), req);
                })
                {
                    IsBackground = true
                };

                t.Start();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
