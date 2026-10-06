using Agrovesto.Backend.API.Hosting;

var app = NomaosMicroserviceHost.Build(args, "warehouse", [
    "Agrovesto.Backend.API.Controllers.WarehouseManagementController"
]);
app.Run();
