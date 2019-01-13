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
        List<Timer> timers;
        List<Webhook> webhooks;

        [MVControlReference("lstWebhooks")]
        private IList lstWebhooks = null;

        [MVControlReference("lstTimers")]
        private IList lstTimers = null;

        [MVControlReference("lstActions")]
        private IList lstActions = null;

        [MVControlReference("chcEventsEvent")]
        private ICombo chcEventsEvent = null;

        [MVControlReference("chcTimersWebhook")]
        private ICombo chcTimersWebhook = null;

        [MVControlReference("chcEventsWebhook")]
        private ICombo chcEventsWebhook = null;

        [MVControlReference("edtTimersMinutes")]
        private ITextBox edtTimersMinutes = null;
        [MVControlReference("edtTimersMessage")]
        private ITextBox edtTimersMessage = null;

        [MVControlReference("edtName")]
        private ITextBox edtName = null;
        [MVControlReference("edtURL")]
        private ITextBox edtURL = null;
        [MVControlReference("chcMethod")]
        private ICombo chcMethod = null;
        [MVControlReference("edtPayload")]
        private ITextBox edtPayload = null;

        // Events the plugin handles, superset of GameEvent
        public enum EVENT
        {
            LOGIN,
            DEATH,
        };

        // Just GameEvent events the plugin handles
        private struct GAMEEVENT
        {
            public const int LOGIN = 0x0013;
            public const int TELL = 0x02BD;
            public const int DEATH = 0x01AC;
        };

        private struct Icons
        {
            public const int Delete = 0x060011F8;
        }

        protected override void Startup()
        {
            try
            {
                Globals.Init("TownCrier", Host, Core);
                MVWireupHelper.WireupStart(this, Host);
                
                // App state
                actions = new List<Action>();
                timers = new List<Timer>();
                webhooks = new List<Webhook>();

                // UI
                RefreshUI();
                PopulateConstantChoiceElements(); // Choice dropdowns that don't change

                // Settings
                LoadSettings();
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
                DisposeAllTimers();
                MVWireupHelper.WireupEnd(this);
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
                DisposeAllTimers();
                timers.Clear();
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

                RefreshUI();
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
                    case "timer":
                        if (tokens.Length != 5)
                        {
                            return;
                        }

                        // Look up webhook by name
                        List<Webhook> found = webhooks.FindAll(w => w.Name == tokens[2]);
                        
                        if (found.Count <= 0)
                        {
                            Util.WriteToChat("Could not find webhook by name " + tokens[2]);
                            break;
                        }

                        timers.Add(new Timer(int.Parse(tokens[1]), found[0], tokens[3], bool.Parse(tokens[4])));
                        
                        break;
                    case "webhook":
                        if (tokens.Length == 4)
                        {
                             webhooks.Add(new Webhook(tokens[1], tokens[2], tokens[3], null));
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
                        writer.WriteLine(action.ToSetting());
                    }

                    foreach (Webhook webhook in webhooks)
                    {
                        writer.WriteLine(webhook.ToSetting());
                    }

                    // Serialize this last because Timers webhooks are serialized by name
                    // and they need to get looked on load by name so all webhooks have
                    // to be present when timers are loaded
                    foreach (Timer timer in timers)
                    {
                        writer.WriteLine(timer.ToSetting());
                    }

                    writer.Close();
                }
            }            
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void DisposeAllTimers()
        {
            if (timers == null)
            {
                return;
            }

            foreach (Timer timer in timers)
            {
                timer.Dispose();
            }
        }

        private void RefreshUI()
        {
            try
            {
                RefreshActionsWebhooksChoice();
                RefreshTimersWebhooksChoice();
                RefreshWebhooksList();
                RefreshTimersList();
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

                    row[0][0] = action.Enabled;
                    row[1][0] = Enum.GetName(typeof(EVENT), action.Event);
                    row[2][0] = action.WebhookName;
                    row[3][1] = Icons.Delete;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }

        }

        private void RefreshTimersList()
        {
            try
            {
                lstTimers.Clear();

                foreach (var timer in timers)
                {
                    IListRow row = lstTimers.Add();

                    row[0][0] = timer.Enabled;
                    row[1][0] = timer.Minute.ToString();
                    row[2][0] = timer.Webhook.Name;
                    row[3][0] = timer.Message;
                    row[4][1] = Icons.Delete;
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
                    row[5][1] = Icons.Delete;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshActionsWebhooksChoice()
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
        private void RefreshTimersWebhooksChoice()
        {
            try
            {
                chcTimersWebhook.Clear();

                foreach (var webhook in webhooks)
                {
                    chcTimersWebhook.Add(webhook.Name, webhook.Name);
                }

                chcTimersWebhook.Selected = 0;
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
                chcEventsEvent.Add("You die", EVENT.DEATH);
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
                Core.CharacterFilter.Death += CharacterFilter_Death;

                TriggerActionsForEvent((int)EVENT.LOGIN, Core.CharacterFilter.Name + " has logged in");
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void CharacterFilter_Death(object sender, DeathEventArgs e)
        {
            TriggerActionsForEvent((int)EVENT.DEATH, Core.CharacterFilter.Name + " has died: " + e.Text);
        }

        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
                SaveSettings();
                Core.CharacterFilter.Death -= CharacterFilter_Death;
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
                    // TODO
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
                List<Action> matched = actions.FindAll(action => action.Enabled && action.Event == eventId);

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
                Action action = new Action(
                    (int)chcEventsEvent.Data[chcEventsEvent.Selected], 
                    (string)chcEventsWebhook.Data[chcEventsWebhook.Selected],
                    true);

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

        [MVControlEvent("btnTimersTimerAdd", "Click")]
        void btnTimersTimerAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                Webhook webhook = webhooks.Find(h => h.Name == (string)chcTimersWebhook.Data[chcTimersWebhook.Selected]);

                if (webhook == null)
                {
                    Util.WriteToChat("Failed to add webhook because it couldn't be found. This is a bad bug.");
                }

                Timer timer = new Timer(
                    int.Parse(edtTimersMinutes.Text),
                    webhook,
                    (string)edtTimersMessage.Text,
                    true);

                timers.Add(timer);

                RefreshTimersList();
                SaveSettings();
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error adding new timer: " + ex.Message);
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
                RefreshActionsWebhooksChoice();
                RefreshTimersWebhooksChoice();
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

        [MVControlEvent("btnFoo", "Click")]
        void btnFoo_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                Util.WriteToChat("...");
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("lstActions", "Click")]
        private void lstActions_Click(object sender, int row, int col)
        {
            try
            {
                switch (col)
                {
                    case 0:
                        bool enabled = (bool)lstActions[row][col][0];

                        if (enabled)
                        {
                            actions[row].Enable();
                        }
                        else
                        {
                            actions[row].Disable();
                        }

                        break;
                    case 3:
                        actions.RemoveAt(row);
                        RefreshEventsList();
                        SaveSettings();

                        break;
                    default:
                        PrintAction(actions[row]);

                        break;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("lstTimers", "Click")]
        private void lstTimers_Click(object sender, int row, int col)
        {
            try
            {
                switch (col)
                {
                    case 0:
                        bool enabled = (bool)lstTimers[row][col][0];

                        if (enabled)
                        {
                            timers[row].Enable();
                        }
                        else
                        {
                            timers[row].Disable();
                        }

                        
                        break;
                    case 4:
                        Timer timer = timers[row];
                        timer.Dispose();
                        timers.RemoveAt(row);

                        RefreshTimersList();
                        SaveSettings();

                        break;
                    default:
                        PrintTimer(timers[row]);

                        break;
                };
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("lstWebhooks", "Click")]
        private void lstWebhooks_Click(object sender, int row, int col)
        {
            try
            {
                switch (col)
                {
                    case 4:
                        webhooks[row].Send(new WebhookMessage("Testing webhook."));

                        break;
                    case 5:
                        webhooks.RemoveAt(row);
                        RefreshActionsWebhooksChoice();
                        RefreshTimersWebhooksChoice();
                        RefreshWebhooksList();
                        SaveSettings();

                        break;
                    default:
                        PrintWebhook(webhooks[row]);

                        break;
                };
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void PrintAction(Action action)
        {
            try
            {
                Util.WriteToChat(action.ToString());
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void PrintTimer(Timer timer)
        {
            try
            {
                Util.WriteToChat(timer.ToString());
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void PrintWebhook(Webhook webhook)
        {
            try
            {
                Util.WriteToChat(webhook.ToString());
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
