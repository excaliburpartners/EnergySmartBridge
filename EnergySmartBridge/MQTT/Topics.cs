using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergySmartBridge.MQTT
{
    public class Topic
    {
        public string Value { get; private set; }

        private Topic(string value)
        {
            Value = value; 
        }

        public override string ToString()
        {
            return Value;
        }

        public static Topic updaterate_state { get { return new Topic("updaterate_state"); } }
        public static Topic updaterate_command { get { return new Topic("updaterate_command"); } }

        public static Topic mode_state { get { return new Topic("mode_state"); } }
        public static Topic mode_command { get { return new Topic("mode_command"); } }

        public static Topic maxsetpoint_state { get { return new Topic("maxsetpoint_state"); } }
        public static Topic setpoint_state { get { return new Topic("setpoint_state"); } }
        public static Topic setpoint_command { get { return new Topic("setpoint_command"); } }

        public static Topic systeminheating_state { get { return new Topic("systeminheating_state"); } }
        public static Topic hotwatervol_state { get { return new Topic("hotwatervol_state"); } }

        public static Topic uppertemp_state { get { return new Topic("uppertemp_state"); } }
        public static Topic lowertemp_state { get { return new Topic("lowertemp_state"); } }

        public static Topic dryfire_state { get { return new Topic("dryfire_state"); } }
        public static Topic elementfail_state { get { return new Topic("elementfail_state"); } }
        public static Topic tanksensorfail_state { get { return new Topic("tanksensorfail_state"); } }

        public static Topic faultcodes_state { get { return new Topic("faultcodes_state"); } }

        public static Topic signalstrength_state { get { return new Topic("signalstrength_state"); } }
    }
}
