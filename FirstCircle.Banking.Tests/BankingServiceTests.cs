using FirstCircle.Banking;
using FirstCircle.Banking.Exceptions;
using Xunit;

namespace FirstCircle.Banking.Tests;

public sealed class BankingServiceTests
{
    [Fact]
    public void CreateAccount_WithInitialDeposit_SetsCorrectBalance()
    {
        var bankingService = new BankingService();

        var accountId = bankingService.CreateAccount(Money.From(100m));

        Assert.Equal(100m, bankingService.GetBalance(accountId));
    }

    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var bankingService = new BankingService();
        var accountId = bankingService.CreateAccount(Money.From(100m));

        bankingService.Deposit(accountId, Money.From(50m));

        Assert.Equal(150m, bankingService.GetBalance(accountId));
    }

    [Fact]
    public void Withdraw_DecreasesBalance()
    {
        var bankingService = new BankingService();
        var accountId = bankingService.CreateAccount(Money.From(100m));

        bankingService.Withdraw(accountId, Money.From(40m));

        Assert.Equal(60m, bankingService.GetBalance(accountId));
    }

    [Fact]
    public void Withdraw_WhenInsufficientFunds_Throws()
    {
        var bankingService = new BankingService();
        var accountId = bankingService.CreateAccount(Money.From(100m));

        var ex = Assert.Throws<InsufficientFundsException>(() =>
            bankingService.Withdraw(accountId, Money.From(101m)));

        Assert.Equal(100m, ex.CurrentBalance);
        Assert.Equal(101m, ex.RequestedAmount);
        Assert.Equal(100m, bankingService.GetBalance(accountId)); // no change
    }

    [Fact]
    public void Transfer_MovesFundsBetweenAccounts()
    {
        var bankingService = new BankingService();
        var fromId = bankingService.CreateAccount(Money.From(200m));
        var toId = bankingService.CreateAccount(Money.From(50m));

        bankingService.Transfer(fromId, toId, Money.From(70m));

        Assert.Equal(130m, bankingService.GetBalance(fromId));
        Assert.Equal(120m, bankingService.GetBalance(toId));
    }

    [Fact]
    public void Transfer_WhenInsufficientFunds_ThrowsAndDoesNotChangeBalances()
    {
        var bankingService = new BankingService();
        var fromId = bankingService.CreateAccount(Money.From(50m));
        var toId = bankingService.CreateAccount(Money.From(10m));

        var ex = Assert.Throws<InsufficientFundsException>(() =>
            bankingService.Transfer(fromId, toId, Money.From(60m)));

        Assert.Equal(50m, ex.CurrentBalance);
        Assert.Equal(60m, ex.RequestedAmount);

        Assert.Equal(50m, bankingService.GetBalance(fromId));
        Assert.Equal(10m, bankingService.GetBalance(toId));
    }

    [Fact]
    public void Transfer_ToSameAccount_Throws()
    {
        var bankingService = new BankingService();
        var id = bankingService.CreateAccount(Money.From(100m));

        Assert.Throws<ArgumentException>(() =>
            bankingService.Transfer(id, id, Money.From(10m)));

        Assert.Equal(100m, bankingService.GetBalance(id)); // no change
    }

    [Fact]
    public void UnknownAccount_ThrowsAccountNotFound()
    {
        var bankingService = new BankingService();
        var unknownId = Guid.NewGuid();

        Assert.Throws<AccountNotFoundException>(() =>
            bankingService.GetBalance(unknownId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Money_From_WithNegativeAmount_Throws(decimal amount)
    {
        var ex = Assert.Throws<InvalidAmountException>(() => Money.From(amount));
        Assert.Equal(amount, ex.Amount);
    }

    [Fact]
    public async Task Withdraw_ConcurrentExactWithdrawals_EndBalanceIsZero_NoExceptions()
    {
        var bankingService = new BankingService();
        var accountId = bankingService.CreateAccount(Money.From(1000m));

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, 1000).Select(_ => Task.Run(() =>
        {
            try
            {
                bankingService.Withdraw(accountId, Money.From(1m));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
        Assert.Equal(0m, bankingService.GetBalance(accountId));
    }


    [Fact]
    public async Task Transfer_ConcurrentBiDirectional_DoesNotDeadlock_TotalBalanceRemainsConstant()
    {
        var bankingService = new BankingService();
        var a = bankingService.CreateAccount(Money.From(1000m));
        var b = bankingService.CreateAccount(Money.From(1000m));

        const int operations = 10_000;
        var totalBefore = bankingService.GetBalance(a) + bankingService.GetBalance(b);

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, operations).Select(i => Task.Run(() =>
        {
            try
            {
                var isEven = i % 2 == 0;
                if (isEven)
                    bankingService.Transfer(a, b, Money.From(1m));
                else
                    bankingService.Transfer(b, a, Money.From(1m));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        var all = Task.WhenAll(tasks);
        var completed = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.Same(all, completed);   // no timeout → no deadlock
        
        await all;

        Assert.Empty(exceptions);

        var totalAfter = bankingService.GetBalance(a) + bankingService.GetBalance(b);
        Assert.Equal(totalBefore, totalAfter);
    }

    [Fact]
    public async Task Transfer_Concurrent3AccountCircle_DoesNotDeadlock_TotalBalanceRemainsConstant()
    {
        var bankingService = new BankingService();

        var a = bankingService.CreateAccount(Money.From(1000m));
        var b = bankingService.CreateAccount(Money.From(1000m));
        var c = bankingService.CreateAccount(Money.From(1000m));

        var totalBefore = bankingService.GetBalance(a) + bankingService.GetBalance(b) + bankingService.GetBalance(c);

        const int operations = 10_000;

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, operations).Select(i => Task.Run(() =>
        {
            try
            {
                switch (i % 3)
                {
                    case 0:
                        bankingService.Transfer(a, b, Money.From(1m));
                        break;
                    case 1:
                        bankingService.Transfer(b, c, Money.From(1m));
                        break;
                    default:
                        bankingService.Transfer(c, a, Money.From(1m));
                        break;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        var all = Task.WhenAll(tasks);
        var completed = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(5)));
        
        Assert.Same(all, completed);
        
        await all;

        Assert.Empty(exceptions);

        var totalAfter = bankingService.GetBalance(a) + bankingService.GetBalance(b) + bankingService.GetBalance(c);
        Assert.Equal(totalBefore, totalAfter);

        Assert.True(bankingService.GetBalance(a) >= 0m);
        Assert.True(bankingService.GetBalance(b) >= 0m);
        Assert.True(bankingService.GetBalance(c) >= 0m);
    }

    [Fact]
    public async Task Transfer_ConcurrentOppositeDirections_OneFails_WithoutPartialStateChange()
    {
        var svc = new BankingService();

        var a = svc.CreateAccount(Money.From(1000m));
        var b = svc.CreateAccount(Money.From(10m));

        using var startGate = new ManualResetEventSlim(false);

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var t1 = Task.Run(() =>
        {
            startGate.Wait();
            try
            {
                svc.Transfer(a, b, Money.From(100m));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        var t2 = Task.Run(() =>
        {
            startGate.Wait();
            try
            {
                svc.Transfer(b, a, Money.From(200m)); 
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        startGate.Set();

        var all = Task.WhenAll(t1, t2);
        var completed = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(3)));
        
        Assert.Same(all, completed);
        await all;

        var insufficient = exceptions.OfType<InsufficientFundsException>().ToList();
        Assert.Single(insufficient);

        /* Final state:
           - A->B (100) succeeds: A = 900, B = 110
           - B->A (200) fails: no change */
        Assert.Equal(900m, svc.GetBalance(a));
        Assert.Equal(110m, svc.GetBalance(b));
    }

}
