using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;

namespace EnergySmartBridge
{
    public static class Settings
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void LoadSettings()
        {
            NameValueCollection settings = LoadCollection(Global.config_file);

            // Web Server
            Global.webserver_port = ValidatePort(settings, "webserver_port");

            // MQTT
            Global.mqtt_server = settings["mqtt_server"];
            Global.mqtt_port = ValidatePort(settings, "mqtt_port");
            Global.mqtt_username = settings["mqtt_username"];
            Global.mqtt_password = settings["mqtt_password"];
            Global.mqtt_discovery_prefix = settings["mqtt_discovery_prefix"];
        }

        private static int ValidateInt(NameValueCollection settings, string section)
        {
            try
            {
                return Int32.Parse(settings[section]);
            }
            catch
            {
                log.Error("Invalid integer specified for " + section);
                throw;
            }
        }

        private static int ValidatePort(NameValueCollection settings, string section)
        {
            try
            {
                int port = Int32.Parse(settings[section]);

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

        private static bool ValidateBool(NameValueCollection settings, string section)
        {
            try
            {
                return Boolean.Parse(settings[section]);
            }
            catch
            {
                log.Error("Invalid bool specified for " + section);
                throw;
            }
        }

        private static IPAddress ValidateIP(NameValueCollection settings, string section)
        {
            if (settings[section] == "*")
                return IPAddress.Any;

            if (settings[section] == "")
                return IPAddress.None;

            try
            {
                return IPAddress.Parse(section);
            }
            catch
            {
                log.Error("Invalid IP specified for " + section);
                throw;
            }
        }

        private static string ValidateDirectory(NameValueCollection settings, string section)
        {
            try
            {
                if (!Directory.Exists(settings[section]))
                    Directory.CreateDirectory(settings[section]);

                return settings[section];
            }
            catch
            {
                log.Error("Invalid directory specified for " + section);
                throw;
            }
        }

        private static MailAddress ValidateMailFrom(NameValueCollection settings, string section)
        {
            try
            {
                return new MailAddress(settings[section]);
            }
            catch
            {
                log.Error("Invalid email specified for " + section);
                throw;
            }
        }

        private static MailAddress[] ValidateMailTo(NameValueCollection settings, string section)
        {
            try
            {
                if(settings[section] == null)
                    return new MailAddress[] {};

                string[] emails = settings[section].Split(',');
                MailAddress[] addresses = new MailAddress[emails.Length];

                for(int i=0; i < emails.Length; i++)
                    addresses[i] = new MailAddress(emails[i]);

                return addresses;
            }
            catch
            {
                log.Error("Invalid email specified for " + section);
                throw;
            }
        }

        private static string[] ValidateMultipleStrings(NameValueCollection settings, string section)
        {
            try
            {
                if (settings[section] == null)
                    return new string[] { };

                return settings[section].Split(',');
            }
            catch
            {
                log.Error("Invalid string specified for " + section);
                throw;
            }
        }

        private static bool ValidateYesNo (NameValueCollection settings, string section)
        {
            if (settings[section] == null)
                return false;
            if (string.Compare(settings[section], "yes", true) == 0)
                return true;
            else if (string.Compare(settings[section], "no", true) == 0)
                return false;
            else
            {
                log.Error("Invalid yes/no specified for " + section);
                throw new Exception();
            }
        }

        private static NameValueCollection LoadCollection(string sFile)
        {
            NameValueCollection settings = new NameValueCollection();

            try
            {
                FileStream fs = new FileStream(sFile, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);

                while (true)
                {
                    string line = sr.ReadLine();

                    if (line == null)
                        break;

                    if (line.StartsWith("#"))
                        continue;

                    int pos = line.IndexOf('=', 0);

                    if (pos == -1)
                        continue;

                    string key = line.Substring(0, pos).Trim();
                    string value = line.Substring(pos + 1).Trim();

                    settings.Add(key, value);
                }

                sr.Close();
                fs.Close();
            }
            catch (FileNotFoundException ex)
            {
                log.Error("Unable to parse settings file " + sFile, ex);
                throw;
            }

            return settings;
        }
    }
}
