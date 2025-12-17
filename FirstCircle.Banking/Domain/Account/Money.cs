using FirstCircle.Banking.Exceptions;

public readonly struct Money
{
    public decimal Amount { get; }

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public static Money From(decimal amount)
    {
        if (amount <= 0m)
            throw new InvalidAmountException(amount);

        return new Money(amount);
    }
}
