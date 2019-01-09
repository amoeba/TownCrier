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

        // Events the plugin handles, superset of GameEvent
        private enum EVENT
        {
            LOGIN,
            LOGOUT,
            TELL,
            DEATH,
            LEVEL
        };

        // Just GameEvent events the plugin handles
        private enum GAMEEVENT
        {
            LOGIN = 0x0013,
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
    
                // App state
                actions = new List<Action>();
                webhooks = new List<Webhook>();

                // UI
                RefreshUI();
                PopulateConstantChoiceElements(); // Choice dropdowns that don't change

                // Settings
                LoadSettings();

                // Events
                lstActions.Click += lstActions_Click;
                lstWebhooks.Click += lstWebhooks_Click;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        protected override void Shutdown()
        {
            try
            {
                MVWireupHelper.WireupEnd(this);
                lstActions.Click -= lstActions_Click;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        public void LoadSettings()
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

                using (StreamReader reader = new StreamReader(path))
                {
                    string line;

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();
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

        public void SaveSettings()
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

                    row[0][0] = Enum.GetName(typeof(EVENT), action.Event);
                    row[1][0] = action.WebhookName;
                    row[2][0] = "Test";
                    row[3][0] = "Delete";
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
                    IListRow row = lstWebhooks.Add();

                    row[0][0] = webhook.Name;
                    row[1][0] = webhook.BaseURI.ToString();
                    row[2][0] = webhook.Method;
                    row[3][0] = webhook.Payload;
                    row[4][0] = "Test";
                    row[5][0] = "Delete";
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

                chcEventsEvent.Add("You log in", EVENT.LOGIN);
                chcEventsEvent.Add("You log out", EVENT.LOGOUT);
                chcEventsEvent.Add("You die", EVENT.DEATH);
                chcEventsEvent.Add("You receive an @tell", EVENT.TELL);
                chcEventsEvent.Add("You level up", EVENT.LEVEL);
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
                LoadSettings();
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
                TriggerActionsForEvent((int)EVENT.LOGOUT, "Logout...");
                SaveSettings();
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

                    switch (eventId) {
                        case (int)GAMEEVENT.LOGIN:
                            TriggerActionsForEvent((int)EVENT.LOGIN, "LOGIN");
                            break;
                        case (int)GAMEEVENT.DEATH:
                            TriggerActionsForEvent((int)EVENT.DEATH, "Death");
                            break;
                        case (int)GAMEEVENT.TELL:
                            TriggerActionsForEvent((int)EVENT.TELL, "TELL");
                            break;
                        case (int)GAMEEVENT.LEVEL:
                            TriggerActionsForEvent((int)EVENT.LEVEL, "LEVEL");
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

        private void TriggerWebhook(string webhookName, WebhookMessage message)
        {
            try
            {
                List<Webhook> matched = webhooks.FindAll(w => w.Name == webhookName);

                if (matched.Count == 0)
                {
                    return;
                }

                matched[0].Send(message);
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
                Action action = new Action(
                    (int)chcEventsEvent.Data[chcEventsEvent.Selected], 
                    (string)chcEventsWebhook.Data[chcEventsWebhook.Selected]);

                actions.Add(action);

                RefreshEventsList();
                SaveSettings();
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error adding new action: " + ex.Message);
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnWebhookAdd", "Click")]
        void btnWebhookAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                // Stop if the name isn't unique
                if (webhooks != null && webhooks.Count > 0)
                {
                    List<Webhook> found = webhooks.FindAll(w => w.Name == edtName.Text);

                    if (found.Count > 0)
                    {
                        Util.WriteToChat("Couldn't add new webhook: Make sure to use unique names and valid URLs.");
                        return;
                    }
                }

                Webhook webhook = new Webhook(edtName.Text, edtURL.Text, (string)chcMethod.Data[chcMethod.Selected], edtPayload.Text);
                webhooks.Add(webhook);

                RefreshWebhooksList();
                RefreshWebhooksChoice();
                SaveSettings();
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error adding new webhook: " + ex.Message);
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnLoad", "Click")]
        void btnLoad_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                LoadSettings();
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
                SaveSettings();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }


        private void lstActions_Click(object sender, int row, int col)
        {
            if (col == 2)
            {
                if (row >= actions.Count)
                {
                    return;
                }

                TriggerWebhooksForAction(actions[row], new WebhookMessage("Testing webhook"));
            }
            else if (col == 3)
            {
                actions.RemoveAt(row);
                RefreshEventsList();
                SaveSettings();
            }
        }

        private void lstWebhooks_Click(object sender, int row, int col)
        {
            if (col == 4)
            {
                if (row >= webhooks.Count)
                {
                    return;
                }

                TriggerWebhook((string)lstWebhooks[row][0][0], new WebhookMessage("Testing webhook"));
            }
            else if (col == 5)
            {
                webhooks.RemoveAt(row);
                RefreshWebhooksChoice();
                RefreshWebhooksList();
                SaveSettings();
            }
        }
    }
}
