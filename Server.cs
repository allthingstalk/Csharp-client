using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace att.iot.client
{
    /// <summary>
    /// a default implementation for the server connection.
    /// </summary>
    /// <remarks>
    /// In case that there are too many threads running due to async tasks, use a custom TaskScheduler to limit the nr of threads.
    /// For an example, see: http://msdn.microsoft.com/en-us/library/system.threading.tasks.taskscheduler%28v=vs.110%29.aspx
    /// </remarks>
    public class Server: IServer, IDisposable
    {
        #region fields
        MqttClient _mqtt;
        string _mqttUserName;
        string _mqttpwd;
        HttpClient _http;
        bool _httpError = false;                                                                                     //so we don't continuously write the same error.
        static Logger _logger = LogManager.GetCurrentClassLogger();
        static DateTime _origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);                                             //for calulatinng unix times.
        #endregion


        #region ctor/~
        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        public Server()
        {

        }
        /// <summary>
        /// Finalizes an instance of the <see cref="Server"/> class.
        /// </summary>
        ~Server()
        {
            Free();
        } 
        #endregion

        #region events
        /// <summary>
        /// Occurs when a new actuator value arrived.
        /// </summary>
        public event EventHandler<ActuatorData> ActuatorValue;

        /// <summary>
        /// Occurs when a management command arrived.
        /// </summary>
        public event EventHandler<ManagementCommandData> ManagementCommand;


        /// <summary>
        /// Occurs when the mqtt connection was reset and recreated. This allows the application to recreate the subscriptions.
        /// You can use <see cref="Server.RegisterGateways"/> for this.
        /// </summary>
        public event EventHandler ConnectionReset;
        #endregion

        /// <summary>
        /// Initializes the server acoording to the specified application settings.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        public void Init(NameValueCollection appSettings)
        {
            InitMqtt(appSettings);
            InitHttp(appSettings);
        }

        #region mqtt
        private void InitMqtt(NameValueCollection appSettings)
        {
            _mqtt = new MqttClient(GetSetting(appSettings, "broker address", "broker.smartliving.io"));
            _mqtt.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            _mqtt.MqttMsgDisconnected += client_MqttMsgDisconnected;
            string clientId = Guid.NewGuid().ToString().Substring(0, 22);                   //need to respect the max id of mqtt.
            _mqttUserName = GetSetting(appSettings, "broker userName", "");
            _mqttpwd = GetSetting(appSettings, "broker pwd", "");
            _mqtt.Connect(clientId, _mqttUserName, _mqttpwd, true, 30); 
        }

        /// <summary>
        /// Gets a single setting and returns the default values if no value was specified in the config.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultVal">The default value.</param>
        /// <returns></returns>
        private static string GetSetting(NameValueCollection appSettings, string key, string defaultVal)
        {
            string found = appSettings.Get(key);
            if (string.IsNullOrEmpty(found) == true)
                found = defaultVal;
            _logger.Info("CAPP Messaging {0}: '{1}'", key, found);
            return found;
        }

        /// <summary>
        /// Handles the MqttMsgDisconnected event of the client control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void client_MqttMsgDisconnected(object sender, EventArgs e)
        {
            _logger.Error("mqtt connection lost, recreating...");
            string clientId = Guid.NewGuid().ToString();
            while (_mqtt.IsConnected == false)
                _mqtt.Connect(clientId, _mqttUserName, _mqttpwd);
            _logger.Trace("mqtt connection recreated, resubscribing...");
            OnConnectionReset();
        }

        /// <summary>
        /// Called when the mqtt connection was recreated. Allows observers to re-register 1 or more gateways.
        /// </summary>
        private void OnConnectionReset()
        {
            if (ConnectionReset != null)
                ConnectionReset(this, EventArgs.Empty);
        }

        /// <summary>
        /// walks over all the ziprs and subscribes to the topics.
        /// </summary>
        public void RegisterGateways(IEnumerable<GatewayCredentials> toSubscribe)
        {
            lock (_mqtt)
            {
                foreach (var i in toSubscribe)
                    SubscribeUnsafe(i);
            }
        }

        /// <summary>
        /// Handles the MqttMsgPublishReceived event of the client control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MqttMsgPublishEventArgs"/> instance containing the event data.</param>
        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                string[] parts = e.Topic.Split(new char[] { '/' });
                TopicPath path = new TopicPath(parts);

                if (path.IsSetter == false && path.AssetId != null)                      //it can only be an actuator value is there is an asset path.
                    OnActuatorValue(path, e);
                else
                    OnManagementCommand(path, e);
            }
            catch (Exception ex)
            {
                _logger.Error("problem with incomming mqtt message", ex.ToString());
            }
        }

        /// <summary>
        /// Called when  a management command message arrived.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="e">The <see cref="MqttMsgPublishEventArgs"/> instance containing the event data.</param>
        private void OnManagementCommand(TopicPath path, MqttMsgPublishEventArgs e)
        {
            if (ManagementCommand != null)
            {
                ManagementCommandData data = new ManagementCommandData();
                data.Path = path;
                data.Command = System.Text.Encoding.UTF8.GetString(e.Message);
                ManagementCommand(this, data);
            }

        }

        /// <summary>
        /// Called when a value arrived that has to be sent to an actuator
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="e">The <see cref="MqttMsgPublishEventArgs"/> instance containing the event data.</param>
        private void OnActuatorValue(TopicPath path, MqttMsgPublishEventArgs e)
        {
            if (ActuatorValue != null)
            {
                ActuatorData data;
                string val = System.Text.Encoding.UTF8.GetString(e.Message);
                if (val[0] == '{')
                    data = new JsonActuatorData();
                else
                    data = new CsvActuatorData();
                data.Load(val);
                data.Path = path;
                ActuatorValue(this, data);                                                                          //even if there is no data, the event has to be raised.
            }
        }

        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void AssetValue(TopicPath asset, object value)
        {
            string toSend;
            if (value is string)
            {
                TimeSpan diff = DateTime.Now.ToUniversalTime() - _origin;
                StringBuilder content = new StringBuilder(Math.Floor(diff.TotalSeconds).ToString());
                content.Append('|');
                content.Append((string)value);
                toSend = content.ToString();
            }
            else if (value is JObject)
            {
                ((JObject)value).Add("At", DateTime.UtcNow);
                toSend = value.ToString();
            }
            else
                throw new NotSupportedException("value is of a none supported type");
            
            string topic = asset.ToString();
            lock (_mqtt)                                                               //make certain that the messages sent by different threads at the same time, don't intermingle.
            {
                _mqtt.Publish(topic, System.Text.Encoding.UTF8.GetBytes(toSend));
                _logger.Trace("message published, topic: {0}, content: {1}", topic, toSend);
            }
        }

        /// <summary>
        /// makes certain that the specified gateway is monitored so that we receive incomming mqtt messages for the specified gateway.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        public void SubscribeToTopics(GatewayCredentials credentials)
        {
            if (_mqtt != null)
            {
                lock (_mqtt)                                                                //only 1 thread can access the mqtt connection at a time.
                    SubscribeUnsafe(credentials);
            }
        }

        private void SubscribeUnsafe(GatewayCredentials credentials)
        {
            string[] toSubscribe = GetTopics(credentials);
            byte[] qos = new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };
            _mqtt.Subscribe(toSubscribe, qos);
            _logger.Trace("subscribed to: {0}", string.Join(", ", toSubscribe));
        }

        private string[] GetTopics(GatewayCredentials credentials)
        {
            string[] topics = new string[5];
            string root = string.Format("client/{0}/in/gateway/{1}", credentials.ClientId, credentials.GatewayId);
            topics[0] = root + "/management";
            topics[1] = root + "/asset/+/command";
            topics[2] = root + "/device/+/management";
            topics[3] = root + "/device/+/asset/+/command";
            topics[4] = root + "/device/+/asset/+/management";
            return topics;
        }

        void _mqtt_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            _logger.Trace("subscribed");
        }
        /// <summary>
        /// removes the monitors for the specified gateway, so we no longer receive mqtt messages for it.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public void UnRegisterGateway(GatewayCredentials credentials)
        {
            if (_mqtt != null && string.IsNullOrEmpty(credentials.GatewayId) == false)      //if there is no gatewayId yet (still registering), then there is no need to remove any topics: we haven't yet been able to subscribe for them.
            {
                lock (_mqtt)                                                                //only 1 thread can access the mqtt connection at a time.
                {
                    string[] toRemove = GetTopics(credentials);
                    _mqtt.Unsubscribe(toRemove);
                    _logger.Trace("unsubscribed from: {0}", toRemove);
                }
            }
        }

        #endregion

        #region http
        /// <summary>
        /// Initializes the HTTP communication.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        private void InitHttp(NameValueCollection appSettings)
        {
            _http = new HttpClient();
            _http.BaseAddress = new Uri(appSettings["cloud address"]);
        }

        /// <summary>
        /// Reports an error back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="gatewayId">The gateway identifier.</param>
        /// <param name="message">The message.</param>
        public void ReportError(GatewayCredentials credentials, string message)
        {
            Report(credentials, message, "gateway error");
        }

        /// <summary>
        /// Reports a warning back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="gatewayId">The gateway identifier.</param>
        /// <param name="message">The message.</param>
        public void ReportWarning(GatewayCredentials credentials, string message)
        {
            Report(credentials, message, "gateway warning");
        }

        /// <summary>
        /// Reports an  info message back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="gatewayId">The gateway identifier.</param>
        /// <param name="message">The message.</param>
        public void ReportInfo(GatewayCredentials credentials, string message)
        {
            Report(credentials, message, "gateway info");
        }

        void Report(GatewayCredentials credentials, string message, string title)
        {
            JObject data = new JObject();
            data["Content"] = message;
            data["Title"] = title;
            data["Link"] = "app/gateway/" + credentials.GatewayId;
            data["Icon"] = "gateway";
            try
            {
                string content = data.ToString();
                _logger.Trace("{0}: {1}", title, content);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "api/UserNotification");
                request.Headers.Add("Auth-GatewayKey", credentials.ClientKey);
                request.Headers.Add("Auth-GatewayId", credentials.GatewayId);
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                    result.EnsureSuccessStatusCode();
                _httpError = false;
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
            }
        }



        /// <summary>
        /// Creates a new gateway.
        /// </summary>
        /// <param name="id">a token that uniquely identifies the gateway.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="name">The name.</param>
        /// <returns>
        /// The Json object that represents the gateway. This can be used further to finish the claim process
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool CreateGateway(string id, byte[] ipAddress, JObject data)
        {
            try
            {
                string content = data.ToString();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "Gateway");
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                {
                    if (result.StatusCode == HttpStatusCode.Unauthorized)                                           //not a success code, but very common, special case, don't treat as comm problem.
                    {
                        _logger.Error("Unauthorised access: {0}, IP address: {1}", id, BitConverter.ToString(ipAddress));
                        return false;
                    }
                    result.EnsureSuccessStatusCode();
                }
                _httpError = false;
                _logger.Trace("gateway created: {0}", content);
                return true;
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
                return false;
            }
        }

        /// <summary>
        /// Gets the gateway with the specified id. Note: this does not use the UID, but the ATT generated id.
        /// </summary>
        /// <param name="id">The credentials for the gateway.</param>
        /// <returns>a jobject containing the gateway definition</returns>
        public JObject GetGateway(GatewayCredentials credentials)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "Gateway/" + credentials.GatewayId);
                request.Headers.Add("Auth-GatewayKey", credentials.ClientKey);
                request.Headers.Add("Auth-GatewayId", credentials.GatewayId);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                using (var result = task.Result)
                {
                    if (result.StatusCode == HttpStatusCode.Unauthorized)                                           //not a success code, but very common, special case, don't treat as comm problem.
                    {
                        _logger.Error("Unauthorised request for gateway details: {0}, zipr: {1}", credentials.GatewayId, credentials.UId);
                        return null;
                    }
                    result.EnsureSuccessStatusCode();
                    using (HttpContent content = result.Content)
                    {
                        var contentTask = content.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;

                        if (resultContent != null)
                        {
                            JObject obj = JObject.Parse(resultContent);
                            _httpError = false;
                            return obj;
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
                return null;
            }
        }

        /// <summary>
        /// Gets the gateway credentials from the server. This only works if the <see cref="GatewayCredentials.UId" /> is already filled in.
        /// This function will fill in the other fields.
        /// Logs an error if the object was not found.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="definition">The definition of the gateway that needs to be udpated.</param>
        /// <returns>
        /// True if the operation was successful.
        /// </returns>
        public bool FinishClaim(GatewayCredentials credentials, JObject definition)
        {
            try
            {
                //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "api/GatewayEnroll?id=" + credentials.ZiprId);
                //var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "Gateway");
                request.Content = new StringContent(definition.ToString(), Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                using (var result = task.Result)
                {
                    result.EnsureSuccessStatusCode();
                    using (HttpContent content = result.Content)
                    {
                        var contentTask = content.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;

                        if (resultContent != null && resultContent.Length >= 50)
                        {
                            JObject obj = JObject.Parse(resultContent);
                            credentials.GatewayId = obj["id"].Value<string>();
                            credentials.ClientKey = obj["key"].Value<string>();
                            var client = obj["client"];
                            if(client != null)
                                credentials.ClientId = client["clientId"].Value<string>();
                            _logger.Trace("credentials found. zipr Id: {0}, gateway id: {1}, key: {2}", credentials.UId, credentials.GatewayId, credentials.ClientKey);
                            return true;
                        }
                    }
                }
                _httpError = false;
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
            }
            return false;                                                                                       //something went wrong if we get here.
        }

        /// <summary>
        /// Authenticates the zipr with the specified credentials.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <returns></returns>
        public bool Authenticate(GatewayCredentials credentials)
        {
            try
            {
                
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/authentication");
                request.Headers.Add("Auth-GatewayKey", credentials.ClientKey);
                request.Headers.Add("Auth-GatewayId", credentials.GatewayId);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                using (var result = task.Result)
                {
                    if (result.IsSuccessStatusCode)
                    {
                        _logger.Trace("gateway authenticated. zipr Id: {0}, gateway id: {1}, key: {2}", credentials.UId, credentials.GatewayId, credentials.ClientKey);
                        return true;
                    }
                    else
                    {
                        _logger.Error("gateway failed to authenticate. zipr Id: {0}, gateway id: {1}, key: {2}", credentials.UId, credentials.GatewayId, credentials.ClientKey);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
            }
            return false; 
        }

        /// <summary>
        /// Updates 1 or more properties of the gateway.
        /// </summary>
        /// <param name="gateway">The gateway.</param>
        /// <param name="content">The content.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void UpdateGateway(GatewayCredentials credentials, JObject content)
        {
            try
            {
                string contentStr = content.ToString();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "api/Gateway/" + credentials.GatewayId);
                request.Headers.Add("Auth-GatewayKey", credentials.ClientKey);
                request.Headers.Add("Auth-GatewayId", credentials.GatewayId);
                request.Content = new StringContent(contentStr, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                var result = task.Result;
                result.EnsureSuccessStatusCode();
                _httpError = false;
                _logger.Trace("gateway definition updated: {0}", contentStr);
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
            }
        }

        /// <summary>
        /// Updates or creates the device.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="content">The content.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool UpdateDevice(GatewayCredentials credentials, string device, JObject content, Dictionary<string, string> extraHeaders = null)
        {
            return UpdateDevice(credentials, device, content.ToString(), extraHeaders);
        }

        /// <summary>
        /// Updates or creates the device.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="content">The content.</param>
        public bool UpdateDevice(GatewayCredentials credentials, string device, string content, Dictionary<string, string> extraHeaders = null)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "api/Device/" + device);
                request.Headers.Add("Auth-GatewayKey", credentials.ClientKey);
                request.Headers.Add("Auth-GatewayId", credentials.GatewayId);
                if (extraHeaders != null)
                {
                    foreach (KeyValuePair<string, string> i in extraHeaders)
                        request.Headers.Add(i.Key, i.Value);
                }
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                    result.EnsureSuccessStatusCode();
                _httpError = false;
                _logger.Trace("device updated: {0}", content);
                return true;
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
                return false;
            }
        }

        /// <summary>
        /// Updates or creates the asset.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="content">The content.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void UpdateAsset(GatewayCredentials credentials, string asset, JObject content, Dictionary<string, string> extraHeaders = null)
        {
            try
            {
                string contentStr = content.ToString();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "api/Asset/" + asset);
                request.Headers.Add("Auth-GatewayKey", credentials.ClientKey);
                request.Headers.Add("Auth-GatewayId", credentials.GatewayId);
                if (extraHeaders != null)
                {
                    foreach (KeyValuePair<string, string> i in extraHeaders)
                        request.Headers.Add(i.Key, i.Value);
                }
                request.Content = new StringContent(contentStr, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                    result.EnsureSuccessStatusCode();
                _httpError = false;
                _logger.Trace("asset updated: {0}", contentStr);
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
            }
        }

        /// <summary>
        /// Deletes the device.
        /// </summary>
        /// <param name="device">The global device id (not the local nr).</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void DeleteDevice(GatewayCredentials credentials, string device)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "Device/" + device);
                request.Headers.Add("Auth-GatewayKey", credentials.ClientKey);
                request.Headers.Add("Auth-GatewayId", credentials.GatewayId);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                {
                    if (result.StatusCode == HttpStatusCode.NotFound)
                    {
                        _httpError = false;
                        _logger.Trace("device not found on cloudapp: {0}", device);
                    }
                    else if (result.IsSuccessStatusCode == true)
                    {
                        _httpError = false;
                        _logger.Trace("device deleted: {0}", device);
                    }
                    else
                        result.EnsureSuccessStatusCode();
                }
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
            }
        }

        /// <summary>
        /// requests the primary asset id and it's profile type of the specified device.
        /// </summary>
        /// <param name="deviceId">The device identifier (global version).</param>
        /// <param name="profileType">Type of the profile.</param>
        /// <returns>the asset id that corresponds with the device</returns>
        public string GetPrimaryAssetFor(GatewayCredentials credentials, string deviceId, out string profileType)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/Device/"  + deviceId + "/Primary");
                request.Headers.Add("Auth-GatewayKey", credentials.ClientKey);
                request.Headers.Add("Auth-GatewayId", credentials.GatewayId);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                using (var result = task.Result)
                {
                    result.EnsureSuccessStatusCode();
                    using (HttpContent content = result.Content)
                    {
                        var contentTask = content.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;

                        if (resultContent != null && resultContent.Length >= 50)
                        {
                            JObject obj = JObject.Parse(resultContent);
                            profileType = obj["type"].Value<string>();
                            var id = obj["id"].Value<string>();
                            _httpError = false;
                            return id;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (_httpError == false)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                _httpError = true;
            }
            profileType = null;
            return null;
        }

        #endregion


        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        private void Free()
        {
            if (_http != null)
            {
                _http.Dispose();
                _http = null;
            }
            if (_mqtt != null)
            {
                if (_mqtt.IsConnected == true)
                    _mqtt.Disconnect();
                _mqtt = null;
            }
        }
    }
}
