using Light.Serilog;
using NUnit.Framework;
using Serilog;

namespace UnitTests.SerilogTests;

public class SerilogLoggerTests
{
    [Test]
    public void EnsureInitialized_CalledTwice_DoesNotReassignLogger()
    {
        // Regression: EnsureInitialized used to compare Log.Logger.GetType() against typeof(Serilogger)
        // (a static helper class, never the logger's runtime type), so the check was always true and a
        // brand-new logger was assigned on every call, defeating the idempotency the method name promises.
        Serilogger.EnsureInitialized();
        var loggerAfterFirstCall = Log.Logger;

        Serilogger.EnsureInitialized();
        var loggerAfterSecondCall = Log.Logger;

        Assert.That(ReferenceEquals(loggerAfterFirstCall, loggerAfterSecondCall), Is.True);
    }

    [Test]
    public void Initialize_ReturnsUsableLogger()
    {
        var logger = Serilogger.Initialize();

        Assert.That(logger, Is.Not.Null);
        Assert.DoesNotThrow(() => logger.Information("test log message"));
    }
}
