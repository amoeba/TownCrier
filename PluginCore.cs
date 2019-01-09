using System;
using System.Collections.Generic;
using System.IO;
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

        [MVControlReference("edtName")]
        private ITextBox edtName = null;
        [MVControlReference("edtURL")]
        private ITextBox edtURL = null;
        [MVControlReference("chcMethod")]
        private ICombo chcMethod = null;
        [MVControlReference("edtPayload")]
        private ITextBox edtPayload = null;

        private enum EVENTS
        {
            LOGIN = 0x0013,
            LOGOUT, // No GameEvent for logout
            TELL = 0x0038,
            DEATH = 0x01AC,
            LEVEL = 0x02BD
        };

        protected override void Startup()
        {
            try
            {
                Globals.Init("TownCrier", Host, Core);

                MVWireupHelper.WireupStart(this, Host);

                //Webhook hookA = new Webhook("SMS", 
                //"https://hooks.zapier.com/hooks/catch/1226461/0ie1ok/?message=@",
                //"GET");
                //Webhook hookB = new Webhook("Discord", 
                //"https://discordapp.com/api/webhooks/531740310674604043/wU1FqslYss6aAlEZ_IPVCHumK53J8hY_BcLVYxjWcpuJwgS4TaI8RIDInYp2zKeSeFy3", 
                //"POST", 
                //"{\"content\": \"@\"}");

                actions = new List<Action>();
                webhooks = new List<Webhook>();

                //actions.Add(new Action((int)EVENTS.LOGIN, hookB.Name));
                //actions.Add(new Action((int)EVENTS.LOGOUT, hookB.Name));

                //webhooks.Add(hookA);
                //webhooks.Add(hookB);

                // UI
                RefreshUI();
                PopulateConstantChoiceElements(); // Choice dropdowns that don't change

                // Settings
                LoadState();

                lstActions.Click += lstActions_Click;
                lstWebhooks.Click += lstWebhooks_Click;
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        protected override void Shutdown()
        {
            try
            {
                MVWireupHelper.WireupEnd(this);
                lstActions.Click -= lstActions_Click;

            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        public void LoadState()
        {
            try
            {
                actions.Clear();
                webhooks.Clear();

                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + 
                    @"\Asheron's Call\" + 
                    Globals.PluginName + 
                    "-settings.txt";

                if (!File.Exists(path))
                {
                    return;
                }

                Util.WriteToChat("Starting to stream settings file...");
                using (StreamReader reader = new StreamReader(path))
                {
                    string line;

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();
                        Util.WriteToChat("LoadSetting(" + line + ")");
                        LoadSetting(line);
                    }
                }

                RefreshEventsList();
                RefreshWebhooksList();
                RefreshWebhooksChoice();
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        public void LoadSetting(string line)
        {
            try
            {
                string[] tokens = line.Split('\t');

                if (tokens.Length < 4) // Minimum # for actions or webhooks
                {
                    return;
                }

                switch (tokens[0])
                {
                    case "action":
                        if (tokens.Length != 4)
                        {
                            return;
                        }

                        actions.Add(new Action(int.Parse(tokens[1]), tokens[2], bool.Parse(tokens[3])));

                        break;
                    case "webhook":
                        if (tokens.Length == 4)
                        {
                             webhooks.Add(new Webhook(tokens[1], tokens[2], tokens[3]));
                        }
                        else if (tokens.Length == 5)
                        {
                            webhooks.Add(new Webhook(tokens[1], tokens[2], tokens[3], tokens[4]));
                        }

                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);    
            }
        }
        public void SaveState()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + 
                    @"\Asheron's Call\" + 
                    Globals.PluginName + 
                    "-settings.txt";

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    foreach (Action action in actions)
                    {
                        writer.WriteLine(action.ToString());
                    }

                    foreach (Webhook webhook in webhooks)
                    {
                        writer.WriteLine(webhook.ToString());
                    }

                    writer.Close();
                }
            }            
            catch (Exception ex) { Util.LogError(ex); }
        }
            

        private void RefreshUI()
        {
            try
            {
                RefreshWebhooksChoice();
                RefreshWebhooksList();
                RefreshEventsList();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
            
        }

        private void RefreshEventsList()
        {
            try
            {
                lstActions.Clear();

                foreach (var action in actions)
                {
                    IListRow row = lstActions.Add();

                    row[0][0] = "EVENT" + action.Event.ToString();
                    row[1][0] = action.WebhookName;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }

        }

        private void RefreshWebhooksList()
        {
            try
            {
                lstWebhooks.Clear();

                foreach (var webhook in webhooks)
                {
                    Util.WriteToChat("Adding a new webhook to the list with name " + webhook.Name);

                    IListRow row = lstWebhooks.Add();

                    row[0][0] = webhook.Name;
                    row[1][0] = webhook.BaseURI.ToString();
                    row[2][0] = webhook.Method;
                    row[3][0] = webhook.Payload;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
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
                    chcEventsWebhook.Add(webhook.Name, webhook.Name);
                }

                chcEventsWebhook.Selected = 0;
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void PopulateConstantChoiceElements()
        {
            try
            {
                chcMethod.Add("GET", "GET");
                chcMethod.Add("POST", "POST");

                chcEventsEvent.Add("You log in", EVENTS.LOGIN);
                chcEventsEvent.Add("You log out", EVENTS.LOGOUT);
                chcEventsEvent.Add("You die", EVENTS.DEATH);
                chcEventsEvent.Add("You receive an @tell", EVENTS.TELL);
                chcEventsEvent.Add("You level up", EVENTS.LEVEL);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }


        [BaseEvent("LoginComplete", "CharacterFilter")]
        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            try
            {
                

            }
            catch (Exception ex) { Util.LogError(ex); }
        }


        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
                TriggerActionsForEvent((int)EVENTS.LOGOUT, "Logout...");
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
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
                    int eventId = (int)e.Message["event"];

                    Util.WriteToChat("Game event = " + eventId.ToString());

                    switch (eventId) {
                        case (int)EVENTS.LOGIN:
                            TriggerActionsForEvent((int)EVENTS.LOGIN, "LOGIN");
                            break;
                        case (int)EVENTS.DEATH:
                            TriggerActionsForEvent((int)EVENTS.DEATH, "Death");
                            break;
                        case (int)EVENTS.TELL:
                            TriggerActionsForEvent((int)EVENTS.TELL, "TELL");
                            break;
                        case (int)EVENTS.LEVEL:
                            TriggerActionsForEvent((int)EVENTS.LEVEL, "LEVEL");
                            break;
                        default:
                            break;
                    }

                    
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }

        }

        private void TriggerActionsForEvent(int eventId, string message)
        {
            try
            {
                List<Action> matched = actions.FindAll(action => action.Event == eventId);

                foreach (Action action in matched)
                {
                    TriggerWebhooksForAction(action, new WebhookMessage(message));
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void TriggerWebhooksForAction(Action action, WebhookMessage message)
        {
            try
            {
                List<Webhook> matched = webhooks.FindAll(w => w.Name == action.WebhookName);

                foreach (Webhook webhook in matched)
                {
                    webhook.Send(message);
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
                    (string)chcEventsWebhook.Data[chcEventsWebhook.Selected]);

                actions.Add(action);

                RefreshEventsList();
            }
            catch (Exception ex)
            {
                Util.WriteToChat(ex.Message);
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnWebhookAdd", "Click")]
        void btnWebhookAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                Util.WriteToChat("btnWebhookAdd Clicked");

                // Stop if the name isn't unique
                if (webhooks != null && webhooks.Count > 0)
                {
                    List<Webhook> found = webhooks.FindAll(w => w.Name == edtName.Text);

                    if (found.Count > 0)
                    {
                        Util.WriteToChat("Couldn't add new webhook: You must give your webhooks unique names.");
                        return;
                    }
                }

                Webhook webhook = new Webhook(edtName.Text, edtURL.Text, (string)chcMethod.Data[chcMethod.Selected], edtPayload.Text);
                webhooks.Add(webhook);

                Util.WriteToChat("Adding new webhook with " + webhook.Name + webhook.BaseURI.ToString() + webhook.Method + webhook.Payload);

                RefreshWebhooksList();
                RefreshWebhooksChoice();
            }
            catch (Exception ex)
            {
                Util.WriteToChat(ex.Message);
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnLoad", "Click")]
        void btnLoad_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                Util.WriteToChat("btnLoad Clicked");

                LoadState();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnSave", "Click")]
        void btnSave_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                Util.WriteToChat("btnSave Clicked");

                SaveState();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }


        private void lstActions_Click(object sender, int row, int col)
        {
            Util.WriteToChat("Clicked on list row " + row.ToString() + " and col " + col.ToString());

            if (col == 4)
            {
                // TODO: Test action
            }
            else if (col == 3)
            {
                Util.WriteToChat("Deleting webhook");

                actions.RemoveAt(row);
                RefreshEventsList();
            }
        }

        private void lstWebhooks_Click(object sender, int row, int col)
        {
            Util.WriteToChat("Clicked on list row " + row.ToString() + " and col " + col.ToString());

            if (col == 4)
            {
                // TODO: Test webhook
            }
            else if (col == 5)
            {
                Util.WriteToChat("Deleting webhook...");

                webhooks.RemoveAt(row);
                RefreshWebhooksChoice();
                RefreshWebhooksList();
            }
        }
    }
}
