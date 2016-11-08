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

using att.iot.client;
using GrovePi;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace lightsensor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string deviceId = "your device id";
        const string clientId = "your client id";
        const string clientKey = "your client key";

        GrovePi.Sensors.ILightSensor _sensor;
        DispatcherTimer _timer;
        static Device _device;

        const int sensorPin = 0;

        public MainPage()
        {
            this.InitializeComponent();

            InitGPIO();
            Init();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(300);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            try
            {
                int value = _sensor.SensorValue();
                _device.Send(sensorPin, value.ToString());
            }
            catch (Exception ex)
            {
                //The grovePi lib can still occationally give an unexpectd error, just continue
            }
        }

        private void Init()
        {
            _device = new Device(clientId, clientKey);
            _device.DeviceId = deviceId;

            _device.UpdateAsset(sensorPin, "lichtSensor", "Licht Sensor", true, "integer");
        }

        private void InitGPIO()
        {
            _sensor = DeviceFactory.Build.LightSensor(Pin.AnalogPin0);
            if (_sensor == null)
                throw new Exception("Failed to intialize button.");
        }
    }
}
