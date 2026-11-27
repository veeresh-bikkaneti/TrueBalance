using Backend.Extensions;
using Microsoft.Extensions.Logging;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Pii;

public class PiiRedactionTests
{
    [Fact]
    public void Raw_values_never_reach_the_inner_logger()
    {
        var capture = new CapturingLoggerProvider();
        var provider = new PiiRedactionLoggerProvider(capture, new MaskingPiiMasker());
        var logger = provider.CreateLogger("Test.Category");

        foreach (var raw in FakePii.LeakTargets())
        {
            // A date of birth is only PII after its label.
            var value = raw == FakePii.DateOfBirth ? $"DOB {raw}" : raw;
            logger.LogInformation("Importing row with value {Value}", value);
        }

        Assert.NotEmpty(capture.Lines);
        foreach (var line in capture.Lines)
        {
            foreach (var raw in FakePii.LeakTargets())
            {
                Assert.DoesNotContain(raw, line, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Non_pii_messages_pass_through_unchanged()
    {
        var capture = new CapturingLoggerProvider();
        var provider = new PiiRedactionLoggerProvider(capture, new MaskingPiiMasker());
        var logger = provider.CreateLogger("Test.Category");

        logger.LogInformation("Processed entry for ACME Corp, amount 12.50");

        var line = Assert.Single(capture.Lines);
        Assert.Equal("Processed entry for ACME Corp, amount 12.50", line);
    }

    [Fact]
    public void Exception_messages_with_pii_are_replaced_with_masked_ones()
    {
        var capture = new CapturingLoggerProvider();
        var provider = new PiiRedactionLoggerProvider(capture, new MaskingPiiMasker());
        var logger = provider.CreateLogger("Test.Category");

        var ex = new InvalidOperationException($"Failed to parse account {FakePii.AccountNumberA}");
        logger.LogError(ex, "Import failed");

        var entry = Assert.Single(capture.Exceptions);
        Assert.DoesNotContain(FakePii.AccountNumberA, entry.Message, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Inner_exception_messages_are_masked_too()
    {
        var capture = new CapturingLoggerProvider();
        var provider = new PiiRedactionLoggerProvider(capture, new MaskingPiiMasker());
        var logger = provider.CreateLogger("Test.Category");

        var inner = new ArgumentException($"bad segment {FakePii.SsnFormatted}");
        var ex = new InvalidOperationException("Statement parsing failed.", inner);
        logger.LogError(ex, "Import failed");

        var entry = Assert.Single(capture.Exceptions);
        Assert.Contains("Statement parsing failed.", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(FakePii.SsnFormatted, entry.ToString(), StringComparison.Ordinal);
        Assert.Contains("ArgumentException", entry.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Clean_exception_chains_keep_the_original_exception_with_its_stack()
    {
        var capture = new CapturingLoggerProvider();
        var provider = new PiiRedactionLoggerProvider(capture, new MaskingPiiMasker());
        var logger = provider.CreateLogger("Test.Category");

        var ex = new InvalidOperationException("plain parse failure");
        logger.LogError(ex, "Import failed");

        var entry = Assert.Single(capture.Exceptions);
        Assert.Same(ex, entry);
    }

    [Fact]
    public void Aggregate_exception_children_are_masked_too()
    {
        var capture = new CapturingLoggerProvider();
        var provider = new PiiRedactionLoggerProvider(capture, new MaskingPiiMasker());
        var logger = provider.CreateLogger("Test.Category");

        var ex = new AggregateException(
            new InvalidOperationException("plain failure"),
            new ArgumentException($"bad segment {FakePii.SsnFormatted}"));
        logger.LogError(ex, "Import failed");

        var entry = Assert.Single(capture.Exceptions);
        Assert.DoesNotContain(FakePii.SsnFormatted, entry.ToString(), StringComparison.Ordinal);
        Assert.Contains("ArgumentException", entry.ToString(), StringComparison.Ordinal);
        Assert.Contains("plain failure", entry.ToString(), StringComparison.Ordinal);
    }
}
