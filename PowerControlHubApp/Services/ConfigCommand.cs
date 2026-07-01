namespace PowerControlHubApp.Services
{
    public record ConfigCommand
    {
        public string Description { get; init; }
        public Func<HttpClient, CancellationToken, Task<bool>> ExecuteAsync { get; init; }
        public string SuccessMessageType { get; init; }
        public string FailureMessageType { get; init; }
        public object Context { get; init; }
    }
}
