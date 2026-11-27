using System.Text;
using Domain.Pii;
using Infrastructure.Pii;
using Infrastructure.Pii.Detectors;
using TrueBalance.Tests.Pii.Fixtures;

namespace TrueBalance.Tests.Pii;

public class PiiPolicyTests
{
    private static PiiPolicy CreatePolicy()
    {
        return new PiiPolicy(Convert.FromBase64String(FakePii.TestKeyBase64), PiiDetectors.All());
    }

    [Fact]
    public void Masking_removes_every_raw_value_and_keeps_the_rest_of_the_text()
    {
        var result = CreatePolicy().Mask(FakePii.SampleStatementText());

        foreach (var raw in FakePii.LeakTargets())
        {
            Assert.DoesNotContain(raw, result.MaskedText, StringComparison.Ordinal);
        }

        // Non-destructive outside detected values: amounts, dates and payee text stay.
        Assert.Contains("Amount 12.50 on 2024-11-25 to ACME Corp.", result.MaskedText, StringComparison.Ordinal);
    }

    [Fact]
    public void Tokens_keep_last_four_digits_and_a_fingerprint()
    {
        var result = CreatePolicy().Mask($"Acct {FakePii.AccountNumberA}");

        var match = Assert.Single(result.Matches);
        Assert.Equal(PiiType.BankAccountNumber, match.Type);
        Assert.Equal(FakePii.AccountNumberA[^4..], match.Last4);
        Assert.Equal(64, match.Fingerprint.Length);
        Assert.Contains($"****{FakePii.AccountNumberA[^4..]}", result.MaskedText, StringComparison.Ordinal);
        Assert.Contains($"fp:{match.Fingerprint[..16]}", result.MaskedText, StringComparison.Ordinal);
    }

    [Fact]
    public void Same_number_masks_to_the_same_token_and_fingerprint()
    {
        var policy = CreatePolicy();
        var first = policy.Mask($"Acct {FakePii.AccountNumberA} debit");
        var second = policy.Mask($"Account {FakePii.AccountNumberA} credit");

        Assert.Equal(first.Matches[0].Token, second.Matches[0].Token);
        Assert.Equal(first.Matches[0].Fingerprint, second.Matches[0].Fingerprint);
        Assert.Equal(first.Matches[0].Last4, second.Matches[0].Last4);
    }

    [Fact]
    public void Different_numbers_with_the_same_last_four_get_different_fingerprints()
    {
        var result = CreatePolicy().Mask(
            $"Acct {FakePii.AccountNumberA} and Acct {FakePii.AccountNumberB}");

        Assert.Equal(2, result.Matches.Count);
        Assert.Equal(result.Matches[0].Last4, result.Matches[1].Last4);
        Assert.NotEqual(result.Matches[0].Fingerprint, result.Matches[1].Fingerprint);
        Assert.NotEqual(result.Matches[0].Token, result.Matches[1].Token);
    }

    [Fact]
    public void Separator_variants_of_the_same_number_share_a_fingerprint()
    {
        var policy = CreatePolicy();
        var dashed = policy.Mask($"SSN {FakePii.SsnFormatted}");
        var bare = policy.Mask($"SSN {FakePii.SsnBare}");

        Assert.Equal(dashed.Matches[0].Fingerprint, bare.Matches[0].Fingerprint);
    }

    [Fact]
    public void Overlapping_matches_resolve_to_the_most_specific_type()
    {
        var result = CreatePolicy().Mask($"Routing {FakePii.RoutingNumber} for account {FakePii.AccountNumberA}");

        // The routing number is also an 8+ digit run; the check digit makes
        // the ABA type claim the span, and the account number is its own match.
        Assert.Equal(2, result.Matches.Count);
        Assert.Equal(PiiType.AbaRoutingNumber, result.Matches[0].Type);
        Assert.Equal(PiiType.BankAccountNumber, result.Matches[1].Type);
    }

    [Fact]
    public void Adjacent_values_in_one_digit_run_are_masked_per_value()
    {
        // The account run and the SSN sit in one greedy digit run; the SSN
        // outranks the whole-run account candidate, so both values are
        // masked separately instead of one token swallowing the other.
        // Account B: with account A's first eight digits, the joined run happens
        // to pass the Luhn check and is claimed as one card.
        var result = CreatePolicy().Mask(
            $"Acct {FakePii.AccountNumberB[..8]} {FakePii.SsnFormatted} debit");

        Assert.Equal(2, result.Matches.Count);
        Assert.Equal(PiiType.BankAccountNumber, result.Matches[0].Type);
        Assert.Equal(PiiType.Ssn, result.Matches[1].Type);
        foreach (var raw in FakePii.LeakTargets())
        {
            Assert.DoesNotContain(raw, result.MaskedText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Grouped_account_numbers_stay_one_value()
    {
        var result = CreatePolicy().Mask("Acct 1234 5678 9012 debit");

        var match = Assert.Single(result.Matches);
        Assert.Equal(PiiType.BankAccountNumber, match.Type);
        Assert.Equal("9012", match.Last4);
        Assert.DoesNotContain("1234 5678 9012", result.MaskedText, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskDetailed_returns_the_structured_matches_for_callers_that_store_keys()
    {
        var result = CreatePolicy().Mask($"Acct {FakePii.AccountNumberA}");

        var match = Assert.Single(result.Matches);
        Assert.Equal(FakePii.AccountNumberA[^4..], match.Last4);
        Assert.Equal(64, match.Fingerprint.Length);
        Assert.Contains(match.Token, result.MaskedText, StringComparison.Ordinal);
    }

    [Fact]
    public void Fingerprint_key_changes_the_digest_but_not_the_masked_shape()
    {
        var keyA = Convert.FromBase64String(FakePii.TestKeyBase64);
        var keyB = Convert.FromBase64String("YW5vdGhlci1zeW50aGV0aWMta2V5LWZvci1tYXNraW5nLXRlc3Rz");
        var detectors = new IPiiDetector[]
        {
            new AbaRoutingNumberDetector(),
            new SsnDetector(),
            new EinItinDetector(),
            new BankAccountNumberDetector(),
        };

        var first = new PiiPolicy(keyA, detectors).Mask($"Acct {FakePii.AccountNumberA}").Matches[0];
        var second = new PiiPolicy(keyB, detectors).Mask($"Acct {FakePii.AccountNumberA}").Matches[0];

        Assert.Equal(first.Last4, second.Last4);
        Assert.NotEqual(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public void Long_texts_mask_every_occurrence()
    {
        var text = new StringBuilder();
        for (var i = 0; i < 50; i++)
        {
            text.Append($"row {i}: account {FakePii.AccountNumberA}; ");
        }

        var result = CreatePolicy().Mask(text.ToString());

        Assert.Equal(50, result.Matches.Count);
        Assert.DoesNotContain(FakePii.AccountNumberA, result.MaskedText, StringComparison.Ordinal);
        Assert.All(result.Matches, m => Assert.Equal(m.Token, result.Matches[0].Token));
    }
}
