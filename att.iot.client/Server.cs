using Newtonsoft.Json.Linq;
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
        ILogger _logger;
        static DateTime _origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);                                             //for calulatinng unix times.
        #endregion


        #region ctor/~
        /// <summary>
        /// Initializes a new instance of the <see cref="Server"/> class.
        /// </summary>
        public Server(ILogger logger = null)
        {
            _logger = logger;
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
            _mqtt.ConnectionClosed += client_MqttMsgDisconnected;
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
        private string GetSetting(NameValueCollection appSettings, string key, string defaultVal)
        {
            string found = appSettings.Get(key);
            if (string.IsNullOrEmpty(found) == true)
                found = defaultVal;
            if (_logger != null)
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
            if (_logger != null)
                _logger.Error("mqtt connection lost, recreating...");
            string clientId = Guid.NewGuid().ToString();
            while (_mqtt.IsConnected == false)
            {
                try
                {
                    _mqtt.Connect(clientId, _mqttUserName, _mqttpwd);
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.Error(ex.Message);
                }
            }
            if (_logger != null)
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
        /// walks over all the gateways and subscribes to the topics.
        /// </summary>
        /// <param name="toSubscribe">The list of credentials to supscribe for</param>
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
                if (_logger != null)
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
            string toSend = PrepareValueForSending(value);           
            string topic = asset.ToString();
            lock (_mqtt)                                                               //make certain that the messages sent by different threads at the same time, don't intermingle.
            {
                _mqtt.Publish(topic, System.Text.Encoding.UTF8.GetBytes(toSend));
                if (_logger != null)
                    _logger.Trace("message published, topic: {0}, content: {1}", topic, toSend);
            }
        }

        private string PrepareValueForSending(object value)
        {
            string toSend = null;
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
                JObject result = new JObject();
                result.Add("at", DateTime.UtcNow);
                result.Add("value", (JObject)value);
                toSend = result.ToString();
            }
            else
                throw new NotSupportedException("value is of a none supported type");
            return toSend;
        }

        /// <summary>
        /// makes certain that the specified gateway is monitored so that we receive incomming mqtt messages for the specified gateway.
        /// Use this if there is a gateway available.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        public void SubscribeToTopics(GatewayCredentials credentials)
        {
            if (_mqtt != null)
            {
                lock (_mqtt)                                                                //only 1 thread can access the mqtt connection at a time.
                    SubscribeUnsafe(credentials);
            }
        }

        /// <summary>
        /// makes certain that the specified device is monitored so that we receive incomming mqtt messages for the specified device.
        /// Use this if there is only a device available but no gateway.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="deviceId">The device identifier (local) to be monitored.</param>
        public void SubscribeToTopics(GatewayCredentials credentials, string deviceId)
        {
            if (_mqtt != null)
            {
                lock (_mqtt)                                                                //only 1 thread can access the mqtt connection at a time.
                    SubscribeUnsafe(credentials, deviceId);
            }
        }

        private void SubscribeUnsafe(GatewayCredentials credentials, string deviceId)
        {
            string[] toSubscribe = GetTopics(credentials, deviceId);
            byte[] qos = new byte[toSubscribe.Length];
            for (int i = 0; i < toSubscribe.Length; i++)
                qos[i] = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
            _mqtt.Subscribe(toSubscribe, qos);
            if (_logger != null)
                _logger.Trace("subscribed to: {0}", string.Join(", ", toSubscribe));
        }

        private string[] GetTopics(GatewayCredentials credentials, string deviceId)
        {
            string[] topics = new string[3];
            string root = string.Format("client/{0}/in/device/{1}", credentials.ClientId, deviceId);
            topics[0] = root + "/management";
            topics[1] = root + "/asset/+/command";
            topics[2] = root + "/asset/+/management";
            return topics;
        }

        private void SubscribeUnsafe(GatewayCredentials credentials)
        {
            string[] toSubscribe = GetTopics(credentials);
            byte[] qos = new byte[toSubscribe.Length];
            for(int i = 0; i < toSubscribe.Length; i++)
                qos[i] = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
            _mqtt.Subscribe(toSubscribe, qos);
            if (_logger != null)
                _logger.Trace("subscribed to: {0}", string.Join(", ", toSubscribe));
        }

        private string[] GetTopics(GatewayCredentials credentials)
        {
            if (string.IsNullOrEmpty(credentials.GatewayId) == false)
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
            else
                throw new InvalidOperationException("Gateway id required in the credentials for this operation");
        }

        void _mqtt_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            if (_logger != null)
                _logger.Trace("subscribed");
        }
        /// <summary>
        /// removes the monitors for the specified gateway, so we no longer receive mqtt messages for it.
        /// Use this only if there is a gateway defined.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        public void UnRegisterGateway(GatewayCredentials credentials)
        {
            if (_mqtt != null && string.IsNullOrEmpty(credentials.GatewayId) == false)      //if there is no gatewayId yet (still registering), then there is no need to remove any topics: we haven't yet been able to subscribe for them.
            {
                lock (_mqtt)                                                                //only 1 thread can access the mqtt connection at a time.
                {
                    string[] toRemove = GetTopics(credentials);
                    _mqtt.Unsubscribe(toRemove);
                    if (_logger != null)
                        _logger.Trace("unsubscribed from: {0}", toRemove);
                }
            }
        }

        /// <summary>
        /// removes the monitors for the specified device, so we no longer receive mqtt messages for it.
        /// Use this only if there is no gateway defined.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        public void UnRegisterDevice(GatewayCredentials credentials, string deviceId)
        {
            if (_mqtt != null)     
            {
                lock (_mqtt)                                                                //only 1 thread can access the mqtt connection at a time.
                {
                    string[] toRemove = GetTopics(credentials, deviceId);
                    _mqtt.Unsubscribe(toRemove);
                    if (_logger != null)
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
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="message">The message to report.</param>
        public void ReportError(GatewayCredentials credentials, string message)
        {
            Report(credentials, message, "gateway error");
        }

        /// <summary>
        /// Reports a warning back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="message">The message to report.</param>
        public void ReportWarning(GatewayCredentials credentials, string message)
        {
            Report(credentials, message, "gateway warning");
        }

        /// <summary>
        /// Reports an  info message back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="gatewayId">The gateway identifier.</param>
        /// <param name="message">The message to report.</param>
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
                if (_logger != null)
                    _logger.Trace("{0}: {1}", title, content);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "api/UserNotification");
                PrepareRequestForAuth(request, credentials);
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                    result.EnsureSuccessStatusCode();
                _httpError = false;
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
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
                        if (_logger != null)
                            _logger.Error("Unauthorised access: {0}, IP address: {1}", id, BitConverter.ToString(ipAddress));
                        return false;
                    }
                    result.EnsureSuccessStatusCode();
                }
                _httpError = false;
                if (_logger != null)
                    _logger.Trace("gateway created: {0}", content);
                return true;
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
                return false;
            }
        }

        /// <summary>
        /// Gets the gateway with the specified id. Note: this does not use the UID, but the ATT generated id.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <returns>a jobject containing the gateway definition</returns>
        public JObject GetGateway(GatewayCredentials credentials)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "Gateway/" + credentials.GatewayId);
                PrepareRequestForAuth(request, credentials);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                using (var result = task.Result)
                {
                    if (result.StatusCode == HttpStatusCode.Unauthorized)                                           //not a success code, but very common, special case, don't treat as comm problem.
                    {
                        if (_logger != null)
                            _logger.Error("Unauthorised request for gateway details: {0}, UID: {1}", credentials.GatewayId, credentials.UId);
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
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
                return null;
            }
        }

        /// <summary>
        /// Gets the gateway credentials from the server. This only works if the <see cref="GatewayCredentials.UId" /> is already filled in.
        /// This function will fill in the other fields.
        /// Logs an error if the object was not found.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="definition">The definition of the gateway that needs to be udpated.</param>
        /// <returns>
        /// True if the operation was successful.
        /// </returns>
        public bool FinishClaim(GatewayCredentials credentials, JObject definition)
        {
            try
            {
                //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "api/GatewayEnroll?id=" + credentials.UIDId);
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
                            if (_logger != null)
                                _logger.Trace("credentials found. UID Id: {0}, gateway id: {1}, key: {2}", credentials.UId, credentials.GatewayId, credentials.ClientKey);
                            return true;
                        }
                    }
                }
                _httpError = false;
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
            }
            return false;                                                                                       //something went wrong if we get here.
        }

        /// <summary>
        /// Authenticates the gateway with the specified credentials.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <returns>True if succesful</returns>
        public bool Authenticate(GatewayCredentials credentials)
        {
            try
            {
                
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/authentication");
                PrepareRequestForAuth(request, credentials);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                using (var result = task.Result)
                {
                    if (result.IsSuccessStatusCode)
                    {
                        if (_logger != null)
                            _logger.Trace("gateway authenticated. UID: {0}, gateway id: {1}, key: {2}", credentials.UId, credentials.GatewayId, credentials.ClientKey);
                        return true;
                    }
                    else
                    {
                        if (_logger != null)
                            _logger.Error("gateway failed to authenticate. UID Id: {0}, gateway id: {1}, key: {2}", credentials.UId, credentials.GatewayId, credentials.ClientKey);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
            }
            return false; 
        }

        /// <summary>
        /// Updates 1 or more properties of the gateway.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="content">The gateway definition as a JObject.</param>
        public void UpdateGateway(GatewayCredentials credentials, JObject content)
        {
            try
            {
                string contentStr = content.ToString();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "Gateway/" + credentials.GatewayId);
                PrepareRequestForAuth(request, credentials);
                request.Content = new StringContent(contentStr, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                var result = task.Result;
                result.EnsureSuccessStatusCode();
                _httpError = false;
                if (_logger != null)
                    _logger.Trace("gateay definition updated: {0}", contentStr);
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
            }
        }

        /// <summary>
        /// Updates or creates the device.
        /// </summary>
        /// <param name="credentials">The credentials for the gateway and client.</param>
        /// <param name="device">The device identifier (cloud platform version, not local).</param>
        /// <param name="content">The device definition as a json object.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        public bool UpdateDevice(GatewayCredentials credentials, string device, JObject content, Dictionary<string, string> extraHeaders = null)
        {
            return UpdateDevice(credentials, device, content.ToString(), extraHeaders);
        }

        /// <summary>
        /// Updates or creates the device.
        /// </summary>
        /// <param name="credentials">The credentials for the gateway and client.</param>
        /// <param name="device">The device identifier (cloud platform version, not local).</param>
        /// <param name="content">The device definition as a string (json format).</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        public bool UpdateDevice(GatewayCredentials credentials, string device, string content, Dictionary<string, string> extraHeaders = null)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "Device/" + device);
                PrepareRequestForAuth(request, credentials);
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
                if (_logger != null)
                    _logger.Trace("device updated: {0}", content);
                return true;
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
                return false;
            }
        }

        /// <summary>
        /// Simple way to update an devce.
        /// Works for assets that belong to stand alone devices or devices connected to a gateway.
        /// When there is no gateway defined, don't fill in the property in the credentials
        /// For mor advanced features, use <see cref="IServer.UpdateDevice" />
        /// </summary>
        /// <param name="credentials">The credentials for the gateway and client.</param>
        /// <param name="deviceId">The device identifier as known by the cloudapp (no local id.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="activityEnabled">if set to <c>true</c>, historical data will be stored for all the assets on this device.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        public bool UpdateDevice(GatewayCredentials credentials, string deviceId, string name, string description, bool activityEnabled = false)
        {
            try
            {
                string content = string.Format("{{ 'description': '{0}', 'name': '{1}', 'activityEnabled': {2}}}", name, description, activityEnabled);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "Device/" + deviceId);
                PrepareRequestForAuth(request, credentials);
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                    result.EnsureSuccessStatusCode();
                _httpError = false;
                if (_logger != null)
                    _logger.Trace("device updated: {0}", content);
                return true;
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
                return false;
            }
        }

        /// <summary>
        /// Simple way to create a devce. 
        /// When there is no gateway defined, don't fill in the property in the credentials
        /// For mor advanced features, use <see cref="IServer.UpdateDevice"/>
        /// </summary>
        /// <param name="credentials">The credentials for the gateway and client.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="activityEnabled">if set to <c>true</c>, historical data will be stored for all the assets on this device.</param>
        /// <returns>
        /// The device identifier as known by the cloudapp
        /// </returns>
        public string CreateDevice(GatewayCredentials credentials, string name, string description, bool activityEnabled = false)
        {
            try
            {
                string content = string.Format(@"{{ 'description' : '{0}', 'name' : '{1}', 'activityEnabled': {2} }}", name, description, activityEnabled);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "Device");
                PrepareRequestForAuth(request, credentials);
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                {
                    result.EnsureSuccessStatusCode();
                    using (HttpContent resContent = result.Content)
                    {
                        var contentTask = resContent.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;

                        if (resultContent != null && resultContent.Length >= 50)
                        {
                            JToken obj = JToken.Parse(resultContent);
                            _httpError = false;
                            return obj["id"].Value<string>();
                        }
                    }
                }

                _httpError = false;
                if (_logger != null)
                    _logger.Trace("device created: {0}", content);
                return null;
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
                return null;
            }
        }

        /// <summary>
        /// Updates or creates the asset.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="asset">The id of the asset (global, cloud version, not local).</param>
        /// <param name="content">The content of the asset as a JObject.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        public void UpdateAsset(GatewayCredentials credentials, string asset, JObject content, Dictionary<string, string> extraHeaders = null)
        {
            try
            {
                string contentStr = content.ToString();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "Asset/" + asset);
                PrepareRequestForAuth(request, credentials);
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
                if (_logger != null)
                    _logger.Trace("asset updated: {0}", contentStr);
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
            }
        }

        /// <summary>
        /// Simple way to create or update an asset.
        /// For mor advanced features, use <see cref="IServer.UpdateAsset" />
        /// </summary>
        /// <param name="credentials">The credentials for the gateway and client.</param>
        /// <param name="deviceId">The device identifier. If there is no gateway defined, this has to be the device id as specified by cloudapp. If
        /// There is a gateway known, the id of the device can be local to the gateway.
        /// </param>
        /// <param name="assetId">The asset identifier (local).</param>
        /// <param name="name">The name of the asset.</param>
        /// <param name="description">The description.</param>
        /// <param name="isActuator">if set to <c>true</c> an actuator should be created, otherwise a sensor.</param>
        /// <param name="type">The profile type of the asst (string, int, float, bool, DateTime, TimeSpan).</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        public bool UpdateAsset(GatewayCredentials credentials, string deviceId, int assetId, string name, string description, bool isActuator, string type)
        {
            try
            {
                string content;

                if (type.StartsWith("{"))                                           //check if it's a complex type, if so, don't add "" between type info
                    content = string.Format("{{ \"is\" : \"{0}\", \"name\" : \"{1}\", \"description\" : \"{2}\", \"deviceId\": \"{3}\", \"profile\" : {4} }}", isActuator == true ? "actuator" : "sensor", name, description, deviceId, type);
                else
                    content = string.Format("{{ \"is\" : \"{0}\", \"name\" : \"{1}\", \"description\" : \"{2}\", \"deviceId\": \"{3}\", \"profile\" : {{ \"type\" : \"{4}\" }}}}", isActuator == true ? "actuator" : "sensor", name, description, deviceId, type);

                string remoteAssetId;
                if (string.IsNullOrEmpty(credentials.GatewayId) == false)
                {
                    TopicPath path = new TopicPath()
                    {
                        Gateway = credentials.GatewayId,
                        DeviceId = deviceId,
                        AssetId = new int[] { assetId }
                    };
                    remoteAssetId = path.RemoteAssetId;
                }
                else
                    remoteAssetId = string.Format("{0}_{1}", deviceId, assetId);

                string contentStr = content.ToString();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "api/Asset/" + remoteAssetId);
                PrepareRequestForAuth(request, credentials);
                request.Content = new StringContent(contentStr, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                    result.EnsureSuccessStatusCode();
                _httpError = false;
                if (_logger != null)
                    _logger.Trace("asset updated: {0}", contentStr);
                return true;
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
            }
            return false;
        }

        private void PrepareRequestForAuth(HttpRequestMessage request, GatewayCredentials credentials)
        {
            if (string.IsNullOrEmpty(credentials.GatewayId) == false)
            {
                request.Headers.Add("Auth-GatewayKey", credentials.ClientKey);
                request.Headers.Add("Auth-GatewayId", credentials.GatewayId);
            }
            else
            {
                request.Headers.Add("Auth-ClientKey", credentials.ClientKey);
                request.Headers.Add("Auth-ClientId", credentials.ClientId);
            }
        }

        /// <summary>
        /// Deletes the device.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="device">The global device id (not the local nr).</param>
        public void DeleteDevice(GatewayCredentials credentials, string device)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "Device/" + device);
                PrepareRequestForAuth(request, credentials);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                {
                    if (result.StatusCode == HttpStatusCode.NotFound)
                    {
                        _httpError = false;
                        if (_logger != null)
                            _logger.Trace("device not found on cloudapp: {0}", device);
                    }
                    else if (result.IsSuccessStatusCode == true)
                    {
                        _httpError = false;
                        if (_logger != null)
                            _logger.Trace("device deleted: {0}", device);
                    }
                    else
                        result.EnsureSuccessStatusCode();
                }
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
            }
        }

        /// <summary>
        /// requests the primary asset id and it's profile type of the specified device.
        /// </summary>
        /// <param name="credentials">The credentials to log into the cloudapp server with.</param>
        /// <param name="deviceId">The device identifier (global version).</param>
        /// <returns>
        /// the asset id that corresponds with the device
        /// </returns>
        public JToken GetPrimaryAssetFor(GatewayCredentials credentials, string deviceId)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "Device/" + deviceId + "/assets?style=primary");
                PrepareRequestForAuth(request, credentials);
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
                            JToken obj = JToken.Parse(resultContent);
                            _httpError = false;
                            return obj;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
            }
            return null;
        }

        /// <summary>
        /// Parses the input asset or assets and extracts for each entry, the asset id and the profile type.
        /// This function can be used to extract the interesting values from the result produced by <see cref="IServer." />
        /// </summary>
        /// <param name="values">A single asset definition or an array..</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">variable 'values': Jarray or JObject expected</exception>
        public IEnumerable<KeyValuePair<string, JToken>> GetAssetIdsAndProfileTypes(JToken values)
        {
            if (values is JArray)
            {
                JArray list = (JArray)values;
                foreach (JObject obj in list.Values<JObject>())
                {
                    JToken profileType = obj["type"];
                    var id = obj["id"].Value<string>();
                    yield return new KeyValuePair<string, JToken>(id, profileType);
                }
            }
            else if (values is JObject)
            {
                JObject obj = (JObject)values;
                JToken profileType = obj["type"];
                var id = obj["id"].Value<string>();
                yield return new KeyValuePair<string, JToken>(id, profileType);
            }
            else
                throw new ArgumentException("variable 'values': Jarray or JObject expected");
        }

        public void SendAssetValueHTTP(GatewayCredentials credentials, string asset, object value)
        {
            string toSend = PrepareValueForSendingHTTP(value);
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "asset/" + asset  + "/state");
                PrepareRequestForAuth(request, credentials);
                request.Content = new StringContent(toSend, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                    result.EnsureSuccessStatusCode();
                _httpError = false;
            }
            catch (Exception e)
            {
                _httpError = true;
                if (_httpError == false && _logger != null)
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                else if(_logger == null)
                    throw;
            }
            if (_logger != null)
                _logger.Trace("message send over http, to: {0}, content: {1}", asset, toSend);
        }

        private string PrepareValueForSendingHTTP(object value)
        {
            string toSend = null;
            JObject result = new JObject();
            
            result.Add("at", DateTime.UtcNow);
            if (value is JObject)
                result.Add("value", (JObject)value);
            else
            {
                JToken conv;
                try
                {
                    conv = JToken.Parse((string)value);                                         //we need to do this for adding numbers correctly (not as strings, but as numbers)
                }
                catch
                {
                    conv = JToken.FromObject(value);                                            //we need to do this for strings. for some reason, the jtoken parser can't load string values.
                }
                result.Add("value", conv);
            }
            toSend = result.ToString();
            return toSend;
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
                {
                    _mqtt.ConnectionClosed -= client_MqttMsgDisconnected;       //we don't need this callback anymore -> we need to close the connection, not try to restart it again
                    _mqtt.Disconnect();
                }
                _mqtt = null;
            }
        }
    }
}
