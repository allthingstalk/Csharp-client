nuget pack att.iot.client.nuspec -IncludeReferencedProjects -Prop Configuration=Release -Verbosity detailed
rem nuget push att.iot.client.nupkg -source c:\nuget_test_rep