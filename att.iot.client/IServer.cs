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
    public interface IServer
    {
        /// <summary>
        /// Occurs when a new actuator value arrived.
        /// </summary>
        event EventHandler<ActuatorData> ActuatorValue;


        /// <summary>
        /// Occurs when a management command arrived.
        /// </summary>
        event EventHandler<ManagementCommandData> ManagementCommand;

        /// <summary>
        /// Occurs when the mqtt connection was reset and recreated. This allows the application to recreate the subscriptions.
        /// You can use <see cref="Server.RegisterGateways"/> for this.
        /// </summary>
        event EventHandler ConnectionReset;

        /// <summary>
        /// Creates a new gateway.
        /// </summary>
        /// <param name="id">a token that uniquely identifies the gateway.</param>
        /// <param name="ipAddress">The ip address of the gateway, so we can use this in logs.</param>
        /// <param name="data">The data object that defines the gateway.</param>
        /// <returns></returns>
        bool CreateGateway(string id, byte[] ipAddress, JObject data);

        /// <summary>
        /// Updates 1 or more properties of the gateway.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="content">The gateway definition as a JObject.</param>
        void UpdateGateway(GatewayCredentials credentials, JObject content);

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
        bool FinishClaim(GatewayCredentials credentials, JObject definition);

        /// <summary>
        /// Gets the gateway with the specified id. Note: this does not use the UID, but the ATT generated id.
        /// </summary>
        /// <param name="id">The credentials for the gateway.</param>
        /// <returns>a jobject containing the gateway definition</returns>
        JObject GetGateway(GatewayCredentials credentials);

        /// <summary>
        /// makes certain that the specified gateway is monitored so that we receive incomming mqtt messages for the specified gateway.
        /// Use this if there is a gateway available.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        void SubscribeToTopics(GatewayCredentials credentials);

        /// <summary>
        /// makes certain that the specified device is monitored so that we receive incomming mqtt messages for the specified device.
        /// Use this if there is only a device available but no gateway.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="deviceId">The device identifier (local) to be monitored.</param>
        void SubscribeToTopics(GatewayCredentials credentials, string deviceId);

        /// <summary>
        /// walks over all the ziprs and subscribes to the topics.
        /// </summary>
        /// <param name="toSubscribe">The list of credentials to supscribe for</param>
        void RegisterGateways(IEnumerable<GatewayCredentials> toSubscribe);

        /// <summary>
        /// removes the monitors for the specified gateway, so we no longer receive mqtt messages for it.
        /// Use this only if there is a gateway defined.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        void UnRegisterGateway(GatewayCredentials credentials);

        /// <summary>
        /// removes the monitors for the specified device, so we no longer receive mqtt messages for it.
        /// Use this only if there is no gateway defined.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        void UnRegisterDevice(GatewayCredentials credentials, string deviceId);

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
        bool UpdateDevice(GatewayCredentials credentials, string device, JObject content, Dictionary<string, string> extraHeaders = null);

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
        bool UpdateDevice(GatewayCredentials credentials, string device, string content, Dictionary<string, string> extraHeaders = null);

        /// <summary>
        /// Simple way to create or update a devce. 
        /// Works for assets that belong to stand alone devices or devices connected to a gateway.
        /// When there is no gateway defined, don't fill in the property in the credentials
        /// For mor advanced features, use <see cref="IServer.UpdateDevice"/>
        /// </summary>
        /// <param name="credentials">The credentials for the gateway and client.</param>
        /// <param name="deviceId">The device identifier as known by the cloudapp (no local id.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <returns>
        /// true when succesfull
        /// </returns>
        bool UpdateDevice(GatewayCredentials credentials, string deviceId, string name, string description);


        /// <summary>
        /// Simple way to update an devce. 
        /// Works for assets that belong to stand alone devices or devices connected to a gateway.
        /// When there is no gateway defined, don't fill in the property in the credentials
        /// For mor advanced features, use <see cref="IServer.UpdateDevice"/>
        /// </summary>
        /// <param name="credentials">The credentials for the gateway and client.</param>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <returns>
        /// The device identifier as known by the cloudapp
        /// </returns>
        string CreateDevice(GatewayCredentials credentials, string name, string description);

        /// <summary>
        /// Updates or creates the asset.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="asset">The id of the asset (global, cloud version, not local).</param>
        /// <param name="content">The content of the asset as a JObject.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        void UpdateAsset(GatewayCredentials credentials, string asset, JObject content, Dictionary<string, string> extraHeaders = null);

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
        bool UpdateAsset(GatewayCredentials credentials, string deviceId, int assetId, string name, string description, bool isActuator, string type);

        /// <summary>
        /// Deletes the device.
        /// </summary>
        /// <param name="id">The credentials for the gateway and client.</param>
        /// <param name="device">The global device id (not the local nr).</param>
        void DeleteDevice(GatewayCredentials credentials, string device);

        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="value">The value, either a string witha single value or a json object with multiple values.</param>
        void AssetValue(TopicPath asset, object value);

        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="credentials">The credentials to authenticate with in the platform.</param>
        /// <param name="asset">The asset id (remote, what the server uses).</param>
        /// <param name="value">The value, either a string with a single value or a json object with multiple values.</param>
        void SendAssetValueHTTP(GatewayCredentials credentials, string asset, object value);

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
