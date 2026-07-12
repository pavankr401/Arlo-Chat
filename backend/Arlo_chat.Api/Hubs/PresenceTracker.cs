using System.Collections.Concurrent;

namespace Arlo_chat.Api.Hubs;

public class PresenceTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userConnections = new();

    public void AddConnection(string userId, string connectionId)
    {
        var connections = _userConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        connections.TryAdd(connectionId, 0);
    }

    public void RemoveConnection(string userId, string connectionId)
    {
        if (!_userConnections.TryGetValue(userId, out var connections))
            return;

        connections.TryRemove(connectionId, out _);
        if (connections.IsEmpty)
            _userConnections.TryRemove(userId, out _);
    }

    public IEnumerable<string> GetConnections(string userId) =>
        _userConnections.TryGetValue(userId, out var connections) ? connections.Keys : Enumerable.Empty<string>();
}
