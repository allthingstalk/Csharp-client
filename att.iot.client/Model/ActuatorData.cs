using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace att.iot.client
{
    /// <summary>
    /// contains the data that we found when an actuator value was send from the cloud to a device.
    /// </summary>
    public class ActuatorData
    {
        public string Asset { get; set; }

        JToken _value;

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="value">The raw value.</param>
        public void Load(string value)
        {
            try {
                _value = JToken.Parse(value);
            }
            catch 
            {
                value = "\"" + value + "\"";                //compensate for strings: they are sent without "" (for arduino, low bandwith, but strict json requires quotes
                _value = JToken.Parse(value);
            }
        }

        public JToken Value
        {
            get
            {
                return _value;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _value.ToString();
        }

    }
}
