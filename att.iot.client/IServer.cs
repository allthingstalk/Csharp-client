using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace att.iot.client
{
    /// <summary>
    /// provides access to the backing IOT server system. It allows us to save data and receive messages from the cloudapp.
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// Occurs when a new actuator value arrived.
        /// </summary>
        event EventHandler<string> ActuatorValue;

        event EventHandler<JToken> ActuatorValueJson;


        /// <summary>
        /// Occurs when a management command arrived for an asset.
        /// </summary>
        event EventHandler<AssetManagementCommandData> AssetManagementCommand;

        /// <summary>
        /// Occurs when a management command arrived for a device.
        /// </summary>
        event EventHandler<string> DeviceManagementCommand;

        /// <summary>
        /// Occurs when the mqtt connection was reset and recreated. This allows the application to recreate the subscriptions.
        /// You can use <see cref="Device.RegisterGateways"/> for this.
        /// </summary>
        event EventHandler ConnectionReset;    

        /// <summary>
        /// Updates or creates the device.
        /// </summary>
        /// <param name="content">The device definition as a json object.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        bool UpdateDevice(JObject content, Dictionary<string, string> extraHeaders = null);

        /// <summary>
        /// Updates or creates the device.
        /// </summary>
        /// <param name="content">The device definition as a string (json format).</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        bool UpdateDevice(string content, Dictionary<string, string> extraHeaders = null);

        /// <summary>
        /// Simple way to create or update a devce. 
        /// Works for assets that belong to stand alone devices or devices connected to a gateway.
        /// When there is no gateway defined, don't fill in the property in the credentials
        /// For mor advanced features, use <see cref="IServer.UpdateDevice"/>
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="activityEnabled">if set to <c>true</c>, historical data will be stored for all the assets on this device.</param>
        /// <returns>
        /// true when succesfull
        /// </returns>
        bool UpdateDevice(string name, string description, bool activityEnabled = false);


        /// <summary>
        /// Simple way to update an devce.
        /// Works for assets that belong to stand alone devices or devices connected to a gateway.
        /// When there is no gateway defined, don't fill in the property in the credentials
        /// For mor advanced features, use 
        /// <see cref="IServer.UpdateDevice" />
        /// </summary>
        /// <param name="credentials">The credentials for the gateway and client.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="activityEnabled">if set to <c>true</c>, historical data will be stored for all the assets on this device.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        bool CreateDevice(string name, string description, bool activityEnabled = false);

        /// <summary>
        /// Updates or creates the asset.
        /// </summary>
        /// <param name="asset">The id of the asset.</param>
        /// <param name="content">The content of the asset as a JObject.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        void UpdateAsset(int asset, JObject content, Dictionary<string, string> extraHeaders = null);

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
        bool UpdateAsset(int assetId, string name, string description, bool isActuator, string type);

        /// <summary>
        /// Deletes the device.
        /// </summary>
        void DeleteDevice();

        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="value">The value, either a string witha single value or a json object with multiple values.</param>
        void Send(int asset, object value);

        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="credentials">The credentials to authenticate with in the platform.</param>
        /// <param name="asset">The asset id (remote, what the server uses). Define it as a topic path, which includes all relevant components</param>
        /// <param name="value">The value, either a string with a single value or a json object with multiple values.</param>
        void SendAssetValueHTTP(GatewayCredentials credentials, TopicPath asset, object value);

        /// <summary>
        /// Reports an error back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="message">The message to report.</param>
        void ReportError(GatewayCredentials credentials, string message);

        /// <summary>
        /// Reports a warning back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="message">The message to report.</param>
        void ReportWarning(GatewayCredentials credentials, string message);

        /// <summary>
        /// Reports an  info message back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="gatewayId">The gateway identifier.</param>
        /// <param name="message">The message to report.</param>
        void ReportInfo(GatewayCredentials credentials, string message);

        /// <summary>
        /// Initializes the server acoording to the specified application settings.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        void Init(NameValueCollection appSettings);

        /// <summary>
        /// Authenticates the zipr with the specified credentials.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <returns>True if succesful</returns>
        bool Authenticate(GatewayCredentials credentials);


        /// <summary>
        /// requests the primary asset id and it's profile type of the specified device.
        /// </summary>
        /// <param name="credentials">The credentials to log into the cloudapp server with.</param>
        /// <param name="deviceId">The device identifier (global version).</param>
        /// <returns>
        /// the asset id that corresponds with the device
        /// </returns>
        JToken GetPrimaryAssetFor(GatewayCredentials credentials, string deviceId);

        /// <summary>
        /// Parses the input asset or assets and extracts for each entry, the asset id and the profile type.
        /// This function can be used to extract the interesting values from the result produced by <see cref="IServer."/>
        /// </summary>
        /// <param name="values">A single asset definition or an array..</param>
        /// <returns></returns>
        IEnumerable<KeyValuePair<string, JToken>> GetAssetIdsAndProfileTypes(JToken values);
    }
}
