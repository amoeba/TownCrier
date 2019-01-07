using System;
using System.Collections.Generic;
using System.Net;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;

namespace TownCrier
{
    [WireUpBaseEvents]

    [MVView("TownCrier.mainView.xml")]
    [MVWireUpControlEvents]

    [FriendlyName("TownCrier")]
	public class PluginCore : PluginBase
	{
        List<Action> actions;
        List<Webhook> webhooks;

        [MVControlReference("lstWebhooks")]
        private IList lstWebhooks = null;

        [MVControlReference("lstActions")]
        private IList lstActions = null;

        [MVControlReference("chcEventsEvent")]
        private ICombo chcEventsEvent = null;

        [MVControlReference("chcEventsWebhook")]
        private ICombo chcEventsWebhook = null;

        [MVControlReference("edtURL")]
        private ITextBox edtURL = null;
        [MVControlReference("edtMethod")]
        private ITextBox edtMethod = null;
        [MVControlReference("edtPayload")]
        private ITextBox edtPayload = null;

        protected override void Startup()
		{
			try
			{
				Globals.Init("TownCrier", Host, Core);

                MVWireupHelper.WireupStart(this, Host);

                Webhook hookA = new Webhook("https://hooks.zapier.com/hooks/catch/1226461/0ie1ok/?message=@", "GET");
                Webhook hookB = new Webhook("https://discordapp.com/api/webhooks/531740310674604043/wU1FqslYss6aAlEZ_IPVCHumK53J8hY_BcLVYxjWcpuJwgS4TaI8RIDInYp2zKeSeFy3", "POST", "{\"content\": \"@\"}");

                actions = new List<Action>();
                actions.Add(new Action(0x0020, hookA));
                actions.Add(new Action(0x0020, hookB));

                webhooks = new List<Webhook>();
                webhooks.Add(hookA);
                webhooks.Add(hookB);

                RefreshUI();
            }
            catch (Exception ex) { Util.LogError(ex); }
		}

        protected override void Shutdown()
		{
			try
			{
                MVWireupHelper.WireupEnd(this);
			}
			catch (Exception ex) { Util.LogError(ex); }
		}

        private void RefreshUI()
        {
            RefreshWebhooksChoice();
            RefreshWebhooksList();
            RefreshEventsList();
        }

        private void RefreshEventsList()
        {
            if (lstActions.RowCount > 0)
            {
                for (int i = 0; i < lstActions.RowCount; i++)
                {
                    lstActions.RemoveRow(i);
                }
            }

            foreach (var action in actions)
            {
                IListRow row = lstActions.Add();

                row[0][0] = action.Event.ToString();
                row[1][0] = action.Webhook.BaseURI.ToString();
            }
        }

        private void RefreshWebhooksList()
        {
            if (lstWebhooks.RowCount > 0)
            {
                for (int i = 0; i < lstWebhooks.RowCount; i++)
                {
                    lstWebhooks.RemoveRow(i);
                }
            }

            foreach (var webhook in webhooks)
            {
                IListRow row = lstWebhooks.Add();

                row[0][0] = webhook.BaseURI.ToString();
                row[1][0] = webhook.Method;
                row[2][0] = webhook.Payload;
            }
        }

        private void RefreshWebhooksChoice()
        {
            try
            {
                Util.WriteToChat("RefreshWebhooksChoice()");

                chcEventsWebhook.Clear();

                foreach (var webhook in webhooks)
                {
                    chcEventsWebhook.Add(webhook.BaseURI.ToString(), webhook);
                }

                chcEventsWebhook.Selected = 0;
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("ServerDispatch", "EchoFilter")]
        private void EchoFilter(object sender, NetworkMessageEventArgs e)
        {
            if (actions == null)
            {
                return;
            }

            try
            {
                if (e.Message.Type == 0xF7B0) // Game Event
                {
                    List<Action> matched = actions.FindAll(a => a.Event  == (int)e.Message["event"] ? true : false);

                    foreach (Action action in matched)
                    {
                        action.Trigger(new WebhookMessage("Webhook triggered."));
                    }
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }

        }

        [MVControlEvent("btnEventsEventAdd", "Click")]
        void btnEventsEventAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                Util.WriteToChat("btnEventsEventAdd Clicked");

                Action action = new Action(
                    (int)chcEventsEvent.Data[chcEventsEvent.Selected], 
                    (Webhook)chcEventsWebhook.Data[chcEventsWebhook.Selected]);

                actions.Add(action);

                RefreshEventsList();
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("btnWebhookAdd", "Click")]
        void btnWebhookAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                Util.WriteToChat("btnWebhookAdd Clicked");

                Webhook webhook = new Webhook(edtURL.Text, edtMethod.Text, edtPayload.Text);
                webhooks.Add(webhook);

                RefreshWebhooksList();
                RefreshWebhooksChoice();
            }
            catch (Exception ex) { Util.LogError(ex); }
        }
    }
}
