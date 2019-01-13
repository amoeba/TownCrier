using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using MyClasses.MetaViewWrappers;

namespace TownCrier
{
    [MVView("TownCrier.mainView.xml")]
    [MVWireUpControlEvents]
    public partial class PluginCore : PluginBase
    {
        [MVControlReference("lstActions")]
        private IList lstActions = null;
        private struct ActionsList
        {
            public const int Enabled = 0, Event = 1, Webhook = 2, Delete = 3;
        }

        [MVControlReference("lstTimers")]
        private IList lstTimers = null;
        private struct TimersList
        {
            public const int Enabled = 0, Minutes = 1, Webhook = 2, Message = 3, Delete = 4;
        }

        [MVControlReference("lstWebhooks")]
        private IList lstWebhooks = null;
        private struct WebhooksList
        {
            public const int Name = 0, URL = 1, Method = 2, Payload = 3, Test = 4, Delete = 5;
        }

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
                Util.WriteToChat("Error adding new Action: " + ex.Message);
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnTimersTimerAdd", "Click")]
        void btnTimersTimerAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                if (int.Parse(edtTimersMinutes.Text) <= 0)
                {
                    throw new Exception("Value for Minutes must be a whole number greater than 0.");
                }

                Webhook webhook = webhooks.Find(h => h.Name == (string)chcTimersWebhook.Data[chcTimersWebhook.Selected]);

                if (webhook == null)
                {
                    throw new Exception("Failed to add webhook because it couldn't be found. This is a bad bug.");
                }

                if (edtTimersMessage.Text.Length <= 0)
                {
                    throw new Exception("You have to enter a Message for your Timer.");
                }

                Timer timer = new Timer(
                    int.Parse(edtTimersMinutes.Text),
                    webhook,
                    edtTimersMessage.Text,
                    true);

                timers.Add(timer);

                RefreshTimersList();
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

                // Either the URL or the Payload should have an @ symbol, but just warn
                if (!edtName.Text.Contains("@") || !edtPayload.Text.Contains("@"))
                {
                    Util.WriteToChat("Warning: Neither your URL or Payload had an @ symbol in them which means your webhooks will trigger without a message.");
                }

                // Stop if the name isn't unique
                if (webhooks != null && webhooks.Count > 0)
                {
                    List<Webhook> found = webhooks.FindAll(w => w.Name == edtName.Text);

                    if (found.Count > 0)
                    {
                        throw new Exception("A webhook with this name already exists");
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
                    case ActionsList.Enabled:
                        bool enabled = (bool)lstActions[row][col][0];

                        if (enabled)
                        {
                            actions[row].Enable();
                        }
                        else
                        {
                            actions[row].Disable();
                        }

                        SaveSettings();

                        break;
                    case ActionsList.Delete:
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
                    case TimersList.Enabled:
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
                    case TimersList.Delete:
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
                    case WebhooksList.Test:
                        webhooks[row].Send(new WebhookMessage("Testing webhook."));

                        break;
                    case WebhooksList.Delete:
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

        [MVControlEvent("chckVerbose", "Change")]
        private void chkVerbose_Change(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                settings["verbose"] = e.Checked;
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

                    row[ActionsList.Enabled][0] = action.Enabled;
                    row[ActionsList.Event][0] = Enum.GetName(typeof(EVENT), action.Event);
                    row[ActionsList.Webhook][0] = action.WebhookName;
                    row[ActionsList.Delete][1] = Icons.Delete;
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

                    row[TimersList.Enabled][0] = timer.Enabled;
                    row[TimersList.Minutes][0] = timer.Minute.ToString();
                    row[TimersList.Webhook][0] = timer.Webhook.Name;
                    row[TimersList.Message][0] = timer.Message;
                    row[TimersList.Delete][1] = Icons.Delete;
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

                    row[WebhooksList.Name][0] = webhook.Name;
                    row[WebhooksList.URL][0] = webhook.BaseURI.ToString();
                    row[WebhooksList.Method][0] = webhook.Method;
                    row[WebhooksList.Payload][0] = webhook.Payload;
                    row[WebhooksList.Test][0] = "Test";
                    row[WebhooksList.Delete][1] = Icons.Delete;
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
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
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
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void PopulateEventChoices()
        {
            try
            {
                chcEventsEvent.Add("You log in", EVENT.LOGIN);
                chcEventsEvent.Add("You die", EVENT.DEATH);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
