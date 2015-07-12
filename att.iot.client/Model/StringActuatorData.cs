using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LumenWorks.Framework.IO.Csv;
using System.IO;

namespace att.iot.client
{
    /// <summary>
    /// contains the data that we found when an actuator value was send from the cloud to a device in csv format.
    /// </summary>
    public class StringActuatorData : ActuatorData
    {
        string _value;

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="value">The raw value.</param>
        public override void Load(string value)
        {
            _value = value;
        }

        public double AsDouble()
        {
            double val;
            if (double.TryParse(_value, out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected double value at position {0}", index));
        }


        public bool AsBool()
        {
            bool val;
            if (bool.TryParse(_value, out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected bool value at position {0}", index));
        }

        public int AsInt()
        {
            int val;
            if (int.TryParse(_value, out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected int value at position {0}", index));
        }

        public DateTime AsDateTime()
        {
            DateTime val;
            if (DateTime.TryParse(_value, out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected DateTime value at position {0}", index));
        }


        public TimeSpan AsTimeSpan()
        {
            TimeSpan val;
            if (TimeSpan.TryParse(_value, out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected TimeSpan value at position {0}", index));
        }

        public string Value { get { return _value; } }
    }
}
