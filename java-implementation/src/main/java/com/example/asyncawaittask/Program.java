package com.example.asyncawaittask;

import org.apache.hc.client5.http.impl.async.CloseableHttpAsyncClient;
import org.apache.hc.client5.http.impl.async.HttpAsyncClients;

import java.time.Duration;
import java.time.Instant;
import java.util.concurrent.CompletableFuture;

public class Program {
    public static void main(String[] args) {
        try (CloseableHttpAsyncClient httpClient = HttpAsyncClients.createDefault()) {
            httpClient.start();
            
            Instant start = Instant.now();

            KettleService kettleService = new KettleService(httpClient);
            TeaMaker teaMaker = new TeaMaker(kettleService);

            Logger.info("=== Tea Making Process ===", "Program");
            
            CompletableFuture<Void> teaMakingTask = teaMaker.makeTeaAsync();
            teaMakingTask.join(); // Wait for completion
            
            Duration elapsed = Duration.between(start, Instant.now());
            Logger.info(String.format("Total elapsed time: %.2fs", elapsed.toMillis() / 1000.0), "Program");
            
        } catch (Exception e) {
            Logger.errorFor(Program.class, "Error in main", e);
        }
    }
}