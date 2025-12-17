using System.Collections.Concurrent;
using FirstCircle.Banking.Exceptions;

namespace FirstCircle.Banking;

public sealed class BankingService
{
    private readonly ConcurrentDictionary<Guid, AccountRecord> _accounts = new();

    public Guid CreateAccount(Money initialDeposit)
    {
        var id = Guid.NewGuid();
        var record = new AccountRecord(initialDeposit.Amount);

        if (!_accounts.TryAdd(id, record))
            throw new InvalidOperationException("Failed to create account.");

        return id;
    }

    public void Deposit(Guid accountId, Money amount)
    {
        var account = GetAccount(accountId);

        lock (account.LockObj)
        {
            account.Balance += amount.Amount;
        }
    }

    public void Withdraw(Guid accountId, Money amount)
    {
        var account = GetAccount(accountId);

        lock (account.LockObj)
        {
            EnsureSufficientFunds(account, amount.Amount);
            account.Balance -= amount.Amount;
        }
    }

    public void Transfer(Guid fromAccountId, Guid toAccountId, Money amount)
    {
        if (fromAccountId == toAccountId)
            throw new ArgumentException("Cannot transfer to the same account.", nameof(toAccountId));

        var from = GetAccount(fromAccountId);
        var to = GetAccount(toAccountId);

        var (first, second) = OrderLocks(fromAccountId, from, toAccountId, to);

        lock (first.LockObj)
        lock (second.LockObj)
        {
            EnsureSufficientFunds(from, amount.Amount);

            from.Balance -= amount.Amount;
            to.Balance += amount.Amount;
        }
    }

    public decimal GetBalance(Guid accountId)
    {
        var account = GetAccount(accountId);

        lock (account.LockObj)
        {
            return account.Balance;
        }
    }

    private AccountRecord GetAccount(Guid accountId)
    {
        if (_accounts.TryGetValue(accountId, out var account))
            return account;

        throw new AccountNotFoundException(accountId);
    }

    private static void EnsureSufficientFunds(AccountRecord account, decimal amount)
    {
        if (account.Balance < amount)
            throw new InsufficientFundsException(account.Balance, amount);
    }

    private static (AccountRecord First, AccountRecord Second) OrderLocks(
        Guid aId, AccountRecord a,
        Guid bId, AccountRecord b)
    {
        return aId.CompareTo(bId) <= 0 ? (a, b) : (b, a);
    }
}
