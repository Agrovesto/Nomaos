using Agrovesto.Backend.API.Hosting;

var app = NomaosMicroserviceHost.Build(args, "input-financing", [
    "Agrovesto.Backend.API.Controllers.V2.Internal.InputFinancingController"
]);
app.Run();
