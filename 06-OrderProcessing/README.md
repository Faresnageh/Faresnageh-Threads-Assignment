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