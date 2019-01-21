using System;
using System.IO;

namespace TownCrier
{
	public static class Util
	{
        public static void EnsurePluginFolder()
        {
            try
            {
                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Decal Plugins"))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Decal Plugins");
                }

                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Decal Plugins\" + Globals.PluginName))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Decal Plugins\" + Globals.PluginName);
                }
            }
            catch (Exception ex) {
                LogError(ex);
            }
        }

        public static void LogError(Exception ex)
		{
			try
			{
                EnsurePluginFolder();

                string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Decal Plugins\" + Globals.PluginName + @"\errors.txt";

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
					writer.Close();
				}
			}
			catch
			{
			}
		}

        public static void LogInfo(string message)
        {
            if (!Settings.Verbose)
            {
                return;
            }

            WriteToChat("[INFO] " + message);
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
