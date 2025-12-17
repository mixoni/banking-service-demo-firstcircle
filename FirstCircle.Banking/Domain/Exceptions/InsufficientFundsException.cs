namespace FirstCircle.Banking.Exceptions;

public sealed class InsufficientFundsException : Exception
{
    public InsufficientFundsException(decimal currentBalance, decimal requestedAmount)
        : base($"Insufficient funds. Balance: {currentBalance}, Requested: {requestedAmount}.")
    {
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
    }

    public decimal CurrentBalance { get; }
    public decimal RequestedAmount { get; }
}
