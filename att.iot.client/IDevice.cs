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
        /// Occurs when the mqtt connection was reset and recreated. The library will automatically set up the connection again.
        /// Use this event to get notified about the action.
        /// </summary>
        event EventHandler ConnectionReset;    

        /// <summary>
        /// Gets or sets the id of the device related to this object.
        /// Create the device on the cloud and assign the generated id to this property.
        /// </summary>
        string DeviceId { get; set; }

        /// <summary>
        /// Updates or creates the asset.
        /// </summary>
        /// <param name="asset">The id of the asset (local to your device). Should be a number or string</param>
        /// <param name="content">The content of the asset as a JObject.</param>
        /// <param name="extraHeaders">any optional extra headers that should be included in the message.</param>
        void UpdateAsset(object asset, JObject content, Dictionary<string, string> extraHeaders = null);

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
        bool UpdateAsset(object assetId, string name, string description, bool isActuator, string type, AssetStyle style = AssetStyle.Undefined);


        /// <summary>
        /// sends the asset value to the server.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="value">The value, either a string witha single value or a json object with multiple values.</param>
        void Send(object asset, object value);


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
        void SendAssetValueHTTP(object asset, object value);

        /// <summary>
        /// gets the last stored value of the specified asset.
        /// </summary>
        /// <param name="asset">the id (local to this device) of the asset for which to return the last recorded value.</param>
        /// <returns>the value as a json structure.</returns>
        JToken GetAssetState(object asset);

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
