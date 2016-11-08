/*
   Copyright 2014-2016 AllThingsTalk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*/

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
