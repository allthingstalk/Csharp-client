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

/*Note: currently, the buzzer is not yet supported on win10 devices. We have replaced the buzzer with a led for this experiment.
*/

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace smartphoneGuard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string deviceId = "";
        const string clientId = "your client id";
        const string clientKey = "your client key";

        GrovePi.Sensors.ILed _buzzer;
        const int _pin = 2;
        static Device _device;

        public MainPage()
        {
            this.InitializeComponent();
            InitGPIO();
            Init();
        }

        private void Init()
        {
            _device = new Device(clientId, clientKey);
            _device.DeviceId = deviceId;
            _device.ActuatorValue += _server_ActuatorValue;

            _device.UpdateAsset(_pin, "vMotor", "vibration motor", true, "boolean");
        }

        private void _server_ActuatorValue(object sender, ActuatorData e)
        {
            if (e.Asset == _pin)
            {
                StringActuatorData data = (StringActuatorData)e;
                if (data.AsBool() == true)
                {
                    _buzzer.ChangeState(GrovePi.Sensors.SensorStatus.On);
                    _device.Send(_pin, "true");             //feedback for led
                }
                else
                {
                    _buzzer.ChangeState(GrovePi.Sensors.SensorStatus.Off);
                    _device.Send(_pin, "false");
                }
            }
        }

        private void InitGPIO()
        {
            _buzzer = DeviceFactory.Build.Led(Pin.DigitalPin2);
            if (_buzzer == null)
                throw new Exception("Failed to intialize v motor - buzzer.");
        }
    }
}
