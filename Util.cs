using System;
using System.IO;
using System.Text;

namespace TownCrier
{
	public static class Util
	{
        internal static string GetPluginDirectory()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                sb.Append(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
                sb.Append(@"\");
                sb.Append("Decal Plugins");
                sb.Append(@"\");
                sb.Append(Globals.PluginName);
            }
            catch (Exception ex) { Util.LogError(ex); }

            return sb.ToString();
        }

        public static string GetPlayerSpecificFolder()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                sb.Append(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
                sb.Append(@"\");
                sb.Append("Decal Plugins");
                sb.Append(@"\");
                sb.Append(Globals.PluginName);
                sb.Append(@"\");
                sb.Append(Globals.Server);
                sb.Append(@"\");
                sb.Append(Globals.Name);
            }
            catch (Exception ex) { Util.LogError(ex); }

            return sb.ToString();
        }

        public static string GetPlayerSpecificFile(string filename)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                sb.Append(GetPlayerSpecificFolder());
                sb.Append(@"\");
                sb.Append(filename);
            }
            catch (Exception ex) { Util.LogError(ex); }

            return sb.ToString();
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

        public static void LogError(Exception ex)
		{
			try
			{
                EnsurePathExists(Util.GetPlayerSpecificFolder());
                String path = Util.GetPlayerSpecificFile("errors.txt");

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
                EnsurePathExists(Util.GetPlayerSpecificFolder());
                String path = Util.GetPlayerSpecificFile("messages.txt");

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
