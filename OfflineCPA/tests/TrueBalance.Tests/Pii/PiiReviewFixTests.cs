using Application.Interfaces;
using Domain.Pii;
using Microsoft.Extensions.Logging;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Pii;

/// <summary>Regressions for the gaps found in the 04b architect review.</summary>
public class PiiReviewFixTests
{
    private const int ArabicIndicZero = 0x0660;
    private const int FullWidthZero = 0xFF10;

    [Theory]
    [InlineData(FakePii.SsnBare, ArabicIndicZero)]
    [InlineData(FakePii.SsnFormatted, FullWidthZero)]
    [InlineData(FakePii.ItinFormatted, ArabicIndicZero)]
    [InlineData(FakePii.AccountNumberA, FullWidthZero)]
    public void Non_ascii_digits_are_masked_like_ascii_ones(string value, int zero)
    {
        var masker = new MaskingPiiMasker();
        var foreign = FakePii.InDigits(value, (char)zero);

        var result = masker.MaskDetailed($"A {foreign} B");
        var ascii = masker.MaskDetailed($"A {value} B");

        Assert.DoesNotContain(foreign, result.MaskedText, StringComparison.Ordinal);
        Assert.Equal(ascii.MaskedText, result.MaskedText);
        Assert.Equal(ascii.Matches[0].Fingerprint, result.Matches[0].Fingerprint);
    }

    [Fact]
    public void Logger_withholds_the_line_when_masking_fails()
    {
        var logs = new CapturingLoggerProvider();
        var logger = new Backend.Extensions.PiiRedactionLoggerProvider(logs, new ThrowingMasker()).CreateLogger("t");

        logger.LogError(new InvalidOperationException(FakePii.SsnFormatted), "{Value}", FakePii.SsnFormatted);

        var line = Assert.Single(logs.Lines);
        Assert.Contains("[log line withheld: masking failed]", line, StringComparison.Ordinal);
        Assert.DoesNotContain(FakePii.SsnFormatted, line, StringComparison.Ordinal);
    }

    private sealed class ThrowingMasker : IPiiMasker
    {
        public string Mask(string text) => throw new FormatException(text);

        public PiiMaskingResult MaskDetailed(string text) => throw new FormatException(text);
    }
}
