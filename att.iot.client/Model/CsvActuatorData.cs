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
    public class CsvActuatorData : ActuatorData
    {
        string[] _values;

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="value">The raw value.</param>
        public override void Load(string value)
        {
            TextReader text = new StringReader(value);
            CsvReader csv = new CsvReader(text, false, '|');
            if (csv.ReadNextRecord() == true)
            {
                _values = new string[csv.FieldCount];
                for (int i = 0; i < csv.FieldCount; i++)
                    _values[i] = csv[i];
            }
        }

        /// <summary>
        /// Gets the length of the data.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public override int Length
        {
            get
            {
                return _values.Length;
            }
        }

        public override double AsDouble(int index)
        {
            double val;
            if (double.TryParse(_values[index], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected double value at position {0}", index));
        }

        public override double AsDouble(int[] index)
        {
            double val;
            if (double.TryParse(_values[index.Sum()], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected double value at position {0}", index.Sum()));
        }

        public override bool AsBool(int index)
        {
            bool val;
            if (bool.TryParse(_values[index], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected bool value at position {0}", index));
        }

        public override bool AsBool(int[] index)
        {
            bool val;
            if (bool.TryParse(_values[index.Sum()], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected bool value at position {0}", index.Sum()));
        }

        public override int AsInt(int index)
        {
            int val;
            if (int.TryParse(_values[index], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected int value at position {0}", index));
        }

        public override int AsInt(int[] index)
        {
            int val;
            if (int.TryParse(_values[index.Sum()], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected int value at position {0}", index.Sum()));
        }

        public override DateTime AsDateTime(int index)
        {
            DateTime val;
            if (DateTime.TryParse(_values[index], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected DateTime value at position {0}", index));
        }

        public override DateTime AsDateTime(int[] index)
        {
            DateTime val;
            if (DateTime.TryParse(_values[index.Sum()], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected DateTime value at position {0}", index.Sum()));
        }

        public override TimeSpan AsTimeSpan(int index)
        {
            TimeSpan val;
            if (TimeSpan.TryParse(_values[index], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected TimeSpan value at position {0}", index));
        }

        public override TimeSpan AsTimeSpan(int[] index)
        {
            TimeSpan val;
            if (TimeSpan.TryParse(_values[index.Sum()], out val) == true)
                return val;
            throw new InvalidCastException(string.Format("Expected TimeSpan value at position {0}", index.Sum()));
        }

        public override string AsString(int index)
        {
            return _values[index];
        }

        public override string AsString(int[] index)
        {
            return _values[index.Sum()];
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Join(", ", _values);
        }
    }
}
