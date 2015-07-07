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
    abstract public class ActuatorData
    {
        public abstract double AsDouble(int index);

        public abstract double AsDouble(int[] index);

        public abstract bool AsBool(int index);

        public abstract bool AsBool(int[] index);

        public abstract int AsInt(int index);

        public abstract int AsInt(int[] index);

        public abstract DateTime AsDateTime(int index);

        public abstract DateTime AsDateTime(int[] index);

        public abstract TimeSpan AsTimeSpan(int index);

        public abstract TimeSpan AsTimeSpan(int[] index);

        public abstract string AsString(int index);

        public abstract string AsString(int[] index);

        public TopicPath Path { get; set; }


        /// <summary>
        /// Gets the length of the data.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public abstract int Length { get; }

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="value">The raw value.</param>
        public abstract void Load(string value);

    }
}
