using PowerControlHubApp.Models.Json;
namespace PowerControlHubApp.Services
{
    public interface IDashboardConnection
    {
        bool IsConfigured { get; }

        Task<IndexModel> GetDashboardDataAsync(CancellationToken ct = default);

        Task<SystemPinsResponseModel> GetSystemPinsAsync(CancellationToken ct = default);

        Task<SystemPinRestrictionsResponseModel> GetSystemPinRestrictionsAsync(CancellationToken ct = default);

        Task<SystemLocationTypesResponseModel> GetSystemLocationTypesAsync(CancellationToken ct = default);

        Task<List<string>> GetWarningsAsync(CancellationToken ct = default);
    }
}
