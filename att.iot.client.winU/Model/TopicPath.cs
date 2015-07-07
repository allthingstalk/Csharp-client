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
        const string MANAGEMENTCHANNEL = "m";       //from command
        const string FEEDCHANNEL = "f";
        const string SETTERCHANNEL = "s";
        const string GATEWAYENTITY = "gateway";
        const string DEVICEENTITY = "device";
        const string ASSETENTITY = "asset";
        const string THINGENTITY = "t";
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
                if (path[3] == GATEWAYENTITY)
                {
                    Gateway = path[4];

                    if (path.Length == 10)
                    {
                        string[] parts = path[6].Split('_');
                        DeviceId = parts[parts.Length - 1];
                        parts = path[8].Split('_');
                        AssetId = GetAssetId(parts, 2);
                        IsSetter = path[9] == MANAGEMENTCHANNEL;
                    }
                    else if (path.Length == 8)
                    {
                        string[] parts = path[6].Split('_');
                        if (path[5] == DEVICEENTITY)
                            DeviceId = parts[parts.Length - 1];
                        else
                            AssetId = GetAssetId(parts, 1);
                        IsSetter = path[7] == MANAGEMENTCHANNEL;
                    }
                    else if (path.Length == 6)
                        IsSetter = true;
                }
                else if (path[3] == DEVICEENTITY)
                {
                    string[] parts = path[4].Split('_');
                    DeviceId = parts[parts.Length - 1];
                    if (path.Length == 6)
                        IsSetter = path[5] == MANAGEMENTCHANNEL;
                    else
                    {
                        parts = path[6].Split('_');
                        AssetId = GetAssetId(parts, 1);
                        IsSetter = path[7] == MANAGEMENTCHANNEL;
                    }
                }
                else
                    throw new NotSupportedException("topic structure invalid, pos 2 should be 'gateway'");
                
            }
            else
                throw new NotSupportedException("topic structure invalid, pos 0 should be 'client'");
                
        }

        private int[] GetAssetId(string[] parts, int offset)
        {
            int[] res = new int[parts.Length - offset];
            for (int i = offset; i < parts.Length; i++)                                                          //the first item is the id of the gateway, followed by the id of the device, whic
            {
                int val;
                if (int.TryParse(parts[i], out val) == true)
                    res[i - offset] = val;
                else
                    throw new InvalidOperationException(string.Format("failed to convert asset id to int[], problem with: {0} in {1}", parts[i], string.Join("_", parts)));
            }
            return res;
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
            this.IsSetter = source.IsSetter;
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
        /// returns the device id, as formatted for the cloud app.
        /// If there is a gateway known, the gateway id will be prepended to the device id, otherwise, only the device id is used.
        /// </summary>
        /// <value>
        /// The remote device identifier.
        /// </value>
        public string RemoteDeviceId
        {
            get 
            {
                if (string.IsNullOrEmpty(Gateway) == false)
                    return string.Format("{0}_{1}", Gateway, DeviceId);
                else
                    return DeviceId;
            }
            set
            {
                string[] temp = value.Split('_');
                if (temp.Length == 2)
                {
                    Gateway = temp[0];
                    DeviceId = temp[1];
                }
                else
                    throw new FormatException("not a device id");
            }
        }

        /// <summary>
        /// gets/sets the full id as used by the cloud.
        /// Format: {GatewayId}_{DeviceId}_{AssetId}
        /// </summary>
        public string RemoteId
        {
            get
            {
                return this.ToString();
            }
            set
            {
                string[] temp = value.Split('_');
                if (temp.Length > 2)
                {
                    Gateway = temp[0];
                    DeviceId = temp[1];
                    StoreAssetId(temp, 2);
                }
                else
                    throw new FormatException("not a full remote id");
            }
        }

        /// <summary>
        /// Gets the device identifier as a byte value.
        /// </summary>
        public byte DeviceIdAsNr
        {
            get
            {
                int res = int.Parse(DeviceId);
                return (byte)res;
            }
        }

        /// <summary>
        /// Gets or sets the (local) asset identifier  to issue the command to (if any).
        /// The asset identifier is an array if integers which form a path into the command class structure.
        /// To get this value as a formatted string, use <see cref="TopicPath.AssetIdStr"/>
        /// </summary>
        /// <value>
        /// The asset identifier.
        /// </value>
        public int[] AssetId { get; set; }

        /// <summary>
        /// Gets or sets the (local) asset identifier string.
        /// Warning: the setter expects a deviceId in front (not stored)
        /// </summary>
        /// <value>
        /// The asset identifier string.
        /// </value>
        public string AssetIdStr
        {
            get
            {
                return TopicPath.BuildAssetStr(AssetId);
            }
            set
            {
                if (string.IsNullOrEmpty(value) == false)
                {
                    string[] parts = value.Split('_');
                    if (parts.Length == 1)                                          //if it's a very simple asset id with no subparts, then don't try to split it up
                        StoreAssetId(parts, 0);
                    else
                        StoreAssetId(parts, 1);                                     //we need to remove the device part from it?
                }
                else
                    AssetId = null;
            }
        }

        /// <summary>
        /// Stores the asset identifier. 
        /// </summary>
        /// <param name="parts">The parts.</param>
        /// <param name="offset">The offset into the list where the asset id starts.</param>
        private void StoreAssetId(string[] parts, int offset)
        {
            AssetId = new int[parts.Length - offset];
            for (int i = offset; i < parts.Length; i++)
            {
                int val;
                if (int.TryParse(parts[i], out val) == true)
                    AssetId[i - offset] = val;
                else
                    throw new InvalidOperationException(string.Format("can't convert string to asset id: {0}", string.Join("_", parts)));
            }
        }

        /// <summary>
        /// Builds an asset id string from the specified assetpath.
        /// </summary>
        /// <param name="assetpath">The asset identifier.</param>
        /// <returns></returns>
        public static string BuildAssetStr(int[] assetpath)
        {
            if (assetpath != null)
            {
                StringBuilder res = new StringBuilder();
                if (assetpath.Length > 0)
                {
                    res.Append(assetpath[0]);
                    for (int i = 1; i < assetpath.Length; i++)
                    {
                        res.Append("_");
                        res.Append(assetpath[i]);
                    }
                }
                return res.ToString();
            }
            return null;
        }

        /// <summary>
        /// Gets/sets the asset identifier as formatted for the cloud app.
        /// </summary>
        /// <value>
        /// The remote asset identifier.
        /// </value>
        public string RemoteAssetId
        {
            get { return string.Format("{0}_{1}", RemoteDeviceId, AssetIdStr); }
            set
            {
                if (string.IsNullOrEmpty(value) == false)
                {
                    string[] parts = value.Split('_');
                    if (parts.Length > 2)
                    {
                        Gateway = parts[0];
                        DeviceId = parts[1];
                        AssetId = new int[parts.Length - 2];
                        for (int i = 2; i < parts.Length; i++)
                        {
                            int val;
                            if (int.TryParse(parts[i], out val) == true)
                                AssetId[i - 2] = val;
                            else
                                throw new InvalidOperationException(string.Format("can't convert string to asset id: {0}", value));
                        }
                    }
                }
                else
                {
                    AssetId = null;
                    DeviceId = null;
                    Gateway = null;
                }
            }
        }

        /// <summary>
        /// when true, it's a data feed from cloud to client.  When false, and the sensorId is declared, then it's a command
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is setter; otherwise, <c>false</c>.
        /// </value>
        public bool IsSetter { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string assetId;
            if (string.IsNullOrEmpty(DeviceId) == false)
            {
                if (string.IsNullOrEmpty(Gateway) == false)
                    assetId = string.Format("{0}_{1}_{2}", Gateway, DeviceId, AssetIdStr);
                else
                    assetId = string.Format("{0}_{1}", DeviceId, AssetIdStr);
            }
            else
                assetId = string.Format("{0}_{1}", Gateway, AssetIdStr);                                            //for assets attached to the gateway
            return string.Format("client/{0}/out/asset/{1}/state", ClientId, assetId);
        }
    }
}
