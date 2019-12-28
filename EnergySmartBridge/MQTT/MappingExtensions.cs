using EnergySmartBridge.WebService;
using System.Linq;

namespace EnergySmartBridge.MQTT
{
    public static class MappingExtensions
    {
        public static string ToTopic(this WaterHeaterInput waterHeater, Topic topic)
        {
            return $"{Global.mqtt_prefix}/{waterHeater.DeviceText}/{topic.ToString()}";
        }
        
        public static string GetDisplayName(this WaterHeaterInput waterHeater)
        {
            return waterHeater.DeviceText.Substring(waterHeater.DeviceText.Length - 4) + " Water Heater";
        }

        public static Climate ToThermostatConfig(this WaterHeaterInput waterHeater)
        {
            Climate ret = new Climate();
            ret.name = waterHeater.GetDisplayName();

            ret.current_temperature_topic = waterHeater.ToTopic(Topic.uppertemp_state);

            ret.temperature_state_topic = waterHeater.ToTopic(Topic.setpoint_state);
            ret.temperature_command_topic = waterHeater.ToTopic(Topic.setpoint_command);

            ret.max_temp = waterHeater.MaxSetPoint.ToString();

            ret.mode_state_topic = waterHeater.ToTopic(Topic.mode_state);
            ret.mode_command_topic = waterHeater.ToTopic(Topic.mode_command);
            ret.modes = waterHeater.AvailableModes.Split(',').ToList();
            return ret;
        }

        public static BinarySensor ToInHeatingConfig(this WaterHeaterInput waterHeater)
        {
            BinarySensor ret = new BinarySensor();
            ret.name = waterHeater.GetDisplayName() + " Element";
            ret.state_topic = waterHeater.ToTopic(Topic.systeminheating_state);
            return ret;
        }

        public static Sensor ToHotWaterVolConfig(this WaterHeaterInput waterHeater)
        {
            Sensor ret = new Sensor();
            ret.name = waterHeater.GetDisplayName() + " Volume";
            ret.state_topic = waterHeater.ToTopic(Topic.hotwatervol_state);
            return ret;
        }

        public static Sensor ToUpperTempConfig(this WaterHeaterInput waterHeater)
        {
            Sensor ret = new Sensor();
            ret.name = waterHeater.GetDisplayName() + " Upper";
            ret.device_class = Sensor.DeviceClass.temperature;
            ret.state_topic = waterHeater.ToTopic(Topic.uppertemp_state);
            ret.unit_of_measurement = "°" + waterHeater.Units;
            return ret;
        }

        public static Sensor ToLowerTempConfig(this WaterHeaterInput waterHeater)
        {
            Sensor ret = new Sensor();
            ret.name = waterHeater.GetDisplayName() + " Lower";
            ret.device_class = Sensor.DeviceClass.temperature;
            ret.state_topic = waterHeater.ToTopic(Topic.lowertemp_state);
            ret.unit_of_measurement = "°" + waterHeater.Units;
            return ret;
        }

        public static BinarySensor ToDryFireConfig(this WaterHeaterInput waterHeater)
        {
            BinarySensor ret = new BinarySensor();
            ret.name = waterHeater.GetDisplayName() + " Dry Fire";
            ret.state_topic = waterHeater.ToTopic(Topic.dryfire_state);
            return ret;
        }

        public static BinarySensor ToElementFailConfig(this WaterHeaterInput waterHeater)
        {
            BinarySensor ret = new BinarySensor();
            ret.name = waterHeater.GetDisplayName() + " Element Fail";
            ret.state_topic = waterHeater.ToTopic(Topic.elementfail_state);
            return ret;
        }

        public static BinarySensor ToTankSensorFailConfig(this WaterHeaterInput waterHeater)
        {
            BinarySensor ret = new BinarySensor();
            ret.name = waterHeater.GetDisplayName() + " Tank Sensor Fail";
            ret.state_topic = waterHeater.ToTopic(Topic.tanksensorfail_state);
            return ret;
        }
    }
}
