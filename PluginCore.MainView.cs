using System;
using System.Collections.Generic;

using Decal.Adapter;
using MyClasses.MetaViewWrappers;

namespace TownCrier
{
    [MVView("TownCrier.mainView.xml")]
    [MVWireUpControlEvents]
    public partial class PluginCore : PluginBase
    {
        // Timed
        [MVControlReference("edtTimedTriggerMinutes")]
        private ITextBox edtTimedTriggerMinutes = null;
        [MVControlReference("edtTimedTriggerMessage")]
        private ITextBox edtTimedTriggerMessage = null;
        [MVControlReference("chcTimedTriggerWebhook")]
        private ICombo chcTimedTriggerWebhook = null;
        [MVControlReference("lstTimedTriggers")]
        private IList lstTimedTriggers = null;
        private struct TimedTriggersList
        {
            public const int Enabled = 0, Minutes = 1, Webhook = 2, Message = 3, Delete = 4;
        }

        // Events
        [MVControlReference("edtEventsMessage")]
        private ITextBox edtEventsMessage = null;
        [MVControlReference("chcEventsWebhook")]
        private ICombo chcEventsWebhook = null;
        [MVControlReference("chcEventTriggerEvent")]
        private ICombo chcEventTriggerEvent = null;
        [MVControlReference("lstEventTriggers")]
        private IList lstEventTriggers = null;
        private struct EventTriggersList
        {
            public const int Enabled = 0, Event = 1, Webhook = 2, MessageFormat = 3, Delete = 4;
        }

        // Chat
        [MVControlReference("edtChatTriggerMessage")]
        private ITextBox edtChatTriggerMessage = null;
        [MVControlReference("chcChatTriggerWebhook")]
        private ICombo chcChatTriggerWebhook = null;
        [MVControlReference("edtChatTriggerPattern")]
        private ITextBox edtChatTriggerPattern = null;
        [MVControlReference("lstChatTriggers")]
        private IList lstChatTriggers = null;
        private struct ChatTriggersList
        {
            public const int Enabled = 0, Pattern = 1, Webhook = 2, Message = 3, Delete = 4;
        }

        // Webhooks
        [MVControlReference("edtName")]
        private ITextBox edtName = null;
        [MVControlReference("edtURL")]
        private ITextBox edtURL = null;
        [MVControlReference("chcMethod")]
        private ICombo chcMethod = null;
        [MVControlReference("edtPayload")]
        private ITextBox edtPayload = null;
        [MVControlReference("lstWebhooks")]
        private IList lstWebhooks = null;
        private struct WebhooksList
        {
            public const int Name = 0, URL = 1, Method = 2, Payload = 3, Test = 4, Delete = 5;
        }

        [MVControlEvent("btnEventTriggerAdd", "Click")]
        void btnEventTriggerAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                EventTrigger trigger = new EventTrigger(
                    (string)chcEventTriggerEvent.Data[chcEventTriggerEvent.Selected],
                    (string)chcEventsWebhook.Data[chcEventsWebhook.Selected],
                    edtEventsMessage.Text,
                    true);

                Globals.EventTriggers.Add(trigger);

                RefreshEventTriggerList();
                SaveProfile();
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error adding new EventTrigger: " + ex.Message);
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnTimedTriggerAdd", "Click")]
        void btnTimedTriggerAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                if (int.Parse(edtTimedTriggerMinutes.Text) <= 0)
                {
                    throw new Exception("Value for Minutes must be a whole number greater than 0.");
                }

                if (edtTimedTriggerMessage.Text.Length <= 0)
                {
                    throw new Exception("You have to enter a Message for your Timer.");
                }

                TimedTrigger trigger = new TimedTrigger(
                    int.Parse(edtTimedTriggerMinutes.Text),
                    (string)chcTimedTriggerWebhook.Data[chcTimedTriggerWebhook.Selected],
                    edtTimedTriggerMessage.Text,
                    true);

                Globals.TimedTriggers.Add(trigger);

                RefreshTimedTriggerList();
                SaveProfile();
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error adding new Timer: " + ex.Message);
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnChatTriggerAdd", "Click")]
        void btnChatTriggerAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                if (edtChatTriggerPattern.Text.Length <= 0)
                {
                    throw new Exception("You have to enter a Pattern for your ChatTrigger.");
                }

                ChatTrigger trigger = new ChatTrigger(
                    edtChatTriggerPattern.Text,
                    (string)chcChatTriggerWebhook.Data[chcChatTriggerWebhook.Selected],
                    edtChatTriggerMessage.Text,
                    true);

                Globals.ChatTriggers.Add(trigger);

                RefreshChatTriggerList();
                SaveProfile();
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error adding new ChatTrigger: " + ex.Message);
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnWebhookAdd", "Click")]
        void btnWebhookAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                // Webhooks need names
                if (edtName.Text.Length <= 0)
                {
                    Util.WriteToChat("Webhooks must have names.");
                    throw new Exception("Webhooks must have names");
                }

                // Either the URL or the PayloadFormatString should have an @ symbol, but just warn
                if (!(edtName.Text.Contains("@") || edtPayload.Text.Contains("@")))
                {
                    Util.WriteToChat("Warning: Neither your URL or JSON had an @ symbol in them which means your webhooks will trigger without a message.");
                }

                // Stop if the name isn't unique
                if (Globals.Webhooks != null && Globals.Webhooks.Count > 0)
                {
                    List<Webhook> found = Globals.Webhooks.FindAll(w => w.Name == edtName.Text);

                    if (found.Count > 0)
                    {
                        throw new Exception("A webhook with this name already exists");
                    }
                }

                Webhook webhook = new Webhook(edtName.Text, edtURL.Text, (string)chcMethod.Data[chcMethod.Selected], edtPayload.Text);
                webhook.Save();
                Globals.Webhooks.Add(webhook);

                RefreshWebhooksList();
                RefreshEventTriggerWebhookChoice();
                RefreshTimedTriggerWebhookChoice();
                RefreshChatTriggerWebhookChoice();
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error adding new Webhook: " + ex.Message);
                Util.LogError(ex);
            }
        }

        [MVControlEvent("lstEventTriggers", "Click")]
        private void lstEventTriggers_Click(object sender, int row, int col)
        {
            try
            {
                switch (col)
                {
                    case EventTriggersList.Enabled:
                        bool enabled = (bool)lstEventTriggers[row][col][0];

                        if (enabled)
                        {
                            Globals.EventTriggers[row].Enable();
                        }
                        else
                        {
                            Globals.EventTriggers[row].Disable();
                        }

                        SaveProfile();

                        break;
                    case EventTriggersList.Delete:
                        Globals.EventTriggers.RemoveAt(row);
                        RefreshEventTriggerList();

                        SaveProfile();

                        break;
                    default:
                        PrintEventTrigger(Globals.EventTriggers[row]);

                        break;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("lstTimedTriggers", "Click")]
        private void lstTimedTriggers_Click(object sender, int row, int col)
        {
            try
            {
                switch (col)
                {
                    case TimedTriggersList.Enabled:
                        bool enabled = (bool)lstTimedTriggers[row][col][0];

                        if (enabled)
                        {
                            Globals.TimedTriggers[row].Enable();
                        }
                        else
                        {
                            Globals.TimedTriggers[row].Disable();
                        }

                        SaveProfile();

                        break;
                    case TimedTriggersList.Delete:
                        Globals.TimedTriggers[row].Dispose();
                        Globals.TimedTriggers.RemoveAt(row);
                        RefreshTimedTriggerList();

                        SaveProfile();

                        break;
                    default:
                        PrintTimedTrigger(Globals.TimedTriggers[row]);

                        break;
                };
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("lstChatTriggers", "Click")]
        private void lstChatTriggers_Click(object sender, int row, int col)
        {
            try
            {
                switch (col)
                {
                    case ChatTriggersList.Enabled:
                        bool enabled = (bool)lstChatTriggers[row][col][0];

                        if (enabled)
                        {
                            Globals.ChatTriggers[row].Enable();
                        }
                        else
                        {
                            Globals.ChatTriggers[row].Disable();
                        }

                        SaveProfile();

                        break;
                    case ChatTriggersList.Delete:
                        Globals.ChatTriggers.RemoveAt(row);
                        RefreshChatTriggerList();

                        SaveProfile();

                        break;
                    default:
                        PrintChatTrigger(Globals.ChatTriggers[row]);

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
                    case WebhooksList.Test:
                        Globals.Webhooks[row].Send(new WebhookMessage("", "Testing webhook."));

                        break;
                    case WebhooksList.Delete:
                        Globals.Webhooks.RemoveAt(row);
                        RefreshEventTriggerWebhookChoice();
                        RefreshTimedTriggerWebhookChoice();
                        RefreshChatTriggerWebhookChoice();
                        RefreshWebhooksList();

                        SaveProfile();

                        break;
                    default:
                        PrintWebhook(Globals.Webhooks[row]);

                        break;
                };
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshUI()
        {
            try
            {
                RefreshEventTriggerWebhookChoice();
                RefreshTimedTriggerWebhookChoice();
                RefreshChatTriggerWebhookChoice();
                RefreshWebhooksList();
                RefreshTimedTriggerList();
                RefreshEventTriggerList();
                RefreshChatTriggerList();
                RefreshSettings();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshEventTriggerList()
        {
            try
            {
                lstEventTriggers.Clear();

                foreach (var trigger in Globals.EventTriggers)
                {
                    IListRow row = lstEventTriggers.Add();

                    row[EventTriggersList.Enabled][0] = trigger.Enabled;
                    row[EventTriggersList.Event][0] = trigger.Event;
                    row[EventTriggersList.Webhook][0] = trigger.WebhookName;
                    row[EventTriggersList.MessageFormat][0] = trigger.MessageFormat;
                    row[EventTriggersList.Delete][1] = Icons.Delete;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshTimedTriggerList()
        {
            try
            {
                lstTimedTriggers.Clear();

                foreach (var timer in Globals.TimedTriggers)
                {
                    IListRow row = lstTimedTriggers.Add();

                    row[TimedTriggersList.Enabled][0] = timer.Enabled;
                    row[TimedTriggersList.Minutes][0] = timer.Minute.ToString();
                    row[TimedTriggersList.Webhook][0] = timer.WebhookName;
                    row[TimedTriggersList.Message][0] = timer.Message;
                    row[TimedTriggersList.Delete][1] = Icons.Delete;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshChatTriggerList()
        {
            try
            {
                lstChatTriggers.Clear();

                foreach (var trigger in Globals.ChatTriggers)
                {
                    IListRow row = lstChatTriggers.Add();

                    row[ChatTriggersList.Enabled][0] = trigger.Enabled;
                    row[ChatTriggersList.Pattern][0] = trigger.Pattern.ToString();
                    row[ChatTriggersList.Webhook][0] = trigger.WebhookName;
                    row[ChatTriggersList.Message][0] = trigger.MessageFormat;
                    row[ChatTriggersList.Delete][1] = Icons.Delete;
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

                foreach (var webhook in Globals.Webhooks)
                {
                    IListRow row = lstWebhooks.Add();

                    row[WebhooksList.Name][0] = webhook.Name;
                    row[WebhooksList.URL][0] = webhook.URLFormatString.ToString();
                    row[WebhooksList.Method][0] = webhook.Method;
                    row[WebhooksList.Payload][0] = webhook.PayloadFormatString;
                    row[WebhooksList.Test][0] = "Test";
                    row[WebhooksList.Delete][1] = Icons.Delete;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshEventTriggerWebhookChoice()
        {
            try
            {
                chcEventsWebhook.Clear();

                foreach (var webhook in Globals.Webhooks)
                {
                    chcEventsWebhook.Add(webhook.Name, webhook.Name);
                }

                chcEventsWebhook.Selected = 0;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshTimedTriggerWebhookChoice()
        {
            try
            {
                chcTimedTriggerWebhook.Clear();

                foreach (var webhook in Globals.Webhooks)
                {
                    chcTimedTriggerWebhook.Add(webhook.Name, webhook.Name);
                }

                chcTimedTriggerWebhook.Selected = 0;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshChatTriggerWebhookChoice()
        {
            try
            {
                chcChatTriggerWebhook.Clear();

                foreach (var webhook in Globals.Webhooks)
                {
                    chcChatTriggerWebhook.Add(webhook.Name, webhook.Name);
                }

                chcChatTriggerWebhook.Selected = 0;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshSettings()
        {
            // TODO
        }

        private void PopulateEventChoices()
        {
            try
            {
                chcEventTriggerEvent.Add(EVENTDESC.LOGIN, EVENTS.LOGIN);
                chcEventTriggerEvent.Add(EVENTDESC.LOGOFF, EVENTS.LOGOFF);
                chcEventTriggerEvent.Add(EVENTDESC.LEVEL, EVENTS.LEVEL);
                chcEventTriggerEvent.Add(EVENTDESC.DEATH, EVENTS.DEATH);
                chcEventTriggerEvent.Add(EVENTDESC.DROPONDEATH, EVENTS.DROPONDEATH);
                chcEventTriggerEvent.Add(EVENTDESC.RARE, EVENTS.RARE);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
