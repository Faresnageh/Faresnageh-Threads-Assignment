# Assignment 02 — Answers

Student name: Fares Nageh Abdelaal

Group / ID: G2

## What is wrong

Many threads try to book tables at the same time

They can check TablesLeft before other threads update it

This causes a Race Condition and makes the final numbers incorrect

We only have 5 tables but the program showed 18 bookings

## How you fixed it

I created a private Lock object

I used lock inside the Book method

Now only one thread can check and update the booking numbers at a time

I kept the same Thread.Sleep and did not change Program.cs

## Console output before

Customer-23 booked a table. Left=4

Customer-33 booked a table. Left=4

Customer-24 booked a table. Left=4

Customer-39 booked a table. Left=2

Customer-40 booked a table. Left=2

Booked=18, TablesLeft=2

## Console output after

Booked=5, TablesLeft=0

## Result

The program now books only 5 tables

The final numbers are correct because lock prevents multiple threads from changing the booking data at the same time
