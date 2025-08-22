using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Xunit;
using FakeLoggerDemo;
using Microsoft.Extensions.Logging.Testing;

namespace FakeLoggerDemoTest;

public class MyServiceTests
{
  [Fact]
  public void DoWork_EmitsExpectedLog()
  {
    // Arrange
    var fakeLogger = new FakeLogger<MyService>();
    var service = new MyService(fakeLogger);

    // Act
    service.DoWork();

    // Assert: latest record exists and contains text
    var record = fakeLogger.LatestRecord;
    Assert.NotNull(record);
    Assert.Equal(LogLevel.Information, record.Level);
    Assert.Contains("Work done", record.Message);

    // Inspect structured state (Result property) - accept multiple shapes
    int? resultValue = null;

    if (record.StructuredState is IReadOnlyList<KeyValuePair<string, object>> structured)
    {
      var kvp = structured.FirstOrDefault(kv => kv.Key == "Result");
      resultValue = kvp.Value as int?;
    }

    if (resultValue is null)
    {
      if (record.State is IReadOnlyList<KeyValuePair<string, object>> state)
      {
        var kvp = state.FirstOrDefault(kv => kv.Key == "Result");
        resultValue = kvp.Value as int?;
      }
    }

    if (resultValue is null)
    {
      // fallback: try to parse the number from the formatted message
      var digits = Regex.Match(record.Message ?? string.Empty, "\\d+");
      if (digits.Success && int.TryParse(digits.Value, out var parsed))
      {
        resultValue = parsed;
      }
    }

    Assert.Equal(42, resultValue);
  }
}
