using System;
using System.IO;
using System.Text;

namespace TownCrier
{
    public class Webhook
    {
        public string Name;
        public string Method;
        public string URLFormatString;
        public string PayloadFormatString;

        public Webhook(string name, string url, string method, string payload)
        {
            Name = name;
            URLFormatString = url;
            Method = method;
            PayloadFormatString = payload;
        }

        public override string ToString()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Webhook: Name: '");
                sb.Append(Name);
                sb.Append("', URL: '");
                sb.Append(URLFormatString.ToString());
                sb.Append(" (");
                sb.Append(Method);
                sb.Append(") with payload '");
                sb.Append(PayloadFormatString);
                sb.Append("'.");

                return sb.ToString();

            }
            catch (Exception ex)
            {
                Util.LogError(ex);

                return "Failed to print Webhook";
            }
        }

        public string ToJSON()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public void Save()
        {
            try
            {
                Util.EnsurePathExists(String.Format(@"{0}\{1}", Globals.PluginDirectory, "Webhooks"));
                string path = String.Format(@"{0}\{1}\{2}.json", Globals.PluginDirectory, "Webhooks", Name);

                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }
    }
}
