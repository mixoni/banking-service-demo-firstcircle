using System;

namespace FirstCircle.Banking;

public readonly record struct Money(decimal Amount)
{
    public static Money From(decimal amount)
    {
        if (amount <= 0m)
            throw new Exceptions.InvalidAmountException(amount);

        return new Money(amount);
    }
}
