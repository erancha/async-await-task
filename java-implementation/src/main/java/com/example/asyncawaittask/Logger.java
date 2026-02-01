package com.example.asyncawaittask;

import java.time.LocalTime;
import java.time.format.DateTimeFormatter;

public class Logger {
    private static final DateTimeFormatter TIME_FORMATTER = DateTimeFormatter.ofPattern("HH:mm:ss.SSS");

    public static void info(String message, String category) {
        write("INF", message, category);
    }

    public static void info(String message) {
        info(message, null);
    }

    public static void warn(String message, String category) {
        write("WRN", message, category);
    }

    public static void warn(String message) {
        warn(message, null);
    }

    public static void error(String message, String category, Exception exception) {
        String formatted = exception == null
                ? message
                : String.format("%s | %s: %s", message, exception.getClass().getSimpleName(), exception.getMessage());
        write("ERR", formatted, category);
    }

    public static void error(String message, String category) {
        error(message, category, null);
    }

    public static void error(String message) {
        error(message, null, null);
    }

    public static <T> void infoFor(Class<T> clazz, String message) {
        info(message, clazz.getSimpleName());
    }

    public static <T> void warnFor(Class<T> clazz, String message) {
        warn(message, clazz.getSimpleName());
    }

    public static <T> void errorFor(Class<T> clazz, String message, Exception exception) {
        error(message, clazz.getSimpleName(), exception);
    }

    public static <T> void errorFor(Class<T> clazz, String message) {
        errorFor(clazz, message, null);
    }

    private static void write(String level, String message, String category) {
        String timestamp = LocalTime.now().format(TIME_FORMATTER);
        long threadId = Thread.currentThread().getId();
        String source = category != null ? category : "General";
        System.out.printf("[%s] [thread #%-2d] [%s] [%s] %s%n", timestamp, threadId, level, source, message);
    }
}