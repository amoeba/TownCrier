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
        [MVControlReference("lstEventTriggers")]
        private IList lstEventTriggers = null;
        private struct EventTriggersList
        {
            public const int Enabled = 0, Event = 1, Webhook = 2, MessageFormat = 3, Delete = 4;
        }

        [MVControlReference("lstTimedTriggers")]
        private IList lstTimedTriggers = null;
        private struct TimedTriggersList
        {
            public const int Enabled = 0, Minutes = 1, Webhook = 2, Message = 3, Delete = 4;
        }

        [MVControlReference("lstWebhooks")]
        private IList lstWebhooks = null;
        private struct WebhooksList
        {
            public const int Name = 0, URL = 1, Method = 2, Payload = 3, Test = 4, Delete = 5;
        }

        [MVControlReference("chcEventTriggerEvent")]
        private ICombo chcEventTriggerEvent = null;

        [MVControlReference("chcTimedTriggerWebhook")]
        private ICombo chcTimedTriggerWebhook = null;

        [MVControlReference("chcEventsWebhook")]
        private ICombo chcEventsWebhook = null;

        [MVControlReference("edtTimedTriggerMinutes")]
        private ITextBox edtTimedTriggerMinutes = null;
        [MVControlReference("edtTimedTriggerMessage")]
        private ITextBox edtTimedTriggerMessage = null;

        [MVControlReference("edtEventsMessage")]
        private ITextBox edtEventsMessage = null;

        [MVControlReference("edtName")]
        private ITextBox edtName = null;
        [MVControlReference("edtURL")]
        private ITextBox edtURL = null;
        [MVControlReference("chcMethod")]
        private ICombo chcMethod = null;
        [MVControlReference("edtPayload")]
        private ITextBox edtPayload = null;

        [MVControlReference("chckVerbose")]
        private ICheckBox chckVerbose = null;

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

                EventTriggers.Add(trigger);

                RefreshEventTriggerList();
                SaveSettings();
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

                Webhook webhook = Webhooks.Find(h => h.Name == (string)chcTimedTriggerWebhook.Data[chcTimedTriggerWebhook.Selected]);

                if (webhook == null)
                {
                    throw new Exception("Failed to add webhook because it couldn't be found. This is a bad bug.");
                }

                if (edtTimedTriggerMessage.Text.Length <= 0)
                {
                    throw new Exception("You have to enter a Message for your Timer.");
                }

                TimedTrigger trigger = new TimedTrigger(
                    int.Parse(edtTimedTriggerMinutes.Text),
                    webhook,
                    edtTimedTriggerMessage.Text,
                    true);

                TimedTriggers.Add(trigger);

                RefreshTimedTriggerList();
                SaveSettings();
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error adding new Timer: " + ex.Message);
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
                    throw new Exception("Webhooks need to have names.");
                }

                // Either the URL or the PayloadFormatString should have an @ symbol, but just warn
                if (!edtName.Text.Contains("@") || !edtPayload.Text.Contains("@"))
                {
                    Util.WriteToChat("Warning: Neither your URL or PayloadFormatString had an @ symbol in them which means your webhooks will trigger without a message.");
                }

                // Stop if the name isn't unique
                if (Webhooks != null && Webhooks.Count > 0)
                {
                    List<Webhook> found = Webhooks.FindAll(w => w.Name == edtName.Text);

                    if (found.Count > 0)
                    {
                        throw new Exception("A webhook with this name already exists");
                    }
                }

                Webhook webhook = new Webhook(edtName.Text, edtURL.Text, (string)chcMethod.Data[chcMethod.Selected], edtPayload.Text);
                Webhooks.Add(webhook);

                RefreshWebhooksList();
                RefreshEventTriggerWebhookChoice();
                RefreshTimedTriggerWebhookChoice();
                SaveSettings();
            }
            catch (Exception ex)
            {
                Util.WriteToChat("Error adding new Webhook: " + ex.Message);
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
                Util.WriteToChat("btnFoo_Click()");
            }
            catch (Exception ex)
            {
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
                            EventTriggers[row].Enable();
                        }
                        else
                        {
                            EventTriggers[row].Disable();
                        }

                        SaveSettings();

                        break;
                    case EventTriggersList.Delete:
                        EventTriggers.RemoveAt(row);
                        RefreshEventTriggerList();

                        SaveSettings();

                        break;
                    default:
                        PrintEventTrigger(EventTriggers[row]);

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
                            TimedTriggers[row].Enable();
                        }
                        else
                        {
                            TimedTriggers[row].Disable();
                        }

                        SaveSettings();

                        break;
                    case TimedTriggersList.Delete:
                        TimedTriggers[row].Dispose();
                        TimedTriggers.RemoveAt(row);
                        RefreshTimedTriggerList();

                        SaveSettings();

                        break;
                    default:
                        PrintTimedTrigger(TimedTriggers[row]);

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
                        Webhooks[row].Send(new WebhookMessage("", "Testing webhook."));

                        break;
                    case WebhooksList.Delete:
                        Webhooks.RemoveAt(row);
                        RefreshEventTriggerWebhookChoice();
                        RefreshTimedTriggerWebhookChoice();
                        RefreshWebhooksList();

                        SaveSettings();

                        break;
                    default:
                        PrintWebhook(Webhooks[row]);

                        break;
                };
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("chckVerbose", "Change")]
        private void chkVerbose_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                Settings.Verbose = e.Checked;

                SaveSettings();
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
                RefreshWebhooksList();
                RefreshTimedTriggerList();
                RefreshEventTriggerList();
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

                foreach (var trigger in EventTriggers)
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

                foreach (var timer in TimedTriggers)
                {
                    IListRow row = lstTimedTriggers.Add();

                    row[TimedTriggersList.Enabled][0] = timer.Enabled;
                    row[TimedTriggersList.Minutes][0] = timer.Minute.ToString();
                    row[TimedTriggersList.Webhook][0] = timer.Webhook.Name;
                    row[TimedTriggersList.Message][0] = timer.Message;
                    row[TimedTriggersList.Delete][1] = Icons.Delete;
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

                foreach (var webhook in Webhooks)
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

                foreach (var webhook in Webhooks)
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

                foreach (var webhook in Webhooks)
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

        private void RefreshSettings()
        {
            chckVerbose.Checked = Settings.Verbose;
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
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
