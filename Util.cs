using System;
using System.IO;

namespace TownCrier
{
	public static class Util
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
                Util.LogError(ex);
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
            catch (Exception ex) { Util.LogError(ex); }

            return path;
        }

        public static string GetPlayerSpecificFolder()
        {
            string path = null;

            try
            {
                path = String.Format(@"{0}\{1}\{2}", Globals.PluginDirectory, Globals.Server, Globals.Name);
            }
            catch (Exception ex) { Util.LogError(ex); }

            return path;
        }

        public static string GetPlayerSpecificFile(string filename)
        {
            string path = null;

            try
            {
                path = String.Format(@"{0}\{1}", GetPlayerSpecificFolder(), filename);

            }
            catch (Exception ex) { Util.LogError(ex); }

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
                Util.EnsurePathExists(GetSharedProfilesDirectory());
                return string.Format(@"{0}\{1}.json", GetSharedProfilesDirectory(), Globals.CurrentProfile);
            }
            else
            {
                Util.EnsurePathExists(Util.GetPlayerSpecificFolder());
                return Util.GetPlayerSpecificFile("Profile.json");
            }
        }

        public static void LogError(Exception ex)
		{
			try
			{
                string path = null;
                bool append = true;

                // Fall back to saving in a global Errors.txt when not logged in
                if (!Globals.IsLoggedIn)
                {
                    path = string.Format(@"{0}\{1}", Globals.PluginDirectory, LOGFILE.ERROR);
                }
                else
                {
                    EnsurePathExists(Util.GetPlayerSpecificFolder());
                    path = Util.GetPlayerSpecificFile(LOGFILE.ERROR);
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
                    EnsurePathExists(Util.GetPlayerSpecificFolder());
                    path = Util.GetPlayerSpecificFile(LOGFILE.ERROR);
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
	}
}
