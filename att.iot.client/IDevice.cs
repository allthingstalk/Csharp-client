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
    /// Manages a single IOT device.
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// Occurs when a new actuator value arrived.
        /// </summary>
        event EventHandler<ActuatorData> ActuatorValue;


        /// <summary>
        /// Occurs when a management command arrived for an asset.
        /// </summary>
        event EventHandler<AssetManagementCommandData> AssetManagementCommand;

        /// <summary>
        /// Occurs when a management command arrived for a device.
        /// </summary>
        event EventHandler<string> DeviceManagementCommand;

        /// <summary>
        /// Occurs when the mqtt connection was reset and recreated. The library will automatically set up the connection again.
        /// Use this event to get notified about the action.
        /// </summary>
        event EventHandler ConnectionReset;    

        /// <summary>
        /// gets or sets the id of the device related to this object.
        /// When the device is created with <see cref="CreateDevice(string, string, bool)"/>, then this property is filled in automatically.
        /// </summary>
        string DeviceId { get; set; }

        /// <summary>
        /// Updates or creates the device.
        /// Use this method if you want to be in full control on how the device gets created.
        /// </summary>
        /// <param name="content">The device definition as a json object.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        bool UpdateDevice(JObject content, Dictionary<string, string> extraHeaders = null);

        /// <summary>
        /// Updates or creates the device.
        /// Use this method if you want to be in full control on how the device gets created.
        /// </summary>
        /// <param name="content">The device definition as a string (json format).</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        bool UpdateDevice(string content, Dictionary<string, string> extraHeaders = null);

        /// <summary>
        /// Simple way to update a devce. 
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
        /// Simple way to create a devce.
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
        /// <param name="asset">The id of the asset (local to your device).</param>
        /// <param name="content">The content of the asset as a JObject.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        void UpdateAsset(int asset, JObject content, Dictionary<string, string> extraHeaders = null);

        /// <summary>
        /// Simple way to create or update an asset.
        /// </summary>
        /// <param name="assetId">The asset identifier (local).</param>
        /// <param name="name">The name of the asset.</param>
        /// <param name="description">The description.</param>
        /// <param name="isActuator">if set to <c>true</c> an actuator should be created, otherwise a sensor.</param>
        /// <param name="type">The data type of the asset (string, int, float, bool, DateTime, TimeSpan) or the full json profile.</param>
        /// <returns>
        /// True if successful, otherwise false
        /// </returns>
        bool UpdateAsset(int assetId, string name, string description, bool isActuator, string type, AssetStyle style = AssetStyle.Undefined);

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
        /// requests the primary asset id and it's profile type of the device.
        /// </summary>
        /// <returns>
        /// the asset definition
        /// </returns>
        JToken GetPrimaryAsset();

        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="asset">The asset id (local to this device). </param>
        /// <param name="value">The value, either a string with a single value or a json object with multiple values.</param>
        void SendAssetValueHTTP(int asset, object value);

        /// <summary>
        /// gets the last stored value of the specified asset.
        /// </summary>
        /// <param name="asset">the id (local to this device) of the asset for which to return the last recorded value.</param>
        /// <returns>the value as a json structure.</returns>
        JToken GetAssetState(int asset);

        /// <summary>
        /// gets all the assets that the cloud knows for this device.
        /// </summary>
        /// <returns>a json object (array) containing all the asset definitions</returns>
        JToken GetAssets();

        /// <summary>
        /// sends a command to an asset on another device.
        /// </summary>
        /// <remarks>
        /// Use this function to command another device. You can only send commands to devices that you own, which are in the
        /// same account as this device.
        /// </remarks>
        /// <param name="asset">The full id of the asset to send a command to</param>
        /// <param name="value">The value to send to the command</param>
        void SendCommandTo(string asset, object value);

    }
}
