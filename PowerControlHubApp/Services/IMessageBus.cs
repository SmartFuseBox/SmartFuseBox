namespace PowerControlHubApp.Services
{
    public interface IMessageBus
    {
        void Publish<T>(T message);
        IDisposable Subscribe<T>(Action<T> handler);
    }
}
