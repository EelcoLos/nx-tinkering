using System;
using Microsoft.Extensions.Logging;

namespace FakeLoggerDemo;

public class MyService(ILogger<MyService> logger)
{
    public void DoWork()
    {
        logger.LogInformation("Work started");

        // simulate some behavior
        var result = 42;

        logger.LogInformation("Work done: {Result}", result);
    }
}
