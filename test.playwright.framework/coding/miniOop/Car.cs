namespace test.playwright.framework.coding.miniOOP;

public class Car
{
    public string Brand;
    public int Year;

    public void StartEngine()
    {
        Console.WriteLine($"{Brand} Engine starting...");
    }

    public virtual void Drive()
    {
        Console.WriteLine($"{Brand} driving...");
    }
}

public class ElectricCar : Car
{
    public int BatteryLevel;

    public void Charge()
    {
        Console.WriteLine($"{Brand} Engine charging...");
    }
}

public class Bike : Car
{
    public override void Drive()
    {
        Console.WriteLine("Riding bike...");
    }
}

public class BankAccount
{
    private decimal _balance;

    public void Deposit(decimal amount)
    {
        if (amount > 0)
        {
            _balance += amount;
        }
    }

    public decimal GetBalance()
    {
        return _balance;
    }
}

public interface IVehicle
{
    void Start();
    void Stop();
}

public class Motorcycle : IVehicle
{
    public void Start()
    {
        Console.WriteLine("Motorcycle starting...");
    }

    public void Stop()
    {
        Console.WriteLine("Motorcycle stopping...");
    }
}