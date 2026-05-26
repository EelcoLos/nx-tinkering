using System.Collections.Concurrent;

namespace A2ADemo.ApiBackend;

public sealed class TriageStore
{
  private readonly ConcurrentDictionary<string, TriageRecord> records = new(StringComparer.OrdinalIgnoreCase);

  public void Save(TriageRecord record) => records[record.Id] = record;

  public TriageRecord? Get(string id) => records.TryGetValue(id, out var record) ? record : null;
}
