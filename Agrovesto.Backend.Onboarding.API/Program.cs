using Agrovesto.Backend.API.Hosting;

var app = NomaosMicroserviceHost.Build(args, "onboarding", [
    "Agrovesto.Backend.API.Controllers.V2.Internal.AgentController",
    "Agrovesto.Backend.API.Controllers.V2.Internal.AuthenticationController",
    "Agrovesto.Backend.API.Controllers.V2.Internal.BulkUploadController",
    "Agrovesto.Backend.API.Controllers.V2.Internal.FarmMappingController",
    "Agrovesto.Backend.API.Controllers.V2.Internal.FarmerController",
    "Agrovesto.Backend.API.Controllers.V2.Internal.MembershipController",
    "Agrovesto.Backend.API.Controllers.V2.Internal.OrganizationController",
    "Agrovesto.Backend.API.Controllers.V2.Internal.ProfileController",
    "Agrovesto.Backend.API.Controllers.V2.Internal.VerificationController"
]);
app.Run();
