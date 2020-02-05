using System;
using System.IO;
using System.Text;

namespace TownCrier
{
	public static class Util
	{
        internal static string GetWebhookDirectory()
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
                Util.EnsurePathExists(string.Format(@"{0}\{1}", Globals.PluginDirectory, "Profiles"));
                return string.Format(@"{0}\{1}\{2}.json", Globals.PluginDirectory, "Profiles", Globals.CurrentProfile);
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

                // Fall back to saving in a global errors.txt when not logged in
                if (!Globals.IsLoggedIn)
                {
                    path = string.Format(@"{0}\{1}", Globals.PluginDirectory, "errors.txt");
                }
                else
                {
                    EnsurePathExists(Util.GetPlayerSpecificFolder());
                    path = Util.GetPlayerSpecificFile("errors.txt");
                }

                using (StreamWriter writer = new StreamWriter(path, true))
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

                // Fall back to saving in a global errors.txt when not logged in
                if (!Globals.IsLoggedIn)
                {
                    path = string.Format(@"{0}\{1}", Globals.PluginDirectory, "messages.txt");
                }
                else
                {
                    EnsurePathExists(Util.GetPlayerSpecificFolder());
                    path = Util.GetPlayerSpecificFile("messages.txt");
                }

                using (StreamWriter writer = new StreamWriter(path, true))
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
