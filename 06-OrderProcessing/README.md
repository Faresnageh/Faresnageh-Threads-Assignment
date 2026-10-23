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

---

## Phase 3 - Full Checkout Workflow

Phase 3 implements the complete checkout workflow with concurrent order processing, payment handling, controlled failures, compensation, email retries, cancellation support, and async demonstrations.

### Features

- Concurrent order processing
- Safe stock reservation and stock rollback on failure
- Order pricing before payment
- Payment processing through `IPaymentGateway`
- Email sending through `IEmailService`
- Payment timeout using a 3-second cancellation timeout
- Payment decline handling
- Payment refund when saving a completed order fails
- Constructor-based failure injection
- Email retry mechanism
- Failed emails are stored in `email_retry.json`
- Cancellation support using `CancellationToken`
- Sync vs Async demonstration
- Fire-and-forget demonstration

### Controlled Failure Injection

Failures are configured through constructors instead of checking specific order IDs inside the services.

```csharp
OrderRepository orderRepository = new([13]);

PaymentGateway paymentGateway = new([5], [8]);

EmailService emailService = new(new Dictionary<int, int>
{
    [3] = 2,
    [17] = int.MaxValue
});
```

This produces the following controlled scenarios:

- Order #5: payment capture exceeds the 3-second timeout and the order becomes `PaymentFailed`
- Order #8: payment is declined with `Card declined by issuer`
- Order #13: payment is captured, saving the completed order fails, the payment is refunded, stock is released, and the order becomes `Failed`
- Order #3: email sending fails twice and succeeds on attempt 3
- Order #17: email sending fails on all 4 attempts and is added to `email_retry.json`
- A failed confirmation email does not change a successfully paid order from `Completed`

### Payment Processing

The payment gateway simulates three steps:

```text
Authorize      800 ms
Fraud check    500 ms
Capture        700 ms
```

Order #5 uses a 5000 ms capture delay to simulate a timeout.

Transaction IDs use this format:

```text
TXN-XXXXXXXXXXXX
```

Example:

```text
TXN-D1F21E7D982D
```

### Payment Timeout

Payment processing is linked to the main run cancellation token and also has a 3-second timeout.

If only the payment timeout expires:

```text
Status = PaymentFailed
FailureReason = Payment gateway timeout (3s)
```

The reserved stock is returned.

If the whole application run is cancelled, the order is handled as a cancelled order instead of `PaymentFailed`.

### Payment Decline

Order #8 is configured as a declined payment.

Result:

```text
Order #8 | PaymentFailed | Card declined by issuer
```

Its reserved stock is returned.

### Database Failure After Payment

Order #13 is configured through `OrderRepository` to fail when it is being saved with:

```text
Status = Completed
```

The simulated repository error is:

```text
Connection lost during commit
```

Because payment has already been captured, the processor performs compensation:

1. Refund the captured payment
2. Release the reserved stock
3. Mark the order as `Failed`
4. Save the final failure state

Example result:

```text
Order #13 | Failed | Database save failed after payment; payment TXN-... refunded
```

### Email Retry

Confirmation emails use one initial attempt and up to three retries.

The delays before retries are:

```text
Attempt 1
↓ failure

Wait 500 ms
Attempt 2
↓ failure

Wait 1000 ms
Attempt 3
↓ failure

Wait 2000 ms
Attempt 4
```

Order #3 is configured to fail twice:

```text
Order #3 Email Attempts: 3
```

It succeeds on the third attempt and is stored normally in `emails.json`.

Order #17 always fails:

```text
Order #17 Email Attempts: 4
```

After attempt 4, the email is stored in:

```text
email_retry.json
```

The order itself remains:

```text
Completed
```

### Final Order Results

The final Phase 3 run produced:

```text
Total Orders: 20
Completed: 11
Rejected: 6
Payment Failed: 2
Failed: 1
```

Final order states:

```text
Order #1  | Completed
Order #2  | Completed
Order #3  | Completed
Order #4  | Rejected      | Customer is inactive
Order #5  | PaymentFailed | Payment gateway timeout (3s)
Order #6  | Completed
Order #7  | Completed
Order #8  | PaymentFailed | Card declined by issuer
Order #9  | Rejected      | Insufficient stock for 'Wireless Mouse'
Order #10 | Completed
Order #11 | Rejected      | Insufficient stock for 'Mechanical Keyboard'
Order #12 | Rejected      | Coupon 'OLD20' has expired
Order #13 | Failed        | Database save failed after payment; payment refunded
Order #14 | Completed
Order #15 | Rejected      | Customer #99 not found
Order #16 | Completed
Order #17 | Completed
Order #18 | Rejected      | Coupon 'FAKE50' does not exist
Order #19 | Completed
Order #20 | Completed
```

Either Order #10 or Order #11 may get the last Mechanical Keyboard depending on which safe stock reservation completes first.

### Payment Results

```text
Captured: 11
Declined: 1
Refunded: 1
```

- Order #8 has the declined payment
- Order #13 has the refunded payment
- Order #5 has no captured payment because its capture operation times out

### Email Results

```text
Sent Emails: 10
Email Retry: 1

Order #3 Email Attempts: 3
Order #17 Email Attempts: 4
```

### Final JSON Files

The final Phase 3 output is copied to:

```text
sample-output/phase3/
```

Files:

```text
orders.json
products.json
payments.json
emails.json
email_retry.json
```

Expected important state:

```text
orders.json
20 records
11 Completed
6 Rejected
2 PaymentFailed
1 Failed

payments.json
11 Captured
1 Declined
1 Refunded

emails.json
10 records

email_retry.json
1 record
Order #17
Attempts = 4
LastError is set
```

### Sync vs Async Demo

The current demonstration compares 10 manually created threads with 10 asynchronous tasks performing simulated waiting work.

Measured result from the latest run:

```text
10 Threads Time: 1.03s
10 Async Tasks Time: 1.02s
```

The wall-clock times are similar because both versions perform approximately one second of waiting concurrently.

The important difference is resource usage.

The thread version creates and blocks 10 dedicated threads:

```text
Thread #1 started on Thread 15
...
Thread #10 started on Thread 24
```

The asynchronous version can start many operations without creating one dedicated blocked thread per operation. While `Task.Delay` is waiting, no thread needs to remain blocked only for that delay.

### Fire-and-Forget Demo

The fire-and-forget demonstration starts an asynchronous operation without awaiting it immediately.

Example output:

```text
Starting fire-and-forget task...
Main method continued without waiting
Fire-and-forget task completed
Fire-and-forget demo finished
```

This demonstrates that the calling method continues without waiting for the asynchronous operation.

Fire-and-forget work must be used carefully because the application may finish before the operation completes and exceptions can be missed if the task is not observed.

### Why Constructor-Based Failure Injection?

The controlled failures are passed into the services through their constructors.

For example:

```csharp
PaymentGateway paymentGateway = new([5], [8]);
```

This keeps testing configuration outside the normal service logic.

The payment gateway does not contain logic such as:

```csharp
if (orderId == 5)
```

to decide which order should fail.

The same approach is used for database-save failures and email failures.

### Compensation

Checkout contains operations that cannot be rolled back automatically as one transaction because payment, stock, files, and email are separate operations.

For this reason, compensation is performed manually.

Examples:

- Payment fails → release stock
- Payment times out → release stock
- Database save fails after payment → refund payment and release stock
- Run cancellation after payment → refund payment and release stock
- Email fails → keep the paid order `Completed` and retry or queue the email

This keeps the final JSON data consistent even when part of the checkout workflow fails.

### Cancellation

All asynchronous operations receive a `CancellationToken`.

The program supports cancellation using:

```text
Ctrl+C
```

or:

```bash
dotnet run -- phase3 --cancel-after 4000
```

Cancellation is propagated through asynchronous delays and file operations.

A cancelled checkout should stop at its next cancellable operation, restore reserved stock, refund an already captured payment, and leave the order in a recoverable state.

### Run Phase 3

```bash
dotnet run -- phase3
```

Run with automatic cancellation:

```bash
dotnet run -- phase3 --cancel-after 4000
```

### Phase 3 Evidence

Screenshots are stored under:

```text
screenshots/Phase03/
```

Sample JSON output is stored under:

```text
sample-output/phase3/
```