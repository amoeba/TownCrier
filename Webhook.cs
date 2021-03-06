﻿using System;
using System.IO;
using System.Text;

namespace TownCrier
{
    public class Webhook
    {
        public string Name { get; set; }
        public string Method { get; set; }
        public string URLFormatString { get; set; }
        public string PayloadFormatString { get; set; }

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
                sb.Append(URLFormatString);
                sb.Append(" (");
                sb.Append(Method);
                sb.Append(") with payload '");
                sb.Append(PayloadFormatString);
                sb.Append("'.");

                return sb.ToString();

            }
            catch (Exception ex)
            {
                Utilities.LogError(ex);

                return "Failed to print Webhook";
            }
        }

        public string ToJSON()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
