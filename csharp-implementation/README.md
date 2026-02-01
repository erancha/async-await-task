# C# Tea Making Implementation

This is a C# implementation of the async tea making functionality, demonstrating Task and async/await patterns for asynchronous operations.

## Structure

- `Program.cs` - Main entry point
- `Logger.cs` - Logging utility
- `IKettleService.cs` - Interface for kettle operations
- `KettleService.cs` - HTTP-based kettle service implementation
- `TeaMaker.cs` - Main tea making orchestration
- `WebScraper.cs` - Web scraping functionality (commented out in Program.cs)
- `KafkaPipeline.cs` - Kafka pipeline demo (commented out in Program.cs)
- `AsyncAwaitTask.csproj` - Project file

## Running

```bash
cd csharp-implementation
dotnet run
```

Or build and run separately:

```bash
cd csharp-implementation
dotnet build
dotnet run --no-build
```

## Features

- Asynchronous water boiling with HTTP kettle status check
- Parallel snack preparation using Task.Run()
- Proper async/await pattern with Task composition
- Thread-safe logging showing different thread IDs for concurrent operations
- Additional components (WebScraper, KafkaPipeline) available but commented out

## Dependencies

- .NET 6.0
- Confluent.Kafka (for KafkaPipeline functionality)
- System.Net.Http (built-in for HTTP operations)