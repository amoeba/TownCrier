using System;
using System.IO;

namespace TownCrier
{
    public static class Settings
    {
        public static bool Verbose { get; set; }

        public static void Init(bool verbose)
        {
            Verbose = verbose;
        }

        internal static void Write(StreamWriter writer)
        {
            try
            {
                writer.WriteLine("setting\t" + "verbose\t" + Verbose.ToString());
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
    }
}
