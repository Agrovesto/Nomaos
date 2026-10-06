using Agrovesto.Backend.API.Hosting;

var app = NomaosMicroserviceHost.Build(args, "monitoring-evaluation", [
    "Agrovesto.Backend.API.Controllers.V2.Public.MandEController"
]);
app.Run();
