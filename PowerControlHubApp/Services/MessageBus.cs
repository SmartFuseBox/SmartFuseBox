using System.Collections.Concurrent;

namespace PowerControlHubApp.Services
{
    public class MessageBus : IMessageBus
    {
        private readonly ConcurrentDictionary<Type, List<WeakReference>> _subscribers;

        public MessageBus()
        {
            _subscribers = new ConcurrentDictionary<Type, List<WeakReference>>();
        }

        public void Publish<T>(T message)
        {
            Type type = typeof(T);

            if (!_subscribers.TryGetValue(type, out List<WeakReference> handlers))
            {
                return;
            }

            List<WeakReference> toRemove = [];

            foreach (WeakReference weak in handlers)
            {
                if (weak.Target is Action<T> action)
                {
                    action(message);
                }
                else
                {
                    toRemove.Add(weak);
                }
            }

            if (toRemove.Count > 0)
            {
                lock (handlers)
                {
                    foreach (WeakReference dead in toRemove)
                    {
                        handlers.Remove(dead);
                    }
                }
            }
        }

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);
            WeakReference weak = new WeakReference(handler);
            List<WeakReference> handlers = _subscribers.GetOrAdd(type, _ => []);

            lock (handlers)
            {
                handlers.Add(weak);
            }

            return new Unsubscriber<T>(handlers, weak);
        }

        private class Unsubscriber<T> : IDisposable
        {
            private readonly List<WeakReference> _handlers;
            private readonly WeakReference _weak;
            private bool _disposed;

            public Unsubscriber(List<WeakReference> handlers, WeakReference weak)
            {
                _handlers = handlers;
                _weak = weak;
                _disposed = false;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                lock (_handlers)
                {
                    _handlers.Remove(_weak);
                }
                _disposed = true;
            }
        }
    }
}
