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
        /// <param name="gateway">The gateway.</param>
        /// <param name="content">The content.</param>
        void UpdateGateway(GatewayCredentials credentials, JObject content);

        /// <summary>
        /// Finishes the claim process and
        /// Gets the gateway credentials from the server. This only works if the <see cref="GatewayCredentials.UId" /> is already filled in.
        /// This function will fill in the other fields.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="definition">The definition of the gateway.</param>
        /// <returns></returns>
        bool FinishClaim(GatewayCredentials credentials, JObject definition);

        /// <summary>
        /// Gets the gateway with the specified id. Note: this does not use the UID, but the ATT generated id.
        /// </summary>
        /// <param name="id">The credentials for the gateway.</param>
        /// <returns>a jobject containing the gateway definition</returns>
        JObject GetGateway(GatewayCredentials credentials);

        /// <summary>
        /// makes certain that the specified gateway is monitored so that we receive incomming mqtt messages for the specified gateway.
        /// </summary>
        /// <param name="credentials">The credentials of the gateway.</param>
        void SubscribeToTopics(GatewayCredentials credentials);

        /// <summary>
        /// walks over all the ziprs and subscribes to the topics.
        /// </summary>
        void RegisterGateways(IEnumerable<GatewayCredentials> toSubscribe);

        /// <summary>
        /// removes the monitors for the specified gateway, so we no longer receive mqtt messages for it.
        /// </summary>
        /// <param name="id">The identifier.</param>
        void UnRegisterGateway(GatewayCredentials credentials);

        /// <summary>
        /// Updates or creates the device.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="device">The device.</param>
        /// <param name="content">The content.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        bool UpdateDevice(GatewayCredentials credentials, string device, JObject content, Dictionary<string, string> extraHeaders = null);

        /// <summary>
        /// Updates or creates the device.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="device">The device.</param>
        /// <param name="content">The content.</param>
        /// /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        /// <returns>True if scuccesfull, otherwise false</returns>
        bool UpdateDevice(GatewayCredentials credentials, string device, string content, Dictionary<string, string> extraHeaders = null);

        /// <summary>
        /// Updates or creates the asset.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="content">The content.</param>
        void UpdateAsset(GatewayCredentials credentials, string asset, JObject content, Dictionary<string, string> extraHeaders = null);

        /// <summary>
        /// Deletes the device.
        /// </summary>
        /// <param name="device">The device.</param>
        void DeleteDevice(GatewayCredentials credentials, string device);

        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="value">The value, either a string witha single value or a json object with multiple values.</param>
        void AssetValue(TopicPath asset, object value);

        /// <summary>
        /// Reports an error back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="gatewayId">The gateway identifier.</param>
        /// <param name="message">The message.</param>
        void ReportError(GatewayCredentials credentials, string message);

        /// <summary>
        /// Reports a warning back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="gatewayId">The gateway identifier.</param>
        /// <param name="message">The message.</param>
        void ReportWarning(GatewayCredentials credentials, string message);

        /// <summary>
        /// Reports an  info message back to the cloudapp for the user that owns the specified id.
        /// </summary>
        /// <param name="gatewayId">The gateway identifier.</param>
        /// <param name="message">The message.</param>
        void ReportInfo(GatewayCredentials credentials, string message);

        /// <summary>
        /// Initializes the server acoording to the specified application settings.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        void Init(NameValueCollection appSettings);

        /// <summary>
        /// Authenticates the zipr with the specified credentials.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <returns></returns>
        bool Authenticate(GatewayCredentials credentials);


        /// <summary>
        /// requests the primary asset id and it's profile type of the specified device.
        /// </summary>
        /// <param name="deviceId">The device identifier (global version).</param>
        /// <param name="profileType">Type of the profile.</param>
        /// <returns>the asset id that corresponds with the device</returns>
        string GetPrimaryAssetFor(GatewayCredentials credentials, string deviceId, out string profileType);
    }
}
