rem we need to move the content to a folder called 'lib'

IF EXIST bin\release\lib GOTO LIBEXISTS
   mkdir bin\release\lib
:LIBEXISTS
copy bin\release\att.iot.client.winU.dll bin\release\lib\*.*
copy bin\release\M2MqttUniversal.dll bin\release\lib\*.*

nuget pack att.iot.client.winU.nuspec -IncludeReferencedProjects -Prop Configuration=Release -Verbosity detailed