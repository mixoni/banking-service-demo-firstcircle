namespace FirstCircle.Banking.Exceptions;

public sealed class AccountNotFoundException : Exception
{
    public AccountNotFoundException(Guid accountId)
        : base($"Account not found: {accountId}.")
    {
        AccountId = accountId;
    }

    public Guid AccountId { get; }
}
