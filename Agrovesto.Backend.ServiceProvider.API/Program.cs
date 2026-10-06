using Agrovesto.Backend.API.Hosting;

const string adminController = "Agrovesto.Backend.API.Controllers.AdminController";

var app = NomaosMicroserviceHost.Build(
    args,
    "service-provider",
    [adminController],
    new Dictionary<string, ISet<string>>
    {
        [adminController] = new HashSet<string>(StringComparer.Ordinal)
        {
            "CreateWalletProvider",
            "GetAllWallets",
            "GetById",
            "DisableWalletProvider",
            "EnableWalletProvider",
            "GetWalletParameters",
            "GetWalletParameter",
            "UpdateWalletParameters",
            "EditParameterValue",
            "EditParameterForBusiness",
            "GetAllProviders",
            "CreateProvider",
            "UpdateProvider",
            "GetParameters",
            "UpdateParameters",
            "DisableProvider",
            "CreateSMSProvier",
            "GetAllSMSProviders",
            "GetProvidersForBusiness",
            "GetSMSProviderById",
            "DisableProviderForBusiness",
            "EnableProviderForBusiness",
            "GetParametersByProvider",
            "EditBusinessParameterValue",
            "UpdateProviderParameter"
        }
    });
app.Run();
