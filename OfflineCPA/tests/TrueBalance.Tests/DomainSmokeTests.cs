using Domain.Entities;

namespace TrueBalance.Tests;

public class DomainSmokeTests
{
    [Fact]
    public void Account_defaults_are_safe()
    {
        var account = new Account();

        Assert.Equal(string.Empty, account.Name);
        Assert.Equal(string.Empty, account.Code);
        Assert.Null(account.TaxLineId);
        Assert.False(account.IsMeals);
        Assert.True(account.IsActive);
        Assert.Empty(account.Postings);
    }
}
