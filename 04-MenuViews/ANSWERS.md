# Assignment 04 — Answers

Student name: Fares Nageh Abdelaal

Group / ID: G2


## What is wrong

The problem is a Race Condition

Views++ is not an atomic operation

It reads the value then adds 1 then writes the new value

Two threads can read the same value and some updates can be lost

## What I changed

I replaced Views++ with Interlocked.Increment(ref Views)

This makes the increment operation atomic

Now multiple threads can update the counter safely

## When would I use another way

I would use lock if I need to protect multiple operations or update more than one shared variable together

Interlocked is better here because we only need to increment one counter

## Console output before

Expected: 400000

Actual: 126661

## Console output after

Expected: 400000

Actual: 400000

## Result

The result is now correct because Interlocked prevents lost updates