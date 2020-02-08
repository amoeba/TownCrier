using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using Decal.Adapter;
using MyClasses.MetaViewWrappers;
using VirindiViewService;

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

        // Profiles
        [MVControlReference("chcProfile")]
        private ICombo chcProfile = null;

        HudView copyProfileView;
        VirindiViewService.Controls.HudFixedLayout copyProfileLayout;
        VirindiViewService.Controls.HudStaticText copyProfileStaticText;
        VirindiViewService.Controls.HudTextBox copyProfileNewName;
        VirindiViewService.Controls.HudButton copyProfileButton;

        [MVControlEvent("btnEventTriggerAdd", "Click")]
        void btnEventTriggerAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                if (chcEventsWebhook.Count <= 0)
                {
                    Util.WriteToChat("Please add a webhook first.");

                    return;
                }

                if (chcEventsWebhook.Selected >= chcEventsWebhook.Count)
                {
                    Util.WriteToChat("Invalid webhook selected.");

                    return;
                }

                EventTrigger trigger = new EventTrigger(
                    (string)chcEventTriggerEvent.Data[chcEventTriggerEvent.Selected],
                    (string)chcEventsWebhook.Data[chcEventsWebhook.Selected],
                    edtEventsMessage.Text.Trim(),
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

        [MVControlEvent("btnProfileCopyTo", "Click")]
        void btnProfileCopyTo_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                ShowProfileCopyTo();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        void ShowProfileCopyTo()
        {
            if (copyProfileView != null)
            {
                CleanUpCopyProfileHUD();
            }

            copyProfileView = new HudView("Copy Profile", 200, 70, new ACImage(0x1F69));
            copyProfileView.Location = new Point(100, 100);
            copyProfileView.Visible = true;

            copyProfileLayout = new VirindiViewService.Controls.HudFixedLayout();
            copyProfileView.Controls.HeadControl = copyProfileLayout;

            copyProfileStaticText = new VirindiViewService.Controls.HudStaticText();
            copyProfileLayout.AddControl(copyProfileStaticText, new Rectangle(10, 12, 75, 20));
            copyProfileStaticText.Text = "New Name";
            copyProfileNewName = new VirindiViewService.Controls.HudTextBox();
            copyProfileLayout.AddControl(copyProfileNewName, new Rectangle(75, 10, 100, 20));

            copyProfileButton = new VirindiViewService.Controls.HudButton();
            copyProfileButton.Text = "Copy";
            copyProfileLayout.AddControl(copyProfileButton, new Rectangle(10, 40, 50, 20));
            copyProfileButton.Hit += CopyProfile;
        }

        private void CopyProfile(object sender, EventArgs e)
        {
            try
            {
                if (copyProfileNewName == null)
                {
                    Util.WriteToChat("You must enter a new name. Profile not copied.");

                    return;
                }

                if (copyProfileNewName.Text.Length <= 0)
                {
                    Util.WriteToChat("New profile name must not be an empty string. Profile not copied.");

                    return;
                }

                System.Text.RegularExpressions.Regex x = new System.Text.RegularExpressions.Regex(@"[A-Za-z\d]+");

                if (!x.IsMatch(copyProfileNewName.Text))
                {
                    Util.WriteToChat("New profile name must only use letters and numbers. (Regex [A-Za-z\\d]+).");

                    return;
                }

                Globals.CurrentProfile = copyProfileNewName.Text;
                SaveCurrentProfileSetting();
                SaveProfile();
                RefreshProfileChoice();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
            finally
            {
                CleanUpCopyProfileHUD();
            }
        }

        private void CleanUpCopyProfileHUD()
        {
            try
            {
                if (copyProfileStaticText != null)
                {
                    copyProfileStaticText.Dispose();
                    copyProfileStaticText = null;
                }

                if (copyProfileButton != null)
                {
                    copyProfileButton.Hit -= CopyProfile;
                    copyProfileButton.Dispose();
                    copyProfileButton = null;
                }

                if (copyProfileLayout != null)
                {
                    copyProfileLayout.Dispose();
                    copyProfileLayout = null;
                }

                if (copyProfileView != null)
                {
                    copyProfileView.Dispose();
                    copyProfileView = null;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnProfileClear", "Click")]
        void btnProfileClear_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                ClearProfile();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnProfileRefresh", "Click")]
        void btnProfileRefresh_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                RefreshProfileChoice();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnTimedTriggerAdd", "Click")]
        void btnTimedTriggerAdd_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                if (chcTimedTriggerWebhook.Count <= 0)
                {
                    Util.WriteToChat("Please add a webhook first.");

                    return;
                }

                if (chcTimedTriggerWebhook.Selected >= chcTimedTriggerWebhook.Count)
                {
                    Util.WriteToChat("Invalid webhook selected.");

                    return;
                }

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
                    edtTimedTriggerMessage.Text.Trim(),
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
                if (chcChatTriggerWebhook.Count <= 0)
                {
                    Util.WriteToChat("Please add a webhook first.");

                    return;
                }

                if (chcChatTriggerWebhook.Selected >= chcChatTriggerWebhook.Count)
                {
                    Util.WriteToChat("Invalid webhook selected.");

                    return;
                }

                if (edtChatTriggerPattern.Text.Length <= 0)
                {
                    throw new Exception("You have to enter a Pattern for your ChatTrigger.");
                }

                ChatTrigger trigger = new ChatTrigger(
                    edtChatTriggerPattern.Text,
                    (string)chcChatTriggerWebhook.Data[chcChatTriggerWebhook.Selected],
                    edtChatTriggerMessage.Text.Trim(),
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
                if (!(edtURL.Text.Contains("@") || edtPayload.Text.Contains("@")))
                {
                    Util.WriteToChat("Warning: Neither your URL or JSON had an @ symbol in them which means your webhooks will trigger without a message.");
                }

                // Stop if the name isn't unique
                if (Globals.Webhooks != null && Globals.Webhooks.Count > 0)
                {
                    List<Webhook> found = Globals.Webhooks.FindAll(w => w.Name == edtName.Text.Trim());

                    if (found.Count > 0)
                    {
                        throw new Exception("A webhook with this name already exists");
                    }
                }

                Webhook webhook = new Webhook(edtName.Text.Trim(), edtURL.Text.Trim(), (string)chcMethod.Data[chcMethod.Selected], edtPayload.Text.Trim());
                SaveWebhook(webhook);
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
                        Util.LogMessage("Testing webhook " + Globals.Webhooks[row].Name);

                        WebhookRequest req = new WebhookRequest(Globals.Webhooks[row], "Test");
                        req.Send();

                        break;
                    case WebhooksList.Delete:
                        DeleteWebhook(Globals.Webhooks[row].Name);
                        RefreshEventTriggerWebhookChoice();
                        RefreshTimedTriggerWebhookChoice();
                        RefreshChatTriggerWebhookChoice();
                        RefreshWebhooksList();

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
                RefreshProfileChoice();
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
                    row[ChatTriggersList.Pattern][0] = trigger.Pattern;
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

        // Profile tab
        [MVControlEvent("chcProfile", "Change")]
        private void chcProfile_Change(object sender, MVIndexChangeEventArgs args)
        {
            try
            {
                Globals.CurrentProfile = (string)chcProfile.Data[chcProfile.Selected];

                SaveCurrentProfileSetting();
                LoadProfile();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        private void RefreshProfileChoice()
        {
            try
            {
                // Rebuild UI state from the profile directory's contents
                chcProfile.Clear();
                chcProfile.Add("[By char]", null); // Null means "[By char]"
                chcProfile.Selected = 0;

                // Stop now if there are no shared profiles
                string path = Util.GetSharedProfilesDirectory();

                if (!Directory.Exists(path))
                {
                    return;
                }

                string[] sharedProfiles = Directory.GetFiles(path);

                FileInfo fi;
                string file;
                string profileName;

                for (int i = 0; i < sharedProfiles.Length; i++)
                {
                    file = sharedProfiles[i];
                    fi = new FileInfo(file);
                    profileName = fi.Name.Replace(".json", "");
                    chcProfile.Add(profileName, profileName);

                    // Update selected item if we can
                    if (profileName == Globals.CurrentProfile)
                    {
                        chcProfile.Selected = i + 1; // + 1 because [By char] is 0
                    }
                }

                profileName = null;
                file = null;
                fi = null;
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

        [MVControlEvent("btnWebhookRefresh", "Click")]
        void btnWebhookRefresh_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                LoadWebhooks();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnEventsWebhookRefresh", "Click")]
        void btnEventsWebhookRefresh_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                LoadWebhooks();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("btnTimedTriggerWebhookRefresh", "Click")]
        void btnTimedTriggerWebhookRefresh_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                LoadWebhooks();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("chcChatTriggerWebhookRefresh", "Click")]
        void chcChatTriggerWebhookRefresh_Click(object sender, MVControlEventArgs e)
        {
            try
            {
                LoadWebhooks();
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
