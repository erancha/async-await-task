package com.example.asyncawaittask;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;

public class TeaMaker {
    private final IKettleService kettleService;
    private static final int BOILING_TIME_MS = 3000;

    public TeaMaker(IKettleService kettleService) {
        this.kettleService = kettleService;
    }

    public CompletableFuture<Void> makeTeaAsync() {
        Logger.infoFor(TeaMaker.class, "MakeTeaAsync - START");

        // Step 1: Start boiling water asynchronously
        CompletableFuture<String> boilingWaterTask = boilWaterAsync();

        // Sample: offload CPU-bound snack prep work to CompletableFuture.supplyAsync
        CompletableFuture<Void> snackPreparationTask = CompletableFuture.runAsync(() -> {
            Logger.infoFor(TeaMaker.class, "CompletableFuture.runAsync -> Preparing snacks (CPU-bound work)...");
            try {
                Thread.sleep(20000);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new RuntimeException(e);
            }
            Logger.infoFor(TeaMaker.class, "CompletableFuture.runAsync -> Snacks ready!");
        });

        // Step 2: Put tea in cup (synchronous operation)
        putTeaInCup();

        // Step 3: Wait for water to boil and pour it
        Logger.infoFor(TeaMaker.class, "MakeTeaAsync - before await boilingWaterTask;");
        
        return boilingWaterTask.thenCompose(water -> {
            pourWaterIntoCup(water);
            
            // Step 4: Ensure snacks are ready
            return snackPreparationTask;
        }).thenRun(() -> {
            // Step 5: Final - Serve the cup
            serveCup();
        });
    }

    private CompletableFuture<String> boilWaterAsync() {
        Logger.infoFor(TeaMaker.class, "BoilWaterAsync START - Checking kettle status...");

        // Simulate boiling water with an async I/O operation, making HTTP call to check "smart kettle" status
        return kettleService.checkKettleStatusAsync().thenCompose(kettleOnline -> {
            if (kettleOnline) {
                Logger.infoFor(TeaMaker.class, "BoilWaterAsync - Kettle responded");
                return CompletableFuture.completedFuture("Boiled Water");
            } else {
                Logger.warnFor(TeaMaker.class, "BoilWaterAsync - Kettle offline, using timer fallback");
                return CompletableFuture.supplyAsync(() -> {
                    try {
                        Thread.sleep(BOILING_TIME_MS);
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                        throw new RuntimeException(e);
                    }
                    return "Boiled Water";
                });
            }
        }).thenApply(water -> {
            Logger.infoFor(TeaMaker.class, "BoilWaterAsync END");
            return water;
        });
    }

    private void putTeaInCup() {
        Logger.infoFor(TeaMaker.class, "PutTeaInCup  -> Tea bag placed in cup");
    }

    private void pourWaterIntoCup(String water) {
        Logger.infoFor(TeaMaker.class, String.format("PourWaterIntoCup  -> Pouring %s into cup", water));
    }

    private void serveCup() {
        Logger.infoFor(TeaMaker.class, "ServeCup  -> Cup is ready to serve!");
    }
}