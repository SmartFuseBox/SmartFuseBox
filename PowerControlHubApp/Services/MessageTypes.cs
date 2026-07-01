namespace PowerControlHubApp.Services
{
    public record MetaDataRefreshed();
    public record SensorConfigSucceeded(int Index, string Name);
    public record RelayConfigSucceeded(int Index);
    public record ConfigCommandFailed(string Command, string Error);
    public record ConnectionStateChanged(string ConnectionName, bool Connected);
}
