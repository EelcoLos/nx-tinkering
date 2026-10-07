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

    // Inspect structured state (the {Result} template value)
    Assert.Equal("42", record.GetStructuredStateValue("Result"));
  }
}
