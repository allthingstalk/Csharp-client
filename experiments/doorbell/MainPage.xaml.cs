
using att.iot.client;
using GrovePi;
using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace doorbell
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string deviceId = "your device id";
        const string clientId = "your client id";
        const string clientKey = "your client key";

        GrovePi.Sensors.IButtonSensor _btn;
        bool _sensorPrev = false;
        DispatcherTimer _timer;
        static Device _device;

        const int doorBellPin = 2;

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
                    _device.Send(doorBellPin, isPressed.ToString().ToLower());        //important: cast to lower so the cloud can interprete the data correclty.If we don't do this, the value will not be stored in the cloud.
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

            _device.UpdateAsset(doorBellPin, "Doorbell", "doorbell", true, "boolean");
        }

        private void InitGPIO()
        {
            _btn = DeviceFactory.Build.ButtonSensor(Pin.DigitalPin2);
            if (_btn == null)
                throw new Exception("Failed to intialize button.");
        }
    }
}
