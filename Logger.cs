using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AsyncAwaitTask
{
    internal static class Logger
    {
        public static void Info(string message, string? category = null)
            => Write("INF", message, category);

        public static void Warn(string message, string? category = null)
            => Write("WRN", message, category);

        public static void Error(string message, string? category = null, Exception? exception = null)
        {
            var formatted = exception is null
                ? message
                : $"{message} | {exception.GetType().Name}: {exception.Message}";
            Write("ERR", formatted, category);
        }

        public static void InfoFor<T>(string message)
            => Info(message, typeof(T).Name);

        public static void WarnFor<T>(string message)
            => Warn(message, typeof(T).Name);

        public static void ErrorFor<T>(string message, Exception? exception = null)
            => Error(message, typeof(T).Name, exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Write(string level, string message, string? category)
        {
            var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var source = category ?? "General";
            Console.WriteLine($"[{timestamp}] [thread #{threadId,-2}] [{level}] [{source}] {message}");
        }
    }
}

