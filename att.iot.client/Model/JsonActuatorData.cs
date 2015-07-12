using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace att.iot.client
{
    /// <summary>
    /// contains the data that we found when an actuator value was send from the cloud to a device in Json format.
    /// </summary>
    public class JsonActuatorData : ActuatorData
    {
        JObject _value;
        List<object> _index;

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="value">The raw value.</param>
        public override void Load(string value)
        {
            _value = JObject.Parse(value);
        }

        public void SetMap(List<object> values)
        {
            _index = values;
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
                return _value.Count;
            }
        }

        public override double AsDouble(int index)
        {
            string name = _index[index] as string;
            if (name != null)
                return _value[name].Value<double>();
            else
                throw new IndexOutOfRangeException(string.Format("index pos {0} does not contain a double", index));
        }

        public override double AsDouble(int[] index)
        {
            throw new NotImplementedException();
        }

        public override bool AsBool(int index)
        {
            string name = _index[index] as string;
            if (name != null)
                return _value[name].Value<bool>();
            else
                throw new IndexOutOfRangeException(string.Format("index pos {0} does not contain a bool", index));
        }

        public override bool AsBool(int[] index)
        {
            throw new NotImplementedException();
        }

        public override int AsInt(int index)
        {
            string name = _index[index] as string;
            if (name != null)
                return _value[name].Value<int>();
            else
                throw new IndexOutOfRangeException(string.Format("index pos {0} does not contain a int", index));
        }

        public override int AsInt(int[] index)
        {
            throw new NotImplementedException();
        }

        public override DateTime AsDateTime(int index)
        {
            string name = _index[index] as string;
            if (name != null)
                return _value[name].Value<DateTime>();
            else
                throw new IndexOutOfRangeException(string.Format("index pos {0} does not contain a DateTime", index));
        }

        public override DateTime AsDateTime(int[] index)
        {
            throw new NotImplementedException();
        }

        public override TimeSpan AsTimeSpan(int index)
        {
            string name = _index[index] as string;
            if (name != null)
                return _value[name].Value<TimeSpan>();
            else
                throw new IndexOutOfRangeException(string.Format("index pos {0} does not contain a TimeSpan", index));
        }

        public override TimeSpan AsTimeSpan(int[] index)
        {
            throw new NotImplementedException();
        }

        public override string AsString(int index)
        {
            string name = _index[index] as string;
            if (name != null)
                return _value[name].Value<string>();
            else
                throw new IndexOutOfRangeException(string.Format("index pos {0} does not contain a string", index));
        }

        public override string AsString(int[] index)
        {
            throw new NotImplementedException();
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
