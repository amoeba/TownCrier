using System;
using System.Net;
using System.Text;

namespace TownCrier
{
    class Webhook
    {
        public string Name { get; }
        public Uri BaseURI { get; }
        public string Method { get; }
        public string Payload { get; }

        public Webhook(string name, string url, string method)
        {
            Name = name;
            BaseURI = new Uri(url);
            Method = method;
            Payload = null;
        }

        public Webhook (string name, string url, string method, string payload)
        {
            Name = name;
            BaseURI = new Uri(url);
            Method = method;
            Payload = payload;
        }

        public override string ToString()
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
            if (Method == "POST")
            {
                return BaseURI;
            }
            else
            {
                return new Uri(BaseURI.ToString().Replace("@", message.ToQueryStringValue()));
            }
        }

        public void Send(WebhookMessage message)
        {
            if (Method == "GET")
            {
                DoSend(message);
            }
            else if (Payload != null)
            {
                DoSendJSON(message);
            }
        }
        
        internal void DoSend(WebhookMessage message)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(FullURI(message));

            req.Method = Method;

            req.BeginGetResponse(new AsyncCallback((IAsyncResult result) =>
            {
                Util.WriteToChat("Sending Webhook " + Method + " to " + FullURI(message).ToString() + " with no payload.");

                WebRequest request = (WebRequest)result.AsyncState;
                WebResponse response = request.EndGetResponse(result);
            }), req);
        }

        internal void DoSendJSON(WebhookMessage message)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("Content-Type: application/json");

                    client.UploadStringCompleted += (s, e) =>
                    {
                        if (e.Error != null)
                        {
                            Util.WriteToChat("Sending webhook failed with error: " + e.Error.Message);
                        }
                    };

                    Util.WriteToChat("Sending Webhook POST to " + FullURI(message).ToString() + " with payload " + message.ToJSON(Payload)  + ".");
                    client.UploadStringAsync(FullURI(message), "POST", message.ToJSON(Payload));
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
