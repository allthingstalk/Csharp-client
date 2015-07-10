using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace att.iot.client
{
    public class AssetManagementCommandData
    {
        public int Asset { get; set; }

        /// <summary>
        /// Gets or sets the command name.
        /// </summary>
        /// <value>
        /// The command.
        /// </value>
        public string Command { get; set; }
    }
}
