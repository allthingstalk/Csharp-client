/*
    Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.

    The MIT License(MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using att.iot.client;
using GrovePi;
using System;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Blinky
{
    public sealed partial class MainPage : Page
    {
        GrovePi.Sensors.ILed redLed;
        GrovePi.Sensors.IRotaryAngleSensor knob;
        int prevKnobVal = -1;                                       //only send value when required
        private DispatcherTimer timer;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        static Device _device;
        static MyLogger _logger;


        public MainPage()
        {
            InitializeComponent();

            Init();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            InitGPIO();
            timer.Start();
        }

        private void Init()
        {
            _logger = new MyLogger();
            _device = new Device("your client id", "your client key", _logger);
            _device.DeviceId = "you device id";
            _device.ActuatorValue += _server_ActuatorValue;

            //update or create the assets on the device
            _device.UpdateAsset("1", "Knob", "a rotary knob", false, "{'type': 'integer', 'minimum': 0, 'maximum': 1023}");
            _device.UpdateAsset("3", "push button", "a virtual push button", false, "boolean");
            _device.UpdateAsset("4", "Red led", "a test sensor", true, "boolean");

            //send a value to the platform
            _device.Send(2, "true");
        }

        private void _server_ActuatorValue(object sender, ActuatorData e)
        {
            if (e.Asset == "4")
            {
                if ((bool)e.Value == true)
                {
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { RedLed.Fill = redBrush; redLed.ChangeState(GrovePi.Sensors.SensorStatus.On); });
                    _device.Send(4, "true");             //feedback for led
                }
                else
                {
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { RedLed.Fill = grayBrush; redLed.ChangeState(GrovePi.Sensors.SensorStatus.Off); });
                    _device.Send(4, "false");
                }
            }
        }

        private void InitGPIO()
        {
            redLed = DeviceFactory.Build.Led(Pin.DigitalPin3);
            if (redLed == null)
            {
                GpioStatus.Text = "Failed to intialize red led.";
                return;
            }
            redLed.ChangeState(GrovePi.Sensors.SensorStatus.Off);

            knob = DeviceFactory.Build.RotaryAngleSensor(Pin.AnalogPin0);
            if (knob == null)
            {
                GpioStatus.Text = "Failed to intialize rotary knob.";
                return;
            }

            GpioStatus.Text = "grove shield and all sensors initialized correctly.";

        }


        private void Timer_Tick(object sender, object e)
        {
            try
            {            
                int val = knob.SensorValue();
                if (prevKnobVal != val)
                {
                    KnobSlider.Value = val;
                    _device.Send(1, val.ToString());
                    prevKnobVal = val;
                }
            }
            catch (Exception ex)
            {
                //The grovePi lib can still occationally give an unexpectd error, just continue
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {

            _device.Send(3, ToggleBtn.IsChecked.ToString().ToLower());
        }
    }
}
