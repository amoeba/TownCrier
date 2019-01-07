using System;
using System.Collections.Generic;
using System.Text;

namespace TownCrier
{
    class Action
    {
        public bool Enabled { get; set; }
        public int Event { get; }
        public Webhook Webhook { get; }


        public Action(int evt, Webhook webhook)
        {
            Event = evt;
            Webhook = webhook;
            Enabled = true;
        }

        public void Trigger(WebhookMessage message)
        {
            Webhook.Send(message);
        }
    }
}
