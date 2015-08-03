
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

        GrovePi.Sensors.IButtonSensor btn;
        bool sensorPrev = false;
        DispatcherTimer timer;
        static Device _device;

        const int doorBellPin = 2;

        public MainPage()
        {
            this.InitializeComponent();

            InitGPIO();
            Init();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(300);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            try
            {
                bool isPressed = btn.CurrentState == GrovePi.Sensors.SensorStatus.On;
                if (sensorPrev != isPressed)
                {
                    _device.Send(doorBellPin, isPressed.ToString().ToLower());        //important: cast to lower so the cloud can interprete the data correclty.If we don't do this, the value will not be stored in the cloud.
                    sensorPrev = isPressed;
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
            btn = DeviceFactory.Build.ButtonSensor(Pin.DigitalPin2);
            if (btn == null)
                throw new Exception("Failed to intialize button.");
        }
    }
}
