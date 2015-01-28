# Csharp-client

## Description

C# Client library for connecting internet of things applications to the AllThingsTalk platform

## features
supports: 

- creation, 2 factor identification, authentification & updating of gateways
- creating, updating & deleting devices
- creating & updating assets
- gateway & device  assets
- sending and receiving asset values, supported formats:
	- json 
	- csv
- uses events for incomming asset values & commands
- reporting user information (errors & info)

Depends on:

- [Newtonsoft.json](https://www.nuget.org/packages/Newtonsoft.Json/) for working with Json data.
- [Lumenworkds.framework.io](https://www.nuget.org/packages/LumenWorks.Framework.IO/) for properly reading csv formatted data.
- [nlog](http://nlog-project.org/) for advanced logging
- [m2mqtt](https://m2mqtt.codeplex.com/) mqtt library for pub-sub communication.

## installation
There's a nuget package available at: [https://www.nuget.org/packages/att.iot.client/](https://www.nuget.org/packages/att.iot.client/) for easy installation.

## usage
### create gateway
    IPEndPoint address = GetAddress();		//get the ip address of the gateway
    var cloud = new Server();
    byte[] address = address.Address.GetAddressBytes();
    cloud.CreateGateway(uid, address, CreateGateway(uid)); 	//typedefBuilder 

     ...

    public static JObject CreateGateway(string uid, bool includeName = true)
    {
        JObject data = new JObject();
        data["uid"] = uid;
        if (includeName == true)                                                    //this allows the user to change the name of the gateway.
            data["name"] = "default gateway name";
        var profile = new JObject();
        profile["ControllerId"] = 0;                                                //profide default value for controllerId, so we can update it later on. If we don't do this, the api wont create the profile object in the db, and our update function fails.
        data["profile"] = profile;                                                  //make certain that there is a profile object.
        return data;
    }
After creating the gateway, it will be registered as an orphan. The user has to claim it by using the uid that was specified during creation. Once the user has claimed the gateway, the application has to finish the claim procedure by calling `FinishClaim`. During this call, the definition of the gateway will be refreshed so you can add additional details that should not yet have been available as long as it was an orphan.  
This procedure allows for 2-factor identification. For instance, `FinishClaim` could be called when the user presses a button on the gateway.

### get gateway details
you can query for all the details of a gateway, including the id, clientId and clientKey which are required in most api calls. These values should be stored in a `GatewayCredentials` object.

    GatewayCredentials credentials = new GatewayCredentials(){ UId = "my UID"};
    JObject result = cloud.GetGateway(credentials);

### receiving asset values from the cloud
before you can receive any values from the cloud, you need to subscribe to the correct topics.

    cloud.SubscribeToTopics(credentials);

asset values are passed to your application through an event

    server.ActuatorValue += _server_ActuatorValue;
    ...

    void _server_ActuatorValue(object sender, ActuatorData e)
    {
	    switch (e.Path.AssetId[0])
	    {
	        case ZiprConstants.COMMAND_CLASS_ATT_DISCOVERYSTATE:
	            ChangeDiscoveryState(e);
	            break;
	        case ZiprConstants.COMMAND_CLASS_ATT_RESET:
	            ResetTask(e.Path.Gateway);
	            break;
	        case ZiprConstants.COMMAND_CLASS_ATT_REFRESH:
	            StartRefresh(e.Path);
	            break;
	        default:
	            int val = e.AsInt(0);
	            break;
	    }
    }

The `ActuatorData` object contains all the details about the incomming event. The topic on which the data arrived, is stored in the `Path` property which is split up into client, gateway, device and assetId. The assetId in turn can contain multiple components (a split is done on the '_' character). This allows you to compose meaningful asset ids that can contain extra information like version number.  
You can retrieve values from the object by calling the appropriate 'AsXXX' functions. These will convert the data in the requested format. By using these functions, you don't have to worry about the data format in which the data arrived (json or csv).

### sending asset values

    TopicPath topic = new TopicPath();
    topic.Gateway = credentials.GatewayId;		//topicPath has been created to work with application that support multiple gateways
    topic.ClientId = credentials.ClientId;
    topic.DeviceId = NodeId.ToString();
    topic.AssetId = new int[] { ZiprConstants.COMMAND_CLASS_ALARM, 1 };
    _server.AssetValue(topic, "1");
    
    JObject content = JObject.Parse("{'value': 1}");
    _server.AssetValue(topic, content);

The library has been created so that it can potentially work with multiple gateways, which is why you always have to supply the gatewayId in the TopicPath.  
Asset values can be sent as json objects or as string values.