# Assignment 05 — Answers

Student name: Fares Nageh Abdelaal

Group / ID: G2

## What is wrong

The problem is a Race Condition

Many threads use Dictionary and Queue at the same time

Dictionary is not thread safe and sometimes throws an exception when threads try to add the same key

The view count can also be incorrect because multiple threads update the same value

Queue is not thread safe and this can cause missing emails or exceptions

## What I changed

I replaced Dictionary with ConcurrentDictionary

I used AddOrUpdate to update the view count safely

I replaced Queue with ConcurrentQueue

I used TryDequeue to remove email jobs safely

I kept lock to protect SentEmails

I did not change Program.cs

## Why a thread safe collection is not always enough

A thread safe collection protects its own operations

But multiple operations that depend on each other can still cause a Race Condition

We need to protect the whole business operation when necessary

## Console output before

The program sometimes throws this exception

```text
System.ArgumentException: An item with the same key has already been added. Key: koshary-bowl
```

The result was not always the same

## Console output after

```text
Views expected=50000, actual=50000
Email jobs placed=20, unique sent=20, total sends=20
```

Exit code: 0

## Result

The program now counts 50000 views correctly

All 20 email jobs were sent without duplicates

No exceptions appeared in the test run