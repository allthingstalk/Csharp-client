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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace motionDetector
{
    /// <summary>
    /// MOTION sensor not yet supported on .net
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string deviceId = "your device id";
        const string clientId = "your client id";
        const string clientKey = "your client key";

        const int _pin = 2;
        GrovePi.Sensors.IButtonSensor _btn;
        bool _sensorPrev = false;
        DispatcherTimer _timer;
        static Device _device;

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
                bool isPressed = _btn.CurrentState == GrovePi.Sensors.SensorStatus.On;
                if (_sensorPrev != isPressed)
                {
                    _device.Send(_pin, isPressed.ToString().ToLower());        //important: cast to lower so the cloud can interprete the data correclty.If we don't do this, the value will not be stored in the cloud.
                    _sensorPrev = isPressed;
                }
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

            _device.UpdateAsset(_pin, "Doorbell", "doorbell", false, "boolean");
        }

        private void InitGPIO()
        {
            _btn = DeviceFactory.Build.ButtonSensor(Pin.DigitalPin2);
            if (_btn == null)
                throw new Exception("Failed to intialize button.");
        }
    }
}
