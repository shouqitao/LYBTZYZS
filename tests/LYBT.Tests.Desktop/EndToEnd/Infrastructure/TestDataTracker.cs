using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.EndToEnd.Infrastructure;

public enum EntityType
{
    MedicalCase,
    Formula,
    Herb,
    Patient,
    User
}

public sealed class TestDataTracker : IAsyncDisposable
{
    private readonly List<(EntityType Type, Guid Id)> _tracked = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public TestDataTracker(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void Track(EntityType type, Guid id)
    {
        _tracked.Add((type, id));
    }

    public Guid Track(EntityType type, ApiResponse<object> response)
    {
        if (!response.Success || response.Data is null)
            throw new InvalidOperationException($"Cannot track failed response: {response.Message}");

        var id = ExtractId(response.Data);
        _tracked.Add((type, id));
        return id;
    }

    public async ValueTask DisposeAsync()
    {
        if (_tracked.Count == 0) return;

        _logger.LogInformation("Cleaning up {Count} tracked entities...", _tracked.Count);

        // Reverse order — delete dependents first (MedicalCase before Patient, etc.)
        foreach (var (type, id) in Enumerable.Reverse(_tracked))
        {
            try
            {
                await DeleteEntityAsync(type, id);
                _logger.LogDebug("Deleted {Type} {Id}", type, id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup {Type} {Id} — may already be deleted", type, id);
            }
        }

        _tracked.Clear();
    }

    private async Task DeleteEntityAsync(EntityType type, Guid id)
    {
        switch (type)
        {
            case EntityType.MedicalCase:
                var medicalCaseApi = _serviceProvider.GetRequiredService<IMedicalCaseApi>();
                await medicalCaseApi.DeleteMedicalCaseAsync(id);
                break;
            case EntityType.Formula:
                var formulaApi = _serviceProvider.GetRequiredService<IFormulaApi>();
                await formulaApi.DeleteFormulaAsync(id);
                break;
            case EntityType.Herb:
                var herbApi = _serviceProvider.GetRequiredService<IHerbApi>();
                await herbApi.DeleteHerbAsync(id);
                break;
            case EntityType.Patient:
                var patientApi = _serviceProvider.GetRequiredService<IPatientApi>();
                await patientApi.DeletePatientAsync(id);
                break;
            case EntityType.User:
                var userApi = _serviceProvider.GetRequiredService<IUserApi>();
                await userApi.DeleteUserAsync(id);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown entity type");
        }
    }

    private static Guid ExtractId(object data)
    {
        if (data is System.Text.Json.JsonElement element)
        {
            if (element.TryGetProperty("Id", out var idProp) || element.TryGetProperty("id", out idProp))
            {
                if (idProp.TryGetGuid(out var guid))
                    return guid;
            }
            throw new InvalidOperationException($"Cannot extract Id from JsonElement: {element}");
        }

        var idProperty = data.GetType().GetProperty("Id");
        if (idProperty?.GetValue(data) is Guid g)
            return g;

        throw new InvalidOperationException($"Cannot extract Id from {data.GetType().Name}");
    }
}
