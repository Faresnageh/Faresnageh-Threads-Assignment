# Assignment 03 — Answers

Student name: Fares Nageh Abdelaal

Group / ID: G2

## Q1 — What is System.Threading.Monitor used for?

Monitor is used to synchronize threads and protect shared resources from race conditions

It does not create new threads

## Q2 — What does the C# lock statement compile to?

When we use lock with a normal object the compiler converts it to Monitor.Enter and Monitor.Exit with try/finally

When we use System.Threading.Lock in modern C# the compiler uses EnterScope instead

## Q3 — Differences between lock and Monitor

lock is easier to use and releases the lock automatically

Monitor needs more code because we have to release the lock ourselves

Monitor also gives us more control using TryEnter, Wait and Pulse

## Q4 — When do we use TryEnter, Wait and Pulse?

**TryEnter**

It is used to check if the lock is available without waiting or with a timeout

**Wait**

It is used when a thread needs to wait for a condition and releases the lock temporarily

**Pulse**

It is used to signal a waiting thread so it can try to continue

## Q5 — Same critical section using lock and Monitor

**Using lock**

```csharp
public class Inventory
{
    public int stock = 10;
    public int sold = 0;

    private readonly object _sync = new();

    public void Buy()
    {
        lock (_sync)
        {
            if (stock <= 0)
                return;

            stock--;
            sold++;
        }
    }
}
```

**Using Monitor**

```csharp
public class Inventory
{
    public int stock = 10;
    public int sold = 0;

    private readonly object _sync = new();

    public void Buy()
    {
        bool lockTaken = false;

        try
        {
            Monitor.Enter(_sync, ref lockTaken);

            if (stock <= 0)
                return;

            stock--;
            sold++;
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(_sync);
        }
    }
}
```

Both methods protect the same critical section

The expected result when 30 threads try to buy 10 items is

```text
Stock: 0
Sold: 10
```

## Sources

- ChatGPT

- https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor

- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock