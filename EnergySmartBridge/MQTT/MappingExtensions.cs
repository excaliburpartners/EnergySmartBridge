using EnergySmartBridge.WebService;
using System.Collections.Generic;
using System.Linq;

namespace EnergySmartBridge.MQTT
{
    public static class MappingExtensions
    {
        public static string ToTopic(this WaterHeaterInput waterHeater, Topic topic)
        {
            return $"{Global.mqtt_prefix}/{waterHeater.DeviceText}/{topic}";
        }
        
        public static string GetDisplayName(this WaterHeaterInput waterHeater)
        {
            return waterHeater.DeviceText.Substring(waterHeater.DeviceText.Length - 4) + " Water Heater";
        }

        public static string GetID(this WaterHeaterInput waterHeater)
        {
            return waterHeater.DeviceText.Substring(waterHeater.DeviceText.Length - 4).ToLower() + "_water_heater";
        }

        public static Climate ToThermostatConfig(this WaterHeaterInput waterHeater)
        {
            Climate ret = new Climate
            {
                name = waterHeater.GetDisplayName(),
                unique_id = waterHeater.GetID() + "_control",

                action_template = "{% if value == 'ON' %} heating {%- else -%} off {%- endif %}",
                action_topic = waterHeater.ToTopic(Topic.systeminheating_state),
                current_temperature_topic = waterHeater.ToTopic(Topic.uppertemp_state),

                temperature_state_topic = waterHeater.ToTopic(Topic.setpoint_state),
                temperature_command_topic = waterHeater.ToTopic(Topic.setpoint_command),

                max_temp = waterHeater.MaxSetPoint.ToString(),

                mode_state_template = "heat",
                mode_state_topic = waterHeater.ToTopic(Topic.mode_state),
                modes = new List<string>(new string[] { "heat" }),

                preset_mode_state_topic = waterHeater.ToTopic(Topic.mode_state),
                preset_mode_command_topic = waterHeater.ToTopic(Topic.mode_command),
                preset_modes = waterHeater.AvailableModes.Split(',').ToList()
            };
            return ret;
        }

        public static BinarySensor ToInHeatingConfig(this WaterHeaterInput waterHeater)
        {
            BinarySensor ret = new BinarySensor
            {
                name = waterHeater.GetDisplayName() + " Element",
                unique_id = waterHeater.GetID() + "_element",
                state_topic = waterHeater.ToTopic(Topic.systeminheating_state)
            };
            return ret;
        }

        public static Sensor ToHotWaterVolConfig(this WaterHeaterInput waterHeater)
        {
            Sensor ret = new Sensor
            {
                name = waterHeater.GetDisplayName() + " Volume",
                unique_id = waterHeater.GetID() + "_volume",
                state_topic = waterHeater.ToTopic(Topic.hotwatervol_state)
            };
            return ret;
        }

        public static Sensor ToUpperTempConfig(this WaterHeaterInput waterHeater)
        {
            Sensor ret = new Sensor
            {
                name = waterHeater.GetDisplayName() + " Upper",
                unique_id = waterHeater.GetID() + "_upper",
                device_class = Sensor.DeviceClass.temperature,
                state_topic = waterHeater.ToTopic(Topic.uppertemp_state),
                unit_of_measurement = "°" + waterHeater.Units
            };
            return ret;
        }

        public static Sensor ToLowerTempConfig(this WaterHeaterInput waterHeater)
        {
            Sensor ret = new Sensor
            {
                name = waterHeater.GetDisplayName() + " Lower",
                unique_id = waterHeater.GetID() + "_lower",
                device_class = Sensor.DeviceClass.temperature,
                state_topic = waterHeater.ToTopic(Topic.lowertemp_state),
                unit_of_measurement = "°" + waterHeater.Units
            };
            return ret;
        }

        public static BinarySensor ToDryFireConfig(this WaterHeaterInput waterHeater)
        {
            BinarySensor ret = new BinarySensor
            {
                name = waterHeater.GetDisplayName() + " Dry Fire",
                unique_id = waterHeater.GetID() + "_dry_fire",
                state_topic = waterHeater.ToTopic(Topic.dryfire_state)
            };
            return ret;
        }

        public static BinarySensor ToElementFailConfig(this WaterHeaterInput waterHeater)
        {
            BinarySensor ret = new BinarySensor
            {
                name = waterHeater.GetDisplayName() + " Element Fail",
                unique_id = waterHeater.GetID() + "_element_fail",
                state_topic = waterHeater.ToTopic(Topic.elementfail_state)
            };
            return ret;
        }

        public static BinarySensor ToTankSensorFailConfig(this WaterHeaterInput waterHeater)
        {
            BinarySensor ret = new BinarySensor
            {
                name = waterHeater.GetDisplayName() + " Tank Sensor Fail",
                unique_id = waterHeater.GetID() + "_tank_sensor_fail", 
                state_topic = waterHeater.ToTopic(Topic.tanksensorfail_state)
            };
            return ret;
        }
    }
}
