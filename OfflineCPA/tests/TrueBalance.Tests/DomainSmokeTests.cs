using Domain.Entities;
using Domain.Enums;

namespace TrueBalance.Tests;

public class DomainSmokeTests
{
    [Fact]
    public void Account_defaults_are_safe()
    {
        var account = new Account();

        Assert.Equal(string.Empty, account.Name);
        Assert.Equal(string.Empty, account.Code);
        Assert.Equal(TaxLineItem.None, account.TaxFormLine);
        Assert.True(account.IsActive);
        Assert.Empty(account.JournalEntries);
    }
}
