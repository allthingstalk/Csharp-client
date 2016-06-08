using att.iot.client;
using GrovePi;
using System;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace shopWindow
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string deviceId = "";
        const string clientId = "your client id";
        const string clientKey = "your client key";

        GrovePi.Sensors.ILed _led;
        const int _ledPin = 4;
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

            _device.UpdateAsset(_ledPin, "Shop Light", "Shop Window Light", true, "boolean");
        }

        private void _server_ActuatorValue(object sender, ActuatorData e)
        {
            if (e.Asset == _ledPin.ToString())          //asset id from the cloud always arrives as a string (you are free to create with string or int, but it always comes in as string)
            {
                if ((bool)e.Value == true)
                {
                    _led.ChangeState(GrovePi.Sensors.SensorStatus.On);
                    _device.Send(_ledPin, "true");             //feedback for led
                }
                else
                {
                    _led.ChangeState(GrovePi.Sensors.SensorStatus.Off);
                    _device.Send(_ledPin, "false");
                }
            }
        }

        private void InitGPIO()
        {
            _led = DeviceFactory.Build.Led(Pin.DigitalPin4);
            if (_led == null)
                throw new Exception("Failed to intialize led.");
        }
    }
}
