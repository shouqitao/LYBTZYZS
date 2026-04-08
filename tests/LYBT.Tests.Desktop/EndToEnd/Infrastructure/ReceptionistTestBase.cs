using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Tests.Desktop.EndToEnd.Infrastructure;

public abstract class ReceptionistTestBase : WebApiE2ETestBase
{
    protected new async Task<LoginResponse> LoginAsReceptionistAsync()
    {
        var username = Configuration["TestCredentials:Receptionist:Username"] ?? "receptionist";
        var password = Configuration["TestCredentials:Receptionist:Password"] ?? "ReceptionistPass123!";
        
        return await LoginAsAsync(username, password);
    }

    protected async Task<bool> VerifyReceptionistRegistrationAccess()
    {
        var response = await RegistrationApi.GetQueueAsync();
        return response.Success;
    }
}
