# Assignment 01 — Answers

Student name: Fares Nageh Abdelaal

Group / ID: G2

## Problems with the current design

The program was using Thread.Sleep which blocks the thread while waiting

It also processed the invoices one by one so when we have many orders the program will take more time

## What you changed

I replaced Thread.Sleep with Task.Delay and used async and await

I used File.ReadAllTextAsync and File.WriteAllTextAsync

I created a new method to process one invoice

Then I started all the tasks and awaited them one by one

The program still creates the same five invoices

## Console output before

Export finished in 6.0 s

## Console output after

Export finished in 1.8 s

The execution time became much faster because the invoices can be processed concurrently