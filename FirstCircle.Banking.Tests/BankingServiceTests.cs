using FirstCircle.Banking;
using FirstCircle.Banking.Exceptions;
using Xunit;

namespace FirstCircle.Banking.Tests;

public sealed class BankingServiceTests
{
    [Fact]
    public void CreateAccount_WithInitialDeposit_SetsCorrectBalance()
    {
        var svc = new BankingService();

        var accountId = svc.CreateAccount(Money.From(100m));

        Assert.Equal(100m, svc.GetBalance(accountId));
    }

    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var svc = new BankingService();
        var accountId = svc.CreateAccount(Money.From(100m));

        svc.Deposit(accountId, Money.From(50m));

        Assert.Equal(150m, svc.GetBalance(accountId));
    }

    [Fact]
    public void Withdraw_DecreasesBalance()
    {
        var svc = new BankingService();
        var accountId = svc.CreateAccount(Money.From(100m));

        svc.Withdraw(accountId, Money.From(40m));

        Assert.Equal(60m, svc.GetBalance(accountId));
    }

    [Fact]
    public void Withdraw_WhenInsufficientFunds_Throws()
    {
        var svc = new BankingService();
        var accountId = svc.CreateAccount(Money.From(100m));

        var ex = Assert.Throws<InsufficientFundsException>(() =>
            svc.Withdraw(accountId, Money.From(101m)));

        Assert.Equal(100m, ex.CurrentBalance);
        Assert.Equal(101m, ex.RequestedAmount);
        Assert.Equal(100m, svc.GetBalance(accountId)); // no change
    }

    [Fact]
    public void Transfer_MovesFundsBetweenAccounts()
    {
        var svc = new BankingService();
        var fromId = svc.CreateAccount(Money.From(200m));
        var toId = svc.CreateAccount(Money.From(50m));

        svc.Transfer(fromId, toId, Money.From(70m));

        Assert.Equal(130m, svc.GetBalance(fromId));
        Assert.Equal(120m, svc.GetBalance(toId));
    }

    [Fact]
    public void Transfer_WhenInsufficientFunds_ThrowsAndDoesNotChangeBalances()
    {
        var svc = new BankingService();
        var fromId = svc.CreateAccount(Money.From(50m));
        var toId = svc.CreateAccount(Money.From(10m));

        var ex = Assert.Throws<InsufficientFundsException>(() =>
            svc.Transfer(fromId, toId, Money.From(60m)));

        Assert.Equal(50m, ex.CurrentBalance);
        Assert.Equal(60m, ex.RequestedAmount);

        Assert.Equal(50m, svc.GetBalance(fromId));
        Assert.Equal(10m, svc.GetBalance(toId));
    }

    [Fact]
    public void Transfer_ToSameAccount_Throws()
    {
        var svc = new BankingService();
        var id = svc.CreateAccount(Money.From(100m));

        Assert.Throws<ArgumentException>(() =>
            svc.Transfer(id, id, Money.From(10m)));

        Assert.Equal(100m, svc.GetBalance(id)); // no change
    }

    [Fact]
    public void UnknownAccount_ThrowsAccountNotFound()
    {
        var svc = new BankingService();
        var unknownId = Guid.NewGuid();

        Assert.Throws<AccountNotFoundException>(() =>
            svc.GetBalance(unknownId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Money_From_WithNonPositiveAmount_Throws(decimal amount)
    {
        var ex = Assert.Throws<InvalidAmountException>(() => Money.From(amount));
        Assert.Equal(amount, ex.Amount);
    }
}
