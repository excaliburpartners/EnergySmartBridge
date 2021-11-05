using EnergySmartBridge.MQTT;
using EnergySmartBridge.WebService;
using log4net;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace EnergySmartBridge.Modules
{
    public class MQTTModule : IModule
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static DeviceRegistry MqttDeviceRegistry { get; set; }

        private WebServerModule WebServer { get; set; }
        private IManagedMqttClient MqttClient { get; set; }

        private readonly Regex regexTopic = new Regex(Global.mqtt_prefix + "/([A-F0-9]+)/(.*)", RegexOptions.Compiled);

        private readonly ConcurrentDictionary<string, Queue<WaterHeaterOutput>> connectedModules = new ConcurrentDictionary<string, Queue<WaterHeaterOutput>>();

        private readonly AutoResetEvent trigger = new AutoResetEvent(false);

        public MQTTModule(WebServerModule webServer)
        {
            WebServer = webServer;
            // Energy Smart module posts to this URL
            WebServer.RegisterPrefix(ProcessRequest, new string[] { "/~branecky/postAll.php" } );
        }

        public void Startup()
        {
            MqttApplicationMessage lastwill = new MqttApplicationMessage()
            {
                Topic = $"{Global.mqtt_prefix}/status",
                Payload = Encoding.UTF8.GetBytes("offline"),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
                Retain = true
            };

            MqttClientOptionsBuilder options = new MqttClientOptionsBuilder()
                .WithTcpServer(Global.mqtt_server)
                .WithWillMessage(lastwill);

            if (!string.IsNullOrEmpty(Global.mqtt_username))
                options = options
                    .WithCredentials(Global.mqtt_username, Global.mqtt_password);

            ManagedMqttClientOptions manoptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options.Build())
                .Build();

            MqttClient = new MqttFactory().CreateManagedMqttClient();
            MqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate((e) =>
            {
                log.Debug("Connected");

                MqttDeviceRegistry = new DeviceRegistry()
                {
                    identifiers = Global.mqtt_prefix,
                    name = Global.mqtt_prefix,
                    sw_version = $"EnergySmartBridge {Assembly.GetExecutingAssembly().GetName().Version}",
                    model = "Water Heater Controller",
                    manufacturer = "EnergySmart"
                };

                // Clear cache so we publish config on next check-in
                connectedModules.Clear();

                log.Debug("Publishing controller online");
                PublishAsync($"{Global.mqtt_prefix}/status", "online");
            });
            MqttClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate((e) => log.Debug("Error connecting " + e.Exception.Message));
            MqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate((e) => log.Debug("Disconnected"));

            MqttClient.StartAsync(manoptions);

            MqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(OnAppMessage);

            // Subscribe to notifications for these command topics
            List<Topic> toSubscribe = new List<Topic>()
            {
                Topic.updaterate_command,
                Topic.mode_command,
                Topic.setpoint_command
            };

            toSubscribe.ForEach((command) => MqttClient.SubscribeAsync(
                new MqttTopicFilterBuilder().WithTopic($"{Global.mqtt_prefix}/+/{command}").Build()));

            // Wait until shutdown
            trigger.WaitOne();

            log.Debug("Publishing controller offline");
            PublishAsync($"{Global.mqtt_prefix}/status", "offline");

            MqttClient.StopAsync();
        }

        protected virtual void OnAppMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            Match match = regexTopic.Match(e.ApplicationMessage.Topic);

            if (!match.Success)
                return;

            if (!Enum.TryParse(match.Groups[2].Value, true, out Topic topic))
                return;

            string id = match.Groups[1].Value;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            log.Debug($"Received: Id: {id}, Command: {topic}, Value: {payload}");

            if(connectedModules.ContainsKey(id))
            {
                if (topic == Topic.updaterate_command && 
                    int.TryParse(payload, out int updateRate) && updateRate >= 30 && updateRate <= 300)
                {
                    log.Debug($"Queued {id} UpdateRate: {updateRate}");
                    connectedModules[id].Enqueue(new WaterHeaterOutput()
                    {
                        UpdateRate = updateRate.ToString()
                    });
                }
                else if (topic == Topic.mode_command)
                {
                    log.Debug($"Queued {id} Mode: {payload}");
                    connectedModules[id].Enqueue(new WaterHeaterOutput()
                    {
                        Mode = payload
                    });
                }
                else if (topic == Topic.setpoint_command &&
                    double.TryParse(payload, out double setPoint) && setPoint >= 80 && setPoint <= 150)
                {
                    log.Debug($"Queued {id} SetPoint: {((int)setPoint)}");
                    connectedModules[id].Enqueue(new WaterHeaterOutput()
                    {
                        SetPoint = ((int)setPoint).ToString()
                    });
                }
            }
        }

        public void Shutdown()
        {
            trigger.Set();
        }

        private object ProcessRequest(HttpListenerRequest request)
        {
            string content = new System.IO.StreamReader(request.InputStream).ReadToEnd();

            log.Debug($"URL: {request.RawUrl}\n{content}");

            WaterHeaterInput waterHeater = HttpUtility.ParseQueryString(content).ToObject<WaterHeaterInput>();

            if(!connectedModules.ContainsKey(waterHeater.DeviceText))
            {
                log.Debug($"Publishing water heater config {waterHeater.DeviceText}");
                PublishWaterHeater(waterHeater);
                connectedModules.TryAdd(waterHeater.DeviceText, new Queue<WaterHeaterOutput>());
            }

            log.Debug($"Publishing water heater state {waterHeater.DeviceText}");
            PublishWaterHeaterState(waterHeater);

            if (connectedModules[waterHeater.DeviceText].Count > 0)
            {
                log.Debug($"Sent queued command {waterHeater.DeviceText}");
                return connectedModules[waterHeater.DeviceText].Dequeue();
            }
            else
            {
                return new WaterHeaterOutput() { };
            }
        }

        private void PublishWaterHeater(WaterHeaterInput waterHeater)
        {
            PublishAsync($"{Global.mqtt_discovery_prefix}/climate/{waterHeater.DeviceText}/config",
                JsonConvert.SerializeObject(waterHeater.ToThermostatConfig()));

            PublishAsync($"{Global.mqtt_discovery_prefix}/binary_sensor/{waterHeater.DeviceText}/heating/config",
                JsonConvert.SerializeObject(waterHeater.ToInHeatingConfig()));

            PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/hotwatervol/config",
                JsonConvert.SerializeObject(waterHeater.ToHotWaterVolConfig()));

            PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/uppertemp/config",
                JsonConvert.SerializeObject(waterHeater.ToUpperTempConfig()));

            PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/lowertemp/config",
                JsonConvert.SerializeObject(waterHeater.ToLowerTempConfig()));

            PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/dryfire/config",
                JsonConvert.SerializeObject(waterHeater.ToDryFireConfig()));

            PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/elementfail/config",
                JsonConvert.SerializeObject(waterHeater.ToElementFailConfig()));

            PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/tanksensorfail/config",
                JsonConvert.SerializeObject(waterHeater.ToTankSensorFailConfig()));
        }

        private void PublishWaterHeaterState(WaterHeaterInput waterHeater)
        {
            PublishAsync(waterHeater.ToTopic(Topic.maxsetpoint_state), waterHeater.MaxSetPoint.ToString());
            PublishAsync(waterHeater.ToTopic(Topic.setpoint_state), waterHeater.SetPoint.ToString());
            PublishAsync(waterHeater.ToTopic(Topic.mode_state), waterHeater.Mode);

            PublishAsync(waterHeater.ToTopic(Topic.systeminheating_state), waterHeater.SystemInHeating ? "ON" : "OFF");
            PublishAsync(waterHeater.ToTopic(Topic.hotwatervol_state), waterHeater.HotWaterVol);

            PublishAsync(waterHeater.ToTopic(Topic.uppertemp_state), waterHeater.UpperTemp.ToString());
            PublishAsync(waterHeater.ToTopic(Topic.lowertemp_state), waterHeater.LowerTemp.ToString());

            PublishAsync(waterHeater.ToTopic(Topic.updaterate_state), waterHeater.UpdateRate.ToString());

            PublishAsync(waterHeater.ToTopic(Topic.dryfire_state), waterHeater.DryFire);
            PublishAsync(waterHeater.ToTopic(Topic.elementfail_state), waterHeater.ElementFail);
            PublishAsync(waterHeater.ToTopic(Topic.tanksensorfail_state), waterHeater.TankSensorFail);

            PublishAsync(waterHeater.ToTopic(Topic.faultcodes_state), waterHeater.FaultCodes);

            PublishAsync(waterHeater.ToTopic(Topic.signalstrength_state), waterHeater.SignalStrength);
        }

        private Task PublishAsync(string topic, string payload)
        {
            return MqttClient.PublishAsync(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, true);
        }
    }
}
