namespace Agrovesto.Backend.Core.Interfaces.Services;

public interface IOutboxWriter
{
    void Enqueue(string eventType, object payload);
}
