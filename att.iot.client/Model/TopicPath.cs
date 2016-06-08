using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace att.iot.client
{
    /// <summary>
    /// contains the data that defines a management command.
    /// </summary>
    public class TopicPath
    {
        #region const
        const string COMMANDCHANNEL = "command";       //from command
        const string EVENTCHANNEL = "event";
        const string STATECHANNEL = "state";
        const string GATEWAYENTITY = "gateway";
        const string DEVICEENTITY = "device";
        const string ASSETENTITY = "asset";
        const string CLIENTENTITY = "client"; 
        #endregion


        #region ctor
        /// <summary>
        /// Initializes a new instance of the <see cref="TopicPath"/> class.
        /// Use this constructor when the object is created to manually define a path.
        /// </summary>
        public TopicPath()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopicPath"/> class.
        /// Use this constructor when the topicPath is created for an incomming value.
        /// </summary>
        /// <param name="path">The path as supplied by the pub-sub client (the topic).</param>
        public TopicPath(string[] path)
        {
            if (path.Length < 5)
                throw new NotSupportedException("topic structure invalid, expecting at least 6 parts");

            if (path[0] == CLIENTENTITY)
            {
                ClientId = path[1];
                Direction = path[2];
                int currentPos = 3;
                if (path[currentPos] == GATEWAYENTITY)
                {
                    currentPos++;
                    Gateway = path[currentPos++];
                }
                if (path[currentPos] == DEVICEENTITY)
                {
                    currentPos++;
                    DeviceId = path[currentPos++];
                }
                if (path[currentPos] == ASSETENTITY)
                {
                    currentPos++;
                    AssetId = path[currentPos++];
                }
                Mode = path[currentPos];
            }
            else
                throw new NotSupportedException("topic structure invalid, pos 0 should be 'client'");
                
        }


        /// <summary>
        /// performs a deep copy
        /// </summary>
        /// <param name="source">The source.</param>
        public TopicPath(TopicPath source)
        {
            this.AssetId = source.AssetId;
            this.DeviceId = source.DeviceId;
            this.Gateway = source.Gateway;
            this.ClientId = source.ClientId;
            this.Mode = source.Mode;
            this.Direction = source.Direction;
        }

        #endregion

        /// <summary>
        /// Gets or sets the gateway to issue the command to.
        /// </summary>
        /// <value>
        /// The gateway.
        /// </value>
        public string Gateway { get; set; }

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>
        /// The client identifier is usually the account name. It is used to identify the api call in the cloudapp.
        /// </value>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the (local) device identifier to issue the command to (if any).
        /// </summary>
        /// <value>
        /// The device identifier. This can be null when the topicPath points to a gateway-asset.
        /// </value>
        public string DeviceId { get; set; }



        /// <summary>
        /// Gets or sets the (local) asset identifier  to issue the command to (if any).
        /// The asset identifier is an array if integers which form a path into the command class structure.
        /// To get this value as a formatted string, use <see cref="TopicPath.AssetIdStr"/>
        /// </summary>
        /// <value>
        /// The asset identifier.
        /// </value>
        public string AssetId { get; set; }


        /// <summary>
        /// Determins the mode of the topic path: 
        /// - state: the value of an asset
        /// - command: the new value for an actuator
        /// - event: device/asset removed or added.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is setter; otherwise, <c>false</c>.
        /// </value>
        public string Mode { get; set; }

        /// <summary>
        /// determines the direction of the message:
        /// - out: from device to cloud
        /// - in: from cloud to device
        /// </summary>
        public string Direction { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("client/{0}/{1}/device/{2}/asset/{3}/{4}", ClientId, Direction, DeviceId, AssetId, Mode);
        }
    }
}
