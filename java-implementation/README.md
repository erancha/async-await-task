# Java Tea Making Implementation

This is a Java implementation of the async tea making functionality, demonstrating CompletableFuture usage for asynchronous operations.

## Structure

- `Program.java` - Main entry point
- `Logger.java` - Logging utility
- `IKettleService.java` - Interface for kettle operations
- `KettleService.java` - HTTP-based kettle service implementation
- `TeaMaker.java` - Main tea making orchestration

## Running

```bash
cd java-implementation
mvn exec:java
```

## Features

- Asynchronous water boiling with HTTP kettle status check
- Parallel snack preparation using CompletableFuture
- Proper async/await pattern using CompletableFuture composition