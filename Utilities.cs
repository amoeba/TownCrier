using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TownCrier
{
	public static class Utilities
	{
        private static int logFileSizeLimit = 1048576; // 1MiB 

        private struct LOGFILE
        {
            public const string MESSAGE = "Messages.txt";
            public const string ERROR = "Errors.txt";
        }

        public static string GetSharedProfilesDirectory()
        {
            string path = null;

            try
            {
                path = string.Format(@"{0}\SharedProfiles", Globals.PluginDirectory);
            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);
            }

            return path;
        }

        public static string GetWebhookDirectory()
        {
            string path = null;
            try
            {
                path = String.Format(@"{0}\{1}", Globals.PluginDirectory, "Webhooks");
            }
            catch (Exception ex) { Utilities.LogError(ex); }

            return path;
        }

        public static string GetPlayerSpecificFolder()
        {
            string path = null;

            try
            {
                path = String.Format(@"{0}\{1}\{2}", Globals.PluginDirectory, Globals.Server, Globals.Name);
            }
            catch (Exception ex) { Utilities.LogError(ex); }

            return path;
        }

        public static string GetPlayerSpecificFile(string filename)
        {
            string path = null;

            try
            {
                path = String.Format(@"{0}\{1}", GetPlayerSpecificFolder(), filename);

            }
            catch (Exception ex) { Utilities.LogError(ex); }

            return path;
        }

        public static void EnsurePathExists(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public static string GetProfilePath()
        {
            if (Globals.CurrentProfile != null && Globals.CurrentProfile.Length > 0)
            {
                Utilities.EnsurePathExists(GetSharedProfilesDirectory());
                return string.Format(@"{0}\{1}.json", GetSharedProfilesDirectory(), Globals.CurrentProfile);
            }
            else
            {
                Utilities.EnsurePathExists(Utilities.GetPlayerSpecificFolder());
                return Utilities.GetPlayerSpecificFile("Profile.json");
            }
        }

        public static void LogError(Exception ex)
		{
			try
			{
                if (ex == null)
                {
                    ex = new Exception("ex was null on LogError, replaced with this message");
                }

                string path = null;
                bool append = true;

                // Fall back to saving in a global Errors.txt when not logged in
                if (!Globals.IsLoggedIn)
                {
                    path = string.Format(@"{0}\{1}", Globals.PluginDirectory, LOGFILE.ERROR);
                }
                else
                {
                    EnsurePathExists(Utilities.GetPlayerSpecificFolder());
                    path = Utilities.GetPlayerSpecificFile(LOGFILE.ERROR);
                }

                // Determine whether we should cut the log file off
                FileInfo fi = new FileInfo(path);

                if (fi.Exists && fi.Length > logFileSizeLimit)
                {
                    append = false;
                }

                using (StreamWriter writer = new StreamWriter(path, append))
				{
					writer.WriteLine("==" +
                        "==========================================================================");
					writer.WriteLine(DateTime.Now.ToString());
					writer.WriteLine("Error: " + ex.Message);
					writer.WriteLine("Source: " + ex.Source);
					writer.WriteLine("Stack: " + ex.StackTrace);
					if (ex.InnerException != null)
					{
						writer.WriteLine("Inner: " + ex.InnerException.Message);
						writer.WriteLine("Inner Stack: " + ex.InnerException.StackTrace);
					}
					writer.WriteLine("============================================================================");
					writer.WriteLine("");
				}
			}
			catch
			{
                // Nothing
			}
		}

        public static void LogMessage(string message)
        {
            try
            {
                string path = null;
                bool append = true;

                // Fall back to saving in a global Messages.txt when not logged in
                if (!Globals.IsLoggedIn)
                {
                    path = string.Format(@"{0}\{1}", Globals.PluginDirectory, LOGFILE.MESSAGE);
                }
                else
                {
                    EnsurePathExists(Utilities.GetPlayerSpecificFolder());
                    path = Utilities.GetPlayerSpecificFile(LOGFILE.MESSAGE);
                }

                // Determine whether we should cut the log file off
                FileInfo fi = new FileInfo(path);

                if (fi.Exists && fi.Length > logFileSizeLimit)
                {
                    append = false;
                }

                using (StreamWriter writer = new StreamWriter(path, append))
                {
                    writer.WriteLine(String.Format("{0}: {1}", DateTime.Now.ToString(), message));
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        public static void WriteToChat(string message)
		{
			try
            {
				Globals.Host.Actions.AddChatText("[" + Globals.PluginName + "] " + message, 1);
			}
			catch (Exception ex)
            {
                LogError(ex);
            }
		}

        /** 
         * GetWeenieProperty
         * 
         * Given to me by Yonneh & parad0x.
         * 
         * It turns out that Decal events run before the client sees the
         * network event hits the client so we can ask the client for 
         * what it knows about the object inside the event handler to get
         * the previous state.
         */
        public static int GetWeenieProperty(int weenie, int property)
        {
            int wielderID = 0;

            IntPtr weeniePtr = (IntPtr)Decal.Adapter.CoreManager.Current.Actions.Underlying.GetWeenieObjectPtr(weenie);

            if (weeniePtr != IntPtr.Zero)
            {
                wielderID = Marshal.ReadInt32(weeniePtr, property);
            }

            return wielderID;
        }
        public static int GetObjectOldContainer(int id)
        {
            return GetWeenieProperty(id, 0xB4);
        }

        public static int GetObjectOldWeilder(int id)
        {
            return GetWeenieProperty(id, 0xB8);
        }

        public static string MaybeURLEncode(string message, bool escape)
        {
 
            if (escape)
            {
                return Uri.EscapeUriString(message);
            }
            else
            {
                return message;
            }
        }

        public static string SubstituteVariables(string target, bool escape)
        {
            string modified = target;

            try
            { 
                if (modified.Contains("$NAME"))
                {
                    modified = modified.Replace("$NAME", MaybeURLEncode(Globals.Core.CharacterFilter.Name, escape));
                }

                if (modified.Contains("$SERVER"))
                {
                    modified = modified.Replace("$SERVER", MaybeURLEncode(Globals.Core.CharacterFilter.Server, escape));
                }

                if (modified.Contains("$LEVEL"))
                {
                    modified = modified.Replace("$LEVEL", MaybeURLEncode(Globals.Core.CharacterFilter.Level.ToString(), escape));
                }

                if (modified.Contains("$UXP"))
                {
                    modified = modified.Replace("$UXP", MaybeURLEncode(string.Format("{0:#,##0}", Globals.Core.CharacterFilter.UnassignedXP), escape));
                }

                if (modified.Contains("$TXP"))
                {
                    modified = modified.Replace("$TXP", MaybeURLEncode(string.Format("{0:#,##0}", Globals.Core.CharacterFilter.TotalXP), escape));
                }

                if (modified.Contains("$HEALTH"))
                {
                    modified = modified.Replace("$HEALTH", MaybeURLEncode(Globals.Core.CharacterFilter.Health.ToString(), escape));
                }

                if (modified.Contains("$STAMINA"))
                {
                    modified = modified.Replace("$STAMINA", MaybeURLEncode(Globals.Core.CharacterFilter.Stamina.ToString(), escape));
                }

                if (modified.Contains("$MANA"))
                {
                    modified = modified.Replace("$MANA", MaybeURLEncode(Globals.Core.CharacterFilter.Mana.ToString(), escape));
                }

                if (modified.Contains("$VITAE"))
                {
                    modified = modified.Replace("$VITAE", MaybeURLEncode(Globals.Core.CharacterFilter.Vitae.ToString() + "%", escape));
                }

                if (modified.Contains("$LOC"))
                {
                    modified = modified.Replace("$LOC", MaybeURLEncode(new Location(Globals.Host.Actions.Landcell, Globals.Host.Actions.LocationX, Globals.Host.Actions.LocationY).ToString(), escape));
                }

                if (modified.Contains("$DATETIME"))
                {
                    DateTime now = DateTime.Now;

                    modified = modified.Replace(
                        "$DATETIME",
                        MaybeURLEncode(
                            String.Format("{0} {1}",
                                now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern),
                                now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern)),
                            escape));
                }

                if (modified.Contains("$DATE"))
                {
                    modified = modified.Replace(
                        "$DATE",
                        MaybeURLEncode(DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern), escape));
                }

                if (modified.Contains("$TIME"))
                {
                    modified = modified.Replace(
                        "$TIME",
                        MaybeURLEncode(DateTime.Now.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern), escape));
                }

                return modified;
            }
            catch (Exception ex)
            {
                LogError(ex);

                return modified;
            }
        }
    }
}
