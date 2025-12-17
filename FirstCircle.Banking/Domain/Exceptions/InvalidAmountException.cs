namespace FirstCircle.Banking.Exceptions;

public sealed class InvalidAmountException : Exception
{
    public InvalidAmountException(decimal amount)
        : base($"Amount must be greater than 0. Provided: {amount}.")
    {
        Amount = amount;
    }

    public decimal Amount { get; }
}
