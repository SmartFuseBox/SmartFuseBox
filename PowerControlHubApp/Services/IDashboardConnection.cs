using PowerControlHubApp.Models.Json;
namespace PowerControlHubApp.Services
{
    public interface IDashboardConnection
    {
        bool IsConfigured { get; }
        Task<IndexModel> GetDashboardDataAsync(CancellationToken ct = default);
    }
}
