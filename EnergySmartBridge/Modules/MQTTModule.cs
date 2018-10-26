using log4net;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using System.Text.RegularExpressions;
using System.Text;
using EnergySmartBridge.MQTT;
using System.Collections.Specialized;
using EnergySmartBridge.WebService;
using System.Net;
using System.Web;
using System.Collections.Concurrent;

namespace EnergySmartBridge.Modules
{
    public class MQTTModule : IModule
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebServerModule WebServer { get; set; }
        private IManagedMqttClient MqttClient { get; set; }

        private Regex regexTopic = new Regex("energysmart/([A-F0-9]+)/(.*)", RegexOptions.Compiled);

        private ConcurrentDictionary<string, Queue<WaterHeaterOutput>> connectedModules = new ConcurrentDictionary<string, Queue<WaterHeaterOutput>>();

        private readonly AutoResetEvent trigger = new AutoResetEvent(false);

        public MQTTModule(WebServerModule webServer)
        {
            WebServer = webServer;
            // Energy Smart module posts to this URL
            WebServer.RegisterPrefix(ProcessRequest, new string[] { "/~branecky/postAll.php" } );
        }

        public void Startup()
        {
            MqttClientOptionsBuilder options = new MqttClientOptionsBuilder()
                .WithTcpServer(Global.mqtt_server);

            if (!string.IsNullOrEmpty(Global.mqtt_username))
                options = options
                    .WithCredentials(Global.mqtt_username, Global.mqtt_password);

            ManagedMqttClientOptions manoptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options.Build())
                .Build();

            MqttClient = new MqttFactory().CreateManagedMqttClient();
            MqttClient.Connected += (sender, e) => 
            {
                log.Debug("Connected");

                // Clear cache so we publish config on next check-in
                connectedModules.Clear();

                MqttClient.PublishAsync("energysmart/status", "online", MqttQualityOfServiceLevel.AtMostOnce, true);
            };
            MqttClient.ConnectingFailed += (sender, e) => { log.Debug("Error connecting" + e.Exception.Message); };

            MqttClient.StartAsync(manoptions);

            MqttClient.ApplicationMessageReceived += MqttClient_ApplicationMessageReceived;

            MqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("energysmart/+/" + Topic.updaterate_command).Build());
            MqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("energysmart/+/" + Topic.mode_command).Build());
            MqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("energysmart/+/" + Topic.setpoint_command).Build());

            // Wait until shutdown
            trigger.WaitOne();

            MqttClient.PublishAsync("energysmart/status", "offline", MqttQualityOfServiceLevel.AtMostOnce, true);
        }

        private void MqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            Match match = regexTopic.Match(e.ApplicationMessage.Topic);

            if (!match.Success)
                return;

            string id = match.Groups[1].Value;
            string command = match.Groups[2].Value;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            log.Debug($"Received: Id: {id}, Command: {command}, Value: {payload}");

            if(connectedModules.ContainsKey(id))
            {
                if (string.Compare(command, Topic.updaterate_command.ToString()) == 0 && 
                    int.TryParse(payload, out int updateRate) && updateRate >= 30 && updateRate <= 300)
                {
                    log.Debug($"Queued {id} UpdateRate: {updateRate.ToString()}");
                    connectedModules[id].Enqueue(new WaterHeaterOutput()
                    {
                        UpdateRate = updateRate.ToString()
                    });
                }
                else if (string.Compare(command, Topic.mode_command.ToString()) == 0)
                {
                    log.Debug($"Queued {id} Mode: {payload}");
                    connectedModules[id].Enqueue(new WaterHeaterOutput()
                    {
                        Mode = payload
                    });
                }
                else if (string.Compare(command, Topic.setpoint_command.ToString()) == 0 &&
                    double.TryParse(payload, out double setPoint) && setPoint >= 80 && setPoint <= 150)
                {
                    log.Debug($"Queued {id} SetPoint: {((int)setPoint).ToString()}");
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
            MqttClient.PublishAsync($"{Global.mqtt_discovery_prefix}/climate/{waterHeater.DeviceText}/config",
                JsonConvert.SerializeObject(waterHeater.ToThermostatConfig()), MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync($"{Global.mqtt_discovery_prefix}/binary_sensor/{waterHeater.DeviceText}/heating/config",
                JsonConvert.SerializeObject(waterHeater.ToInHeatingConfig()), MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/hotwatervol/config",
                JsonConvert.SerializeObject(waterHeater.ToHotWaterVolConfig()), MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/uppertemp/config",
                JsonConvert.SerializeObject(waterHeater.ToUpperTempConfig()), MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/lowertemp/config",
                JsonConvert.SerializeObject(waterHeater.ToLowerTempConfig()), MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/dryfire/config",
                JsonConvert.SerializeObject(waterHeater.ToDryFireConfig()), MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/elementfail/config",
                JsonConvert.SerializeObject(waterHeater.ToElementFailConfig()), MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync($"{Global.mqtt_discovery_prefix}/sensor/{waterHeater.DeviceText}/tanksensorfail/config",
                JsonConvert.SerializeObject(waterHeater.ToTankSensorFailConfig()), MqttQualityOfServiceLevel.AtMostOnce, true);
        }

        private void PublishWaterHeaterState(WaterHeaterInput waterHeater)
        {
            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.maxsetpoint_state), waterHeater.MaxSetPoint.ToString(), MqttQualityOfServiceLevel.AtMostOnce, true);
            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.setpoint_state), waterHeater.SetPoint.ToString(), MqttQualityOfServiceLevel.AtMostOnce, true);
            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.mode_state), waterHeater.Mode, MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.systeminheating_state), waterHeater.SystemInHeating ? "ON" : "OFF", MqttQualityOfServiceLevel.AtMostOnce, true);
            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.hotwatervol_state), waterHeater.HotWaterVol, MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.uppertemp_state), waterHeater.UpperTemp.ToString(), MqttQualityOfServiceLevel.AtMostOnce, true);
            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.lowertemp_state), waterHeater.LowerTemp.ToString(), MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.updaterate_state), waterHeater.UpdateRate.ToString(), MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.dryfire_state), waterHeater.DryFire, MqttQualityOfServiceLevel.AtMostOnce, true);
            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.elementfail_state), waterHeater.ElementFail, MqttQualityOfServiceLevel.AtMostOnce, true);
            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.tanksensorfail_state), waterHeater.TankSensorFail, MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.faultcodes_state), waterHeater.FaultCodes, MqttQualityOfServiceLevel.AtMostOnce, true);

            MqttClient.PublishAsync(waterHeater.ToTopic(Topic.signalstrength_state), waterHeater.SignalStrength, MqttQualityOfServiceLevel.AtMostOnce, true);
        }
    }
}
