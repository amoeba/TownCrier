using System;
using System.Collections.Generic;
using System.Net;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;

/*
 * Created by Mag-nus. 8/19/2011, VVS added by Virindi-Inquisitor.
 * 
 * No license applied, feel free to use as you wish. H4CK TH3 PL4N3T? TR45H1NG 0UR R1GHT5? Y0U D3C1D3!
 * 
 * Notice how I use try/catch on every function that is called or raised by decal (by base events or user initiated events like buttons, etc...).
 * This is very important. Don't crash out your users!
 * 
 * In 2.9.6.4+ Host and Core both have Actions objects in them. They are essentially the same thing.
 * You sould use Host.Actions though so that your code compiles against 2.9.6.0 (even though I reference 2.9.6.5 in this project)
 * 
 * If you add this plugin to decal and then also create another plugin off of this sample, you will need to change the guid in
 * Properties/AssemblyInfo.cs to have both plugins in decal at the same time.
 * 
 * If you have issues compiling, remove the Decal.Adapater and VirindiViewService references and add the ones you have locally.
 * Decal.Adapter should be in C:\Games\Decal 3.0\
 * VirindiViewService should be in C:\Games\VirindiPlugins\VirindiViewService\
*/

namespace TownCrier
{
    //Attaches events from core
    [WireUpBaseEvents]

    //View (UI) handling
    [MVView("TownCrier.mainView.xml")]
    [MVWireUpControlEvents]


	// FriendlyName is the name that will show up in the plugins list of the decal agent (the one in windows, not in-game)
	// View is the path to the xml file that contains info on how to draw our in-game plugin. The xml contains the name and icon our plugin shows in-game.
	// The view here is SamplePlugin.mainView.xml because our projects default namespace is SamplePlugin, and the file name is mainView.xml.
	// The other key here is that mainView.xml must be included as an embeded resource. If its not, your plugin will not show up in-game.
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

        /// <summary>
        /// This is called when the plugin is started up. This happens only once.
        /// </summary>
        protected override void Startup()
		{
			try
			{
				// This initializes our static Globals class with references to the key objects your plugin will use, Host and Core.
				// The OOP way would be to pass Host and Core to your objects, but this is easier.
				Globals.Init("TownCrier", Host, Core);

                //Initialize the view.
                MVWireupHelper.WireupStart(this, Host);

                Webhook hookA = new Webhook("https://hooks.zapier.com/hooks/catch/1226461/0ie1ok/?message=@", "GET");
                Webhook hookB = new Webhook("https://discordapp.com/api/webhooks/531740310674604043/wU1FqslYss6aAlEZ_IPVCHumK53J8hY_BcLVYxjWcpuJwgS4TaI8RIDInYp2zKeSeFy3", "POST", "{\"content\": \"@\"}");

                actions = new List<Action>();
                actions.Add(new Action(0x0020, hookA));
                actions.Add(new Action(0x0020, hookB));

                webhooks = new List<Webhook>();
                webhooks.Add(hookA);
                webhooks.Add(hookB);


                RefreshWebhooksChoice();
                RefreshWebhooksList();
                RefreshEventsList();
            }
            catch (Exception ex) { Util.LogError(ex); }
		}


        /// <summary>
        /// This is called when the plugin is shut down. This happens only once.
        /// </summary>
        protected override void Shutdown()
		{
			try
			{
                //Destroy the view.
                MVWireupHelper.WireupEnd(this);
			}
			catch (Exception ex) { Util.LogError(ex); }
		}

		//[BaseEvent("LoginComplete", "CharacterFilter")]
		//private void CharacterFilter_LoginComplete(object sender, EventArgs e)
		//{
  //          try
  //          {
  //              Util.WriteToChat("Plugin now online. Server population: " + Core.CharacterFilter.ServerPopulation);

  //              Util.WriteToChat("CharacterFilter_LoginComplete");

  //              InitSampleList();

  //              // Subscribe to events here
  //              Globals.Core.WorldFilter.ChangeObject += new EventHandler<ChangeObjectEventArgs>(WorldFilter_ChangeObject2);


  //          }

  //          catch (Exception ex) { Util.LogError(ex); }
		//}

        

  //      [BaseEvent("Logoff", "CharacterFilter")]
		//private void CharacterFilter_Logoff(object sender, Decal.Adapter.Wrappers.LogoffEventArgs e)
		//{
		//	try
		//	{
		//		// Unsubscribe to events here, but know that this event is not gauranteed to happen. I've never seen it not fire though.
		//		// This is not the proper place to free up resources, but... its the easy way. It's not proper because of above statement.
		//		Globals.Core.WorldFilter.ChangeObject -= new EventHandler<ChangeObjectEventArgs>(WorldFilter_ChangeObject2);
		//	}
		//	catch (Exception ex) { Util.LogError(ex); }
		//}

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
                    Util.WriteToChat("ServerDispatch/GameEvent of " + e.Message.Type.ToString() + "/" + (int)e.Message["event"]);


                    Util.WriteToChat("Before...");

                    List<Action> matched = actions.FindAll(a => a.Event  == (int)e.Message["event"] ? true : false);

                    Util.WriteToChat("After...");
                    Util.WriteToChat("Found " + matched.Count.ToString() + " actions matching " + e.Message["event"].ToString());

                    foreach (Action action in matched)
                    {
                        Util.WriteToChat("Action found " + action.Webhook.FullURI(new WebhookMessage("NULL")).ToString());
                        action.Trigger(new WebhookMessage("Allegiance info retrieved..."));
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

                Webhook hook = new Webhook(edtURL.Text, edtMethod.Text, edtPayload.Text);
                // Add to lst below...
                MyClasses.MetaViewWrappers.IListRow row = lstWebhooks.Add();
                row[0][0] = edtURL.Text;
                row[1][0] = edtMethod.Text;
                row[2][0] = edtPayload.Text;
                // Refresh Webhooks list in Events page
                RefreshWebhooksChoice();
            }
            catch (Exception ex) { Util.LogError(ex); }
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

        
    }
}
