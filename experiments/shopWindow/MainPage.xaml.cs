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

        GrovePi.Sensors.ILed redLed;
        const int ledPin = 4;
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

            _device.UpdateAsset(ledPin, "Shop Light", "Shop Window Light", true, "boolean");
        }

        private void _server_ActuatorValue(object sender, ActuatorData e)
        {
            if (e.Asset == ledPin)
            {
                StringActuatorData data = (StringActuatorData)e;
                if (data.AsBool() == true)
                {
                    //the next line would be used to update any UI -> has to be done on UI thread
                    //Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { RedLed.Fill = redBrush; redLed.ChangeState(GrovePi.Sensors.SensorStatus.On); });
                    redLed.ChangeState(GrovePi.Sensors.SensorStatus.On);
                    _device.Send(ledPin, "true");             //feedback for led
                }
                else
                {
                    //the next line would be used to update any UI -> has to be done on UI thread
                    //Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { RedLed.Fill = grayBrush; redLed.ChangeState(GrovePi.Sensors.SensorStatus.Off); });
                    redLed.ChangeState(GrovePi.Sensors.SensorStatus.Off);
                    _device.Send(ledPin, "false");
                }
            }
        }

        private void InitGPIO()
        {
            redLed = DeviceFactory.Build.Led(Pin.DigitalPin3);
            if (redLed == null)
                throw new Exception("Failed to intialize led.");
        }
    }
}
