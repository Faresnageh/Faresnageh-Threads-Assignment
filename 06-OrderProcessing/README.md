# Order Processing System

## Phase 1 - Async Data Access Layer

### Validation Results

- Total Orders: 20
- Validated: 15
- Rejected: 5
- Sync Time: 51.5 seconds
- Async Time: 67.3 seconds
- Saved Orders: 20



### Phase 1 Questions and Answers

#### Q1 Why are sync and async times almost the same

Both sync and async run orders one by one

Each order must finish before the next order starts

Async does not make each order faster

In my test async took more time because it also saved the orders

#### Q2 Is the thread ID always the same before and after await

No it is not always the same

The thread can return to the ThreadPool while waiting

Another thread can continue the work after await

In my logs the ProductRepository started the query on Thread 11 and finished it on Thread 12

```text
[20:28:11.816] [Thread 11] ProductRepository -> Executing query for Product #3...
[20:28:12.317] [Thread 12] ProductRepository -> Product #3 found (Wireless Mouse)
```

#### Q3 What happens during Task Delay and Thread Sleep

Thread Sleep blocks the thread until the waiting time ends

Task Delay does not block the waiting thread

The thread can return to the ThreadPool and do other work

#### Q4 Why should a repository return Task of T instead of T

Task of T lets us use await with an async operation

It also gives us the result when the operation finishes

This helps us avoid blocking the calling thread while waiting

#### Q5 What is the difference between ReadAllText and ReadAllTextAsync

Both methods read data from a file

ReadAllText blocks the calling thread until reading finishes

ReadAllTextAsync lets us use await without blocking the calling thread

The system may still use a background thread for some file operations


---

## Phase 2 - Concurrent Order Processing

### Implementation

In this phase, I processed multiple orders concurrently using async/await and Task.

I implemented the pricing calculations, safe stock reservations, invoice generation, and report generation.

I also added a race condition demo to compare unsafe and safe stock reservation.

### Results

| Test | Result |
|---|---|
| Unsafe stock reservation | Both orders succeeded with only one keyboard available |
| Safe stock reservation | One order succeeded and the other failed |
| Sequential pricing | 4.73 seconds |
| Concurrent pricing | 3.61 seconds |
| Concurrent order processing | 7.81 seconds |
| Completed orders | 13 |
| Rejected orders | 6 |
| Failed orders | 1 |
| Total orders saved | 20 |
| Top-selling product | USB-C Hub (5 units) |

Order #7 failed because the shipping provider was unavailable. Its reserved stock was released.

I also tested cancellation. In that run, 6 orders finished and 14 were cancelled. I checked `products.json` and confirmed that the stock quantities returned to their original values.

---

### Phase 2 Questions

**1. What is the difference between starting a task and awaiting it?**

Starting a task begins the operation. Awaiting it means waiting asynchronously for its result or completion. I can start multiple tasks first and then await them one by one.

**2. Does every Task create a new thread? Does Task.Delay use a thread while waiting?**

No. A Task does not always create a new thread. For example, Task.Delay waits using a timer and does not keep a thread busy during the delay.

**3. What happens if you call .Result or .Wait() instead of await?**

They block the calling thread until the task finishes. This can waste threads and may cause deadlocks in some applications.

I created a separate `BlockingDemo` class to test this. The output was:

```text
===== BLOCKING DEMO =====
Before Wait
After Wait
Before Result
Result: 42
```

**4. What happens to an exception inside a task that is never awaited?**

The task becomes Faulted, but the exception may not be observed by the calling code. This can make errors difficult to detect.

**5. When should you use Task.Run()?**

I use Task.Run for CPU-bound work, like rendering an invoice. I avoid using it just to wrap database queries or asynchronous file operations because they already have async APIs.

**6. What is the difference between Task and Task<T>?**

Task represents an asynchronous operation without a return value. Task<T> represents an asynchronous operation that returns a value of type T.

**7. Why can't a lock block contain await?**

A lock is designed for synchronous code and thread-based locking. Await may suspend the method, so C# does not allow await inside a lock statement. For asynchronous synchronization, SemaphoreSlim can be used.

**8. Why did the unsafe stock reservation oversell, and how did you prevent it?**

Both operations read the same stock value before either update was completed. Both believed the last keyboard was available.

I fixed this by protecting the read-and-update operation using SemaphoreSlim in JsonFileStore.UpdateAsync. After the fix, only one reservation succeeded.