using log4net;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;

namespace EnergySmartBridge
{
    public static class Settings
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static bool ShowDebug { get; set; }
        public static bool UseEnvironment { get; set; }

        public static void LoadSettings(string file)
        {
            LoadSettings(LoadCollection(file));
        }

        public static void LoadSettings(string[] lines)
        {
            LoadSettings(LoadCollection(lines));
        }

        public static void LoadSettings(NameValueCollection settings)
        { 
            // Web Server
            Global.webserver_port = settings.ValidatePort("webserver_port");

            // MQTT
            Global.mqtt_server = settings.CheckEnv("mqtt_server");
            Global.mqtt_port = settings.ValidatePort("mqtt_port");
            Global.mqtt_username = settings.CheckEnv("mqtt_username");
            Global.mqtt_password = settings.CheckEnv("mqtt_password");
            Global.mqtt_prefix = settings.CheckEnv("mqtt_prefix") ?? "energysmart";
            Global.mqtt_discovery_prefix = settings.CheckEnv("mqtt_discovery_prefix");
        }

        private static string CheckEnv(this NameValueCollection settings, string name)
        {
            string env = UseEnvironment ? Environment.GetEnvironmentVariable(name.ToUpper()) : null;
            string value = !string.IsNullOrEmpty(env) ? env : settings[name];

            if (ShowDebug)
                log.Debug((!string.IsNullOrEmpty(env) ? "ENV" : "CONF").PadRight(5) + $"{name}: {value}");

            return value;
        }

        private static int ValidatePort(this NameValueCollection settings, string section)
        {
            try
            {
                int port = int.Parse(settings.CheckEnv(section));

                if (port < 1 || port > 65534)
                    throw new Exception();

                return port;
            }
            catch
            {
                log.Error("Invalid port specified for " + section);
                throw;
            }
        }

        private static NameValueCollection LoadCollection(string[] lines)
        {
            NameValueCollection settings = new NameValueCollection();

            foreach (string line in lines)
            {
                if (line.StartsWith("#"))
                    continue;

                int pos = line.IndexOf('=', 0);

                if (pos == -1)
                    continue;

                string key = line.Substring(0, pos).Trim();
                string value = line.Substring(pos + 1).Trim();

                settings.Add(key, value);
            }

            return settings;
        }

        private static NameValueCollection LoadCollection(string sFile)
        {
            if (ShowDebug)
                log.Debug($"Using settings file {sFile}");

            if (!File.Exists(sFile))
            {
                log.Warn($"Unable to locate settings file {sFile}");
                return new NameValueCollection();
            }

            try
            {
                return LoadCollection(File.ReadAllLines(sFile));
            }
            catch (FileNotFoundException ex)
            {
                log.Error("Error parsing settings file " + sFile, ex);
                throw;
            }
        }
    }
}
