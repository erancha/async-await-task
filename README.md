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
=== Tea Making Process ===

[15:39:05.947] [thread #1] BoilWaterAsync START - Checking kettle status...
[15:39:06.033] [thread #1] PutTeaInCup  -> Tea bag placed in cup
[15:39:19.138] [thread #9] BoilWaterAsync - Kettle responded
[15:39:19.138] [thread #9] BoilWaterAsync END
[15:39:19.139] [thread #9] PourWaterIntoCup  -> Pouring Boiled Water into cup
[15:39:19.139] [thread #9] ServeCup  -> Cup is ready to serve!

=== Tea is ready! ===
```

## Output Analysis

| Timestamp     | Thread | Event           | Explanation                                    |
| ------------- | ------ | --------------- | ---------------------------------------------- |
| 05.947        | #1     | Boiling starts  | HTTP request initiated, returns immediately    |
| 06.033        | #1     | Tea in cup      | Thread #1 continues while water boils          |
| 19.138        | #9     | Kettle responds | ~13s later, HTTP response arrives on thread #9 |
| 19.138-19.139 | #9     | Pour & serve    | Remaining steps execute on thread #9           |

**Key Observation**: Thread #1 is not blocked during the 13-second wait. The async operation releases the thread, and execution resumes on thread #9 when the I/O completes.

**Why Thread #9 instead of Thread #1?** When `await` releases Thread #1, it returns to the thread pool and becomes available for other work. When the HTTP response arrives 13 seconds later, the thread pool assigns **any available thread** to run the continuation - in this case, Thread #9. The runtime doesn't guarantee you'll get the same thread back; it uses whatever thread is available, maximizing thread pool efficiency. Thread #1 could theoretically be reused, but the thread pool scheduler decides based on availability and load balancing.

## Running the Demo

```bash
dotnet run
```

## Configuration

Adjust boiling time in [TeaMaker.cs](TeaMaker.cs):

```csharp
private const int BoilingTimeMs = 3000;  // 3 seconds server delay (not including network latency)
```
