#att.iot.client


##iot.client.IDevice
            
Manages a single IOT device.
        
###Properties

####DeviceId
Gets or sets the id of the device related to this object. Create the device on the cloud and assign the generated id to this property.
###Methods


####UpdateAsset(System.Object,Newtonsoft.Json.Linq.JObject,System.Collections.Generic.Dictionary{System.String,System.String})
Updates or creates the asset.
> #####Parameters
> **asset:** The id of the asset (local to your device). Should be a number or string

> **content:** The content of the asset as a JObject.

> **extraHeaders:** any optional extra headers that should be included in the message.


####UpdateAsset(System.Object,System.String,System.String,System.Boolean,System.String,att.iot.client.AssetStyle)
Simple way to create or update an asset.
> #####Parameters
> **assetId:** The asset identifier (local).

> **name:** The name of the asset.

> **description:** The description.

> **isActuator:** if set to true an actuator should be created, otherwise a sensor.

> **type:** The data type of the asset (string, int, float, bool, DateTime, TimeSpan) or the full json profile.

> #####Return value
> True if successful, otherwise false

####Send(System.Object,System.Object)
sends the asset value to the server.
> #####Parameters
> **asset:** The asset.

> **value:** The value, either a string witha single value or a json object with multiple values.


####GetPrimaryAsset
requests the primary asset id and it's profile type of the device.
> #####Return value
> the asset definition

####SendAssetValueHTTP(System.Object,System.Object)
sends the asset value to the server.
> #####Parameters
> **asset:** The asset id (local to this device).

> **value:** The value, either a string with a single value or a json object with multiple values.


####GetAssetState(System.Object)
gets the last stored value of the specified asset.
> #####Parameters
> **asset:** the id (local to this device) of the asset for which to return the last recorded value.

> #####Return value
> the value as a json structure.

####GetAssets
gets all the assets that the cloud knows for this device.
> #####Return value
> a json object (array) containing all the asset definitions

####SendCommandTo(System.Object,System.Object)
Use this function to command another device. You can only send commands to devices that you own, which are in the same account as this device.
sends a command to an asset on another device.
> #####Parameters
> **asset:** The full id of the asset to send a command to

> **value:** The value to send to the command


##iot.client.ILogger
            
Implement this interface so that the class can log trace, info, warning and error messages to the desired output.
        
###Methods


####Trace(System.String,System.Object[])
Writes a diagnostic message at the trace level to the desired output using the specified arguments.
> #####Parameters
> **value:** The message to log.

> **args:** any arguments to replace in the message.


####Info(System.String,System.Object[])
Writes a diagnostic message at the infor level to the desired output using the specified arguments.
> #####Parameters
> **value:** The message to log.

> **args:** any arguments to replace in the message.


####Warn(System.String,System.Object[])
Writes a diagnostic message at the warning level to the desired output using the specified arguments.
> #####Parameters
> **value:** The message to log.

> **args:** any arguments to replace in the message.


####Error(System.String,System.Object[])
Writes a diagnostic message at the error level to the desired output using the specified arguments.
> #####Parameters
> **value:** The message to log.

> **args:** any arguments to replace in the message.


##iot.client.ActuatorData
            
contains the data that we found when an actuator value was send from the cloud to a device.
        
###Methods


####Load(System.String)
Loads the data.
> #####Parameters
> **value:** The raw value.


####ToString
Returns a that represents this instance.
> #####Return value
> A that represents this instance.

##iot.client.TopicPath
            
contains the data that defines a management command.
        
###Properties

####Gateway
Gets or sets the gateway to issue the command to. The gateway.
####ClientId
Gets or sets the client identifier. The client identifier is usually the account name. It is used to identify the api call in the cloudapp.
####DeviceId
Gets or sets the (local) device identifier to issue the command to (if any). The device identifier. This can be null when the topicPath points to a gateway-asset.
####AssetId
Gets or sets the (local) asset identifier to issue the command to (if any). The asset identifier is an array if integers which form a path into the command class structure. To get this value as a formatted string, use The asset identifier.
####Mode
Determins the mode of the topic path: - state: the value of an asset - command: the new value for an actuator - event: device/asset removed or added. true if this instance is setter; otherwise, false.
####Direction
determines the direction of the message: - out: from device to cloud - in: from cloud to device
###Methods


####Constructor
Initializes a new instance of the class. Use this constructor when the object is created to manually define a path.

####Constructor
Initializes a new instance of the class. Use this constructor when the topicPath is created for an incomming value.
> #####Parameters
> **path:** The path as supplied by the pub-sub client (the topic).


####Constructor
performs a deep copy
> #####Parameters
> **source:** The source.


####ToString
Returns a that represents this instance.
> #####Return value
> A that represents this instance.