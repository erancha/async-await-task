# Async/Await Tea Maker Demo

A C# demonstration of async/await with Task, showing thread behavior and timing during asynchronous operations.

## Table of Contents

- [What It Does](#what-it-does)
- [Key Concepts](#key-concepts-demonstrated)
- [Expected Output](#expected-output)
- [Output Analysis](#output-analysis)
- [Running the Demo](#running-the-demo)
- [Configuration](#configuration)

## What It Does

Simulates making tea with these steps:

1. **Boils water asynchronously** - Makes an HTTP call to a smart kettle API
2. **Puts tea in cup** - Runs synchronously while water is boiling
3. **Pours water** - Waits for water to finish boiling, then pours
4. **Serves the cup** - Final step

## Key Concepts

- **Non-blocking async operations** - A thread starts boiling water and immediately continues to put tea in cup
- **Thread switching** - After `await`, execution resumes on a [potentially different](#output-analysis) thread (thread pool)
- **Real async I/O** - Uses `HttpClient.GetStringAsync()` to simulate an asynchronous I/O operation to the kettle
- **Task as Promise** - `Task<string>` works like JavaScript's `Promise<string>`

## Expected Output

```
[16:45:30.329] [thread #1 ] [INF] [Program] === Tea Making Process ===
[16:45:30.346] [thread #1 ] [INF] [TeaMaker] MakeTeaAsync - START
[16:45:30.347] [thread #1 ] [INF] [TeaMaker] BoilWaterAsync START - Checking kettle status...
[16:45:30.415] [thread #1 ] [INF] [TeaMaker] PutTeaInCup  -> Tea bag placed in cup
[16:45:30.417] [thread #1 ] [INF] [TeaMaker] MakeTeaAsync - before await boilingWaterTask;
[16:45:30.422] [thread #7 ] [INF] [TeaMaker] Task.Run -> Preparing snacks (CPU-bound work)...
[16:45:48.303] [thread #10] [INF] [TeaMaker] BoilWaterAsync - Kettle responded
[16:45:48.307] [thread #10] [INF] [TeaMaker] BoilWaterAsync END
[16:45:48.308] [thread #10] [INF] [TeaMaker] PourWaterIntoCup  -> Pouring Boiled Water into cup
[16:45:50.427] [thread #7 ] [INF] [TeaMaker] Task.Run -> Snacks ready!
[16:45:50.428] [thread #7 ] [INF] [TeaMaker] ServeCup  -> Cup is ready to serve!
[16:45:50.429] [thread #7 ] [INF] [Program] Total elapsed time: 20.10s
```

## Output Analysis

| Timestamp     | Thread | Event                 | Explanation                                                    |
| ------------- | ------ | --------------------- | -------------------------------------------------------------- |
| 30.347        | #1     | Boiling starts        | HTTP request initiated, returns immediately                    |
| 30.415        | #1     | Tea in cup            | Thread #1 continues work while water boils                     |
| 30.422        | #7     | Snack prep starts     | CPU-bound work offloaded to another thread via `Task.Run`      |
| 48.303        | #10    | Kettle responds       | ~18s later, HTTP response arrives on thread #10                |
| 48.308        | #10    | Pour boiling water    | Continuation runs on thread #10 after `await` completes        |
| 50.427        | #7     | Snacks ready & serve  | Snack task finishes, final serving happens on the same thread  |

**Key Observation**: Thread #1 is not blocked during the wait. It hands control back to the thread pool, allowing other work (like snack prep) to progress while the smart kettle HTTP call runs asynchronously.

**Why Thread #10 instead of Thread #1?** When `await` releases Thread #1, it returns to the thread pool and becomes available for other work. When the HTTP response arrives ~18 seconds later, the thread pool assigns **any available thread**—thread #10—to run the continuation. The runtime doesn't guarantee you'll get the same thread back; it picks whichever worker is free, maximizing efficiency.

## Running the Demo

```bash
dotnet run
```

## Configuration

Adjust boiling time in [TeaMaker.cs](TeaMaker.cs):

```csharp
private const int BoilingTimeMs = 3000;  // 3 seconds server delay (not including network latency)
```
