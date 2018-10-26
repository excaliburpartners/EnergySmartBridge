using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergySmartBridge.MQTT
{
    public class BinarySensor : Device
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum DeviceClass
        {
            heat,
            problem,
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DeviceClass? device_class { get; set; }
    }
}
