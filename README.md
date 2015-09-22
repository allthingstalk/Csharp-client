# Csharp-client

## Description

C# Client library for connecting internet of things applications to the AllThingsTalk platform

## Features
supports: 

- creating, updating & deleting devices
- creating & updating assets
- sending and receiving asset values, supported formats:
	- json 
	- csv
- uses events for incomming asset values & commands

Depends on:

- [Newtonsoft.json](https://www.nuget.org/packages/Newtonsoft.Json/) for working with Json data.
- [m2mqtt](https://m2mqtt.codeplex.com/) mqtt library for pub-sub communication.

## Installation
There's a nuget package available at: [https://www.nuget.org/packages/att.iot.client/](https://www.nuget.org/packages/att.iot.client/) for easy installation.

## Usage

### Full demo app
The following application will create a new device, add 2 assets to it and will wait for an incomming asset command or until you press a key upon which it will send an asset value to the cloud.  
The 'MyLogger' object that is used, is a interface implementation that simply writes the text to the console screen (Console.WriteLn)

    class Program
    {
        static Device _device;
        static MyLogger _logger;

        private static void Init()
        {
            //provide a logger object
            _logger = new MyLogger();
            //create the device object with your account details
            _device = new Device("testjan", "5i4duakv2bq", _logger);
            //if the device was already created, load the id from the settings.
            _device.DeviceId = Properties.Settings.Default["deviceId"].ToString();
            _device.ActuatorValue += _device_ActuatorValue;
        }



        static void Main(string[] args)
        {
            Init();
            bool success;
            //create or update the device in the cloud.
            if (string.IsNullOrEmpty(_device.DeviceId) == true)
                success = _device.CreateDevice("C# test device", "a device created from my test script");
            else
                success = _device.UpdateDevice("C# test device", "a device created from my test script");

            if (success)
            {
                //store the device id in the settings, so we can reuse it later on.
                Properties.Settings.Default["deviceId"] = _device.DeviceId; 
                Properties.Settings.Default.Save();

                //update or create the assets on the device
                _device.UpdateAsset(1, "test actuator", "a test actuator", true, "bool");
                _device.UpdateAsset(2, "test sensor", "a test sensor", false, "bool");

                //wait to continue so that we can send a value from the cloud to the app.
                Console.ReadKey();                                          

                //send a value to the platform
                _device.Send(2, "true");
            }
        }

        static void _device_ActuatorValue(object sender, ActuatorData e)
        {
            _logger.Trace("incomming value found: {0}", e.ToString());

            //check the actuator for which we received a command
            if (e.Asset == 1)
            {
                //actuators can send simple strings or complex json values. 
                StringActuatorData data = (StringActuatorData)e;
                //do something with the value
                if(data.AsBool() == true)
                    _logger.Trace("actuating sensor");
            }
        }
    }
