using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace att.iot.client
{
    /// <summary>
    /// contains the credentials for a single zipr. These can be stored on disk
    /// </summary>
    public class GatewayCredentials 
    {
        /// <summary>
        /// Gets or sets the zipr identifier (as provided by the zipr)
        /// </summary>
        /// <value>
        /// The zipr identifier.
        /// </value>
        public string UId { get; set; }

        /// <summary>
        /// Gets or sets the gateway or client identifier.
        /// </summary>
        /// <value>
        /// The gateway identifier.
        /// </value>
        public string GatewayId { get; set; }

        /// <summary>
        /// Gets or sets the client key that should be used for web communications.
        /// </summary>
        /// <value>
        /// The client key.
        /// </value>
        public string ClientKey { get; set; }

        /// <summary>
        /// Gets or sets the client key that should be used for web communications.
        /// </summary>
        /// <value>
        /// The client id that should be used (for mqtt stuff).
        /// </value>
        public string ClientId { get; set; }

    }
}
