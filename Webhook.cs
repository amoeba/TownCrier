using System;
using System.Collections.Generic;
using System.Net;

namespace TownCrier
{
    class Webhook
    {
        public Uri BaseURI { get; }
        public string Method { get; }
        public string Payload { get; }

        public Webhook(string url, string method)
        {
            BaseURI = new Uri(url);
            Method = method;
            Payload = null;
        }

        public Webhook (string url, string method, string payload)
        {
            BaseURI = new Uri(url);
            Method = method;
            Payload = payload;
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
                GET(message);
            }
            else if (Method == "POST")
            {
                POST(message);
            }
        }

        public void Test(WebhookMessage message)
        {
            Send(new WebhookMessage("Test"));
        }

        internal void GET(WebhookMessage message)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(FullURI(message));

            req.BeginGetResponse(new AsyncCallback((IAsyncResult result) =>
            {
                WebRequest request = (WebRequest)result.AsyncState;
                WebResponse response = request.EndGetResponse(result);

                // TODO: Handle response
            }), req);
        }

        internal void POST(WebhookMessage message)
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

                    Util.WriteToChat("Sending Webhook post to" + FullURI(message).ToString() + " with payload " + message.ToJSON(Payload)  + ".");
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
