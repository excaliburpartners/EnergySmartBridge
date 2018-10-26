using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Mail;

namespace EnergySmartBridge
{
    public abstract class Global
    {
        public static bool running;

        // Config File
        public static string config_file;

        // Web Server
        public static int webserver_port;

        // MQTT
        public static string mqtt_server;
        public static int mqtt_port;
        public static string mqtt_username;
        public static string mqtt_password;
        public static string mqtt_discovery_prefix;
    }
}
