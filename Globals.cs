using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace TownCrier
{
	public static class Globals
	{
        public static string PluginName { get; private set; }
        public static PluginHost Host { get; private set; }
        public static CoreManager Core { get; private set; }

        public static void Init(string pluginName, PluginHost host, CoreManager core)
        {
            PluginName = pluginName;
            Host = host;
            Core = core;
        }

        public static void TriggerWebhook(string name, string message)
        {
        }
	}
}
