using att.iot.client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demoApp
{
    class Program
    {
        static IServer _server;
        static GatewayCredentials credentials = new GatewayCredentials() { ClientKey = "your client key", ClientId = "your client id" };
        static string _deviceId;
        static MyLogger _logger;


        static void Main(string[] args)
        {
            Init();
            bool success;
            if (string.IsNullOrEmpty(_deviceId) == true)
            {
                _deviceId = _server.CreateDevice(credentials, "C# test device", "a device created from my test script");
                success = !string.IsNullOrEmpty(_deviceId);
            }
            else
                success = _server.UpdateDevice(credentials, _deviceId, "C# test device", "a device created from my test script");
            if (success)
            {
                _server.UpdateAsset(credentials, _deviceId, 1, "test actuator", "a test actuator", true, "bool");
                _server.UpdateAsset(credentials, _deviceId, 2, "test sensor", "a test sensor", false, "bool");

                _server.SubscribeToTopics(credentials, _deviceId);

                TopicPath path = new TopicPath() { ClientId = credentials.ClientId, DeviceId = _deviceId, AssetIdStr = "2" };

                Console.ReadKey();                                          //wait to continue so that we can send a value from the cloud to the app.

                _server.AssetValue(path, "true");
            }
        }

        private static void Init()
        {
            _logger = new MyLogger();
            _server = new Server(_logger);
            _server.Init(ConfigurationManager.AppSettings);
            _server.ActuatorValue += _server_ActuatorValue;
        }

        private static void _server_ActuatorValue(object sender, ActuatorData e)
        {
            if (e.Path.AssetId[0] == 1)
                _logger.Trace("incomming value found: {0}", e.AsBool(0));
        }
    }
}
