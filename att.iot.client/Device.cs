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
    public class Device : IDevice, IDisposable
    {
        #region fields
        MqttClient _mqtt;
        HttpClient _http;
        bool _httpError = false;                                                                                     //so we don't continuously write the same error.
        ILogger _logger;
        string _deviceId;
        string _clientId;
        string _clientKey;
        string _brokerUri;
        string _apiUri;
        static DateTime _origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);                                             //for calulatinng unix times.
        #endregion


        #region ctor/~
        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class.
        /// </summary>
        public Device(string clientId, string clientKey, ILogger logger = null, string apiUri = "http://api.smartliving.io", string brokerUri = "broker.smartliving.io")
        {
            _clientId = clientId;
            _clientKey = clientKey;
            _brokerUri = brokerUri;
            _apiUri = apiUri;
            _logger = logger;
            Init();
        }
        /// <summary>
        /// Finalizes an instance of the <see cref="Device"/> class.
        /// </summary>
        ~Device()
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
        /// Occurs when a management command arrived for an asset.
        /// </summary>
        public event EventHandler<AssetManagementCommandData> AssetManagementCommand;


        /// <summary>
        /// Occurs when a management command arrived for a device.
        /// </summary>
        public event EventHandler<string> DeviceManagementCommand;


        /// <summary>
        /// Occurs when the mqtt connection was reset and recreated. This allows the application to recreate the subscriptions.
        /// You can use <see cref="Device.RegisterGateways"/> for this.
        /// </summary>
        public event EventHandler ConnectionReset;
        #endregion


        /// <summary>
        /// gets or sets the id of the device related to this object.
        /// When the device is created with <see cref="CreateDevice(string, string, bool)"/>, then this property is filled in automatically.
        /// </summary>
        public string DeviceId
        {
            get { return _deviceId; }
            set
            {
                if (_deviceId != value)
                {
                    if (string.IsNullOrEmpty(_deviceId) == false)
                        UnSubscribeToTopics();
                    _deviceId = value;
                    if (string.IsNullOrEmpty(_deviceId) == false)
                        SubscribeToTopics();
                }
            }
        }

        /// <summary>
        /// Initializes the server acoording to the specified application settings.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        void Init()
        {
            InitHttp();
            InitMqtt();
            SubscribeToTopics();
        }

        #region mqtt
        private void InitMqtt()
        {
            _mqtt = new MqttClient(_brokerUri);
            _mqtt.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            _mqtt.ConnectionClosed += client_MqttMsgDisconnected;
            string clientId = Guid.NewGuid().ToString().Substring(0, 22);                   //need to respect the max id of mqtt.
            string mqttUserName = _clientId + ":" + _clientId;
            _mqtt.Connect(clientId, mqttUserName, _clientKey, true, 30);
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
                    string mqttUserName = _clientId + ":" + _clientId;
                    _mqtt.Connect(clientId, mqttUserName, _clientKey);
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
            SubscribeToTopics();
            if (ConnectionReset != null)
                ConnectionReset(this, EventArgs.Empty);
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
            if (path.AssetId != null)
            {
                if (AssetManagementCommand != null)
                {
                    AssetManagementCommandData data = new AssetManagementCommandData();
                    data.Asset = path.AssetId[0];
#if WINDOWS_UAP
                    data.Command = System.Text.Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
#else
                    data.Command = System.Text.Encoding.UTF8.GetString(e.Message);
#endif

                    AssetManagementCommand(this, data);
                }
            }
            else if (DeviceManagementCommand != null)
            {
#if WINDOWS_UAP
                string command = System.Text.Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
#else
                string command = System.Text.Encoding.UTF8.GetString(e.Message);
#endif
                DeviceManagementCommand(this, command);
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
#if WINDOWS_UAP
                string val = System.Text.Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
#else
                string val = System.Text.Encoding.UTF8.GetString(e.Message);
#endif
                ActuatorData data = null;
                if (val[0] == '{')
                {
                    data = new JsonActuatorData();
                }
                else
                    data = new StringActuatorData();
                data.Load(val);
                data.Asset = path.AssetId[0];
                ActuatorValue(this, data);
            }
        }

        string getTopicPath(int assetId)
        {
            return string.Format("client/{0}/out/asset/{1}_{2}/state", _clientId, DeviceId, assetId);
        }

        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Send(int asset, object value)
        {
            string toSend = PrepareValueForSending(value);
            string topic = getTopicPath(asset);
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
        /// makes certain that the specified device is monitored so that we receive incomming mqtt messages for the specified device.
        /// Use this if there is only a device available but no gateway.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="deviceId">The device identifier (local) to be monitored.</param>
        void SubscribeToTopics()
        {
            if (_mqtt != null)
            {
                lock (_mqtt)                                                                //only 1 thread can access the mqtt connection at a time.
                {
                    string[] toSubscribe = GetTopics();
                    byte[] qos = new byte[toSubscribe.Length];
                    for (int i = 0; i < toSubscribe.Length; i++)
                        qos[i] = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
                    _mqtt.Subscribe(toSubscribe, qos);
                    if (_logger != null)
                        _logger.Trace("subscribed to: {0}", string.Join(", ", toSubscribe));
                }
            }
        }



        private string[] GetTopics()
        {
            string[] topics = new string[3];
            string root = string.Format("client/{0}/in/device/{1}", _clientId, _deviceId);
            topics[0] = root + "/management";
            topics[1] = root + "/asset/+/command";
            topics[2] = root + "/asset/+/management";
            return topics;
        }



        void _mqtt_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            if (_logger != null)
                _logger.Trace("subscribed");
        }

        /// <summary>
        /// removes the monitors for the specified device, so we no longer receive mqtt messages for it.
        /// Use this only if there is no gateway defined.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        void UnSubscribeToTopics()
        {
            if (_mqtt != null)
            {
                lock (_mqtt)                                                                //only 1 thread can access the mqtt connection at a time.
                {
                    string[] toRemove = GetTopics();
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
        private void InitHttp()
        {
            _http = new HttpClient();
            _http.BaseAddress = new Uri(_apiUri);
        }

        private bool processUpdateDeviceResult(Task<HttpResponseMessage> task)
        {
            using (var result = task.Result)
            {
                using (HttpContent resContent = result.Content)
                {
                    var contentTask = resContent.ReadAsStringAsync();                                          // ... Read the string.
                    string resultContent = contentTask.Result;
                    if (_logger != null)
                        _logger.Trace("response from server: {0}", resultContent);
                    result.EnsureSuccessStatusCode();
                    if (resultContent != null && resultContent.Length >= 30)
                    {
                        JToken obj = JToken.Parse(resultContent);
                        _httpError = false;
                        string newDeviceId = obj["id"].Value<string>();
                        if (DeviceId != newDeviceId)
                        {
                            DeviceId = newDeviceId;
                            if (_logger != null)
                                _logger.Trace("device updated: {0}", resultContent);
                        }
                        else if (_logger != null)
                            _logger.Trace("device created from update: {0}", resultContent);
                        return true;
                    }
                    else
                        throw new Exception("Invalid result received from server, can't find Device Id");
                }
            }
        }

        string getRemoteAssetId(int assetId)
        {
            return string.Format("{0}_{1}", DeviceId, assetId);
        }

        /// <summary>
        /// Updates or creates the asset.
        /// </summary>
        /// <param name="asset">The id of the asset.</param>
        /// <param name="content">The content of the asset as a JObject.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        public void UpdateAsset(int asset, JObject content, Dictionary<string, string> extraHeaders = null)
        {
            try
            {
                string contentStr = content.ToString();
                string uri = "Asset/" + getRemoteAssetId(asset);
                if (_logger != null)
                    _logger.Trace("asset update request\nURI: {0}\nvalue: {1}", uri, contentStr);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
                PrepareRequestForAuth(request);
                if (extraHeaders != null)
                {
                    foreach (KeyValuePair<string, string> i in extraHeaders)
                        request.Headers.Add(i.Key, i.Value);
                }
                request.Content = new StringContent(contentStr, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                {
                    using (HttpContent resContent = result.Content)
                    {
                        var contentTask = resContent.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;
                        if (_logger != null)
                            _logger.Trace("asset update response: {0}", resultContent);
                        result.EnsureSuccessStatusCode();
                    }
                }
                _httpError = false;
            }
            catch (Exception e)
            {
                if (_httpError == false && _logger != null)
                {
                    _httpError = true;
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                }
                else if (_logger == null)
                    throw;

            }
        }

        /// <summary>
        /// Simple way to create or update an asset.
        /// For mor advanced features, use <see cref="IServer.UpdateAsset" />
        /// </summary>
        /// <param name="assetId">The asset identifier (local).</param>
        /// <param name="name">The name of the asset.</param>
        /// <param name="description">The description.</param>
        /// <param name="isActuator">if set to <c>true</c> an actuator should be created, otherwise a sensor.</param>
        /// <param name="type">The profile type of the asst (string, int, float, bool, DateTime, TimeSpan).</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        public bool UpdateAsset(int assetId, string name, string description, bool isActuator, string type, AssetStyle style = AssetStyle.Undefined)
        {
            try
            {
                string content;

                if (type.StartsWith("{"))                                           //check if it's a complex type, if so, don't add "" between type info
                    content = string.Format("{{ \"is\" : \"{0}\", \"name\" : \"{1}\", \"description\" : \"{2}\", \"deviceId\": \"{3}\", \"style\": \"{4}\",  \"profile\" : {5} }}", isActuator == true ? "actuator" : "sensor", name, description, DeviceId, style, type);
                else
                    content = string.Format("{{ \"is\" : \"{0}\", \"name\" : \"{1}\", \"description\" : \"{2}\", \"deviceId\": \"{3}\", \"style\": \"{4}\", \"profile\" : {{ \"type\" : \"{5}\" }}}}", isActuator == true ? "actuator" : "sensor", name, description, DeviceId, style, type);

                string contentStr = content.ToString();
                string uri = "api/Asset/" + getRemoteAssetId(assetId);
                if (_logger != null)
                    _logger.Trace("asset update request\nURI: {0}\nvalue: {1}", uri, contentStr);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
                PrepareRequestForAuth(request);
                request.Content = new StringContent(contentStr, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                {
                    using (HttpContent resContent = result.Content)
                    {
                        var contentTask = resContent.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;
                        if (_logger != null)
                            _logger.Trace("asset update response: {0}", resultContent);
                        result.EnsureSuccessStatusCode();
                    }
                }
                _httpError = false;
                return true;
            }
            catch (Exception e)
            {
                if (_httpError == false && _logger != null)
                {
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                    _httpError = true;
                }
                else if (_logger == null)
                    throw;
            }
            return false;
        }

        private void PrepareRequestForAuth(HttpRequestMessage request)
        {
            request.Headers.Add("Auth-ClientKey", _clientKey);
            request.Headers.Add("Auth-ClientId", _clientId);
        }

        /// <summary>
        /// requests the primary asset id and it's profile type of the  device.
        /// </summary>
        /// the asset definition
        /// </returns>
        public JToken GetPrimaryAsset()
        {
            try
            {
                string uri = "Device/" + DeviceId + "/assets?style=primary";
                if (_logger != null)
                    _logger.Trace("get primary asset; URI: {0}", uri);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                PrepareRequestForAuth(request);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                using (var result = task.Result)
                {
                    using (HttpContent content = result.Content)
                    {
                        var contentTask = content.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;
                        if (_logger != null)
                            _logger.Trace("get primary asset response: {0}", resultContent);
                        result.EnsureSuccessStatusCode();
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
                if (_httpError == false && _logger != null)
                {
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                    _httpError = true;
                }
                else if (_logger == null)
                    throw;
            }
            return null;
        }

        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="asset">The asset id (local to this device). </param>
        /// <param name="value">The value, either a string with a single value or a json object with multiple values.</param>
        public void SendAssetValueHTTP(int asset, object value)
        {
            string toSend = PrepareValueForSendingHTTP(value);
            try
            {
                string uri = "asset/" + getRemoteAssetId(asset) + "/state";
                if (_logger != null)
                    _logger.Trace("send asset value over HTTP request\nURI: {0}\nvalue: {1}", uri, toSend);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
                PrepareRequestForAuth(request);
                request.Content = new StringContent(toSend, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                {
                    using (HttpContent resContent = result.Content)
                    {
                        var contentTask = resContent.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;
                        if (_logger != null)
                            _logger.Trace("send asset value over HTTP response: {0}", resultContent);
                        result.EnsureSuccessStatusCode();
                    }
                }
                _httpError = false;
            }
            catch (Exception e)
            {
                if (_httpError == false && _logger != null)
                {
                    _httpError = true;
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                }
                if (_logger != null)
                    _logger.Error("failed to send message over http, to: {0}, content: {1}", asset, toSend);
                else
                    throw;
            }
        }

        /// <summary>
        /// sends a command to an asset on another device.
        /// </summary>
        /// <remarks>
        /// Use this function to command another device. You can only send commands to devices that you own, which are in the
        /// same account as this device.
        /// </remarks>
        /// <param name="asset">The full id of the asset to send a command to</param>
        /// <param name="value">The value to send to the command</param>
        public void SendCommandTo(string asset, object value)
        {
            string toSend = value.ToString();
            try
            {
                string uri = "asset/" + asset + "/command";
                if (_logger != null)
                    _logger.Trace("send command request\nURI: {0}\nvalue: {1}", uri, toSend);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
                PrepareRequestForAuth(request);
                request.Content = new StringContent(toSend, Encoding.UTF8, "application/json");
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                using (var result = task.Result)
                {
                    using (HttpContent resContent = result.Content)
                    {
                        var contentTask = resContent.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;
                        if (_logger != null)
                            _logger.Trace("send command response: {0}", resultContent);
                        result.EnsureSuccessStatusCode();
                    }
                }
                _httpError = false;
            }
            catch (Exception e)
            {
                if (_httpError == false && _logger != null)
                {
                    _httpError = true;
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                }
                if (_logger != null)
                    _logger.Error("failed to send message over http, to: {0}, content: {1}", asset, toSend);
                else
                    throw;
            }
        }

        /// <summary>
        /// gets the last stored value of the specified asset.
        /// </summary>
        /// <param name="asset">the id (local to this device) of the asset for which to return the last recorded value.</param>
        /// <returns>the value as a json structure.</returns>
        public JToken GetAssetState(int asset)
        {
            try
            {
                string uri = "/asset/" + DeviceId + "_" + asset.ToString() + "/state";
                if (_logger != null)
                    _logger.Trace("get asset state; URI: {0}", uri);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                PrepareRequestForAuth(request);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                using (var result = task.Result)
                {
                    using (HttpContent content = result.Content)
                    {
                        var contentTask = content.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;
                        if (_logger != null)
                            _logger.Trace("get asset state response: {0}", resultContent);
                        result.EnsureSuccessStatusCode();
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
                if (_httpError == false && _logger != null)
                {
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                    _httpError = true;
                }
                else if (_logger == null)
                    throw;
            }
            return null;
        }

        /// <summary>
        /// gets all the assets that the cloud knows for this device.
        /// </summary>
        /// <returns>a json object (array) containing all the asset definitions</returns>
        public JToken GetAssets()
        {
            try
            {
                string uri = "/device/" + DeviceId + "/assets";
                if (_logger != null)
                    _logger.Trace("get assets; URI: {0}", uri);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                PrepareRequestForAuth(request);
                var task = _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                using (var result = task.Result)
                {
                    using (HttpContent content = result.Content)
                    {
                        var contentTask = content.ReadAsStringAsync();                                          // ... Read the string.
                        string resultContent = contentTask.Result;
                        if (_logger != null)
                            _logger.Trace("get assets response: {0}", resultContent);
                        result.EnsureSuccessStatusCode();
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
                if (_httpError == false && _logger != null)
                {
                    _logger.Error("HTTP comm problem: {0}", e.ToString());
                    _httpError = true;
                }
                else if (_logger == null)
                    throw;
            }
            return null;
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
