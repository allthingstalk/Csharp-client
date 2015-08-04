using att.iot.client;
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

        //GrovePi.Sensors.
        bool sensorPrev = false;
        DispatcherTimer timer;
        static Device _device;

        public MainPage()
        {
            this.InitializeComponent();
        }
    }
}
