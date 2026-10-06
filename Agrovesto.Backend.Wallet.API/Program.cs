using Agrovesto.Backend.API.Hosting;

var app = NomaosMicroserviceHost.Build(args, "wallet", [
    "Agrovesto.Backend.API.Controllers.V2.BankController",
    "Agrovesto.Backend.API.Controllers.V2.Internal.UserWalletController"
]);
app.Run();
