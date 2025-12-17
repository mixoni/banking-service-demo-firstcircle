namespace FirstCircle.Banking;

internal sealed class AccountRecord
{
    public AccountRecord(decimal initialBalance)
    {
        Balance = initialBalance;
        LockObj = new object();
    }

    public decimal Balance { get; set; }

    public object LockObj { get; }
}
