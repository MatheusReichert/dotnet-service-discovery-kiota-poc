#:sdk Aspire.AppHost.Sdk@13.1.2
#:project ServiceA/ServiceA.csproj
#:project ServiceB/ServiceB.csproj
#:project ServiceC/ServiceC.csproj

var builder = DistributedApplication.CreateBuilder(args);

var serviceA = builder.AddProject<Projects.ServiceA>("servicea");
var serviceB = builder.AddProject<Projects.ServiceB>("serviceb");
var serviceC = builder.AddProject<Projects.ServiceC>("servicec");

builder.Build().Run();
