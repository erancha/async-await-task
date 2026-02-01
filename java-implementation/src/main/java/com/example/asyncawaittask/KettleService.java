package com.example.asyncawaittask;

import org.apache.hc.client5.http.async.methods.SimpleHttpRequest;
import org.apache.hc.client5.http.async.methods.SimpleHttpRequests;
import org.apache.hc.client5.http.async.methods.SimpleHttpResponse;
import org.apache.hc.client5.http.impl.async.CloseableHttpAsyncClient;
import org.apache.hc.core5.concurrent.FutureCallback;

import java.util.concurrent.CompletableFuture;

public class KettleService implements IKettleService {
    private final CloseableHttpAsyncClient httpClient;
    private static final int BOILING_TIME_MS = 3000;
    private static final String KETTLE_API_URL = "https://httpbin.org/delay/" + (BOILING_TIME_MS / 1000);

    public KettleService(CloseableHttpAsyncClient httpClient) {
        this.httpClient = httpClient;
    }

    @Override
    public CompletableFuture<Boolean> checkKettleStatusAsync() {
        CompletableFuture<Boolean> future = new CompletableFuture<>();
        
        try {
            SimpleHttpRequest request = SimpleHttpRequests.get(KETTLE_API_URL);
            
            httpClient.execute(request, new FutureCallback<SimpleHttpResponse>() {
                @Override
                public void completed(SimpleHttpResponse result) {
                    future.complete(true); // Kettle is online
                }

                @Override
                public void failed(Exception ex) {
                    Logger.warnFor(KettleService.class,
                            String.format("CheckKettleStatusAsync - Kettle offline because request to %s failed: %s | %s",
                                    KETTLE_API_URL, ex.getClass().getSimpleName(), ex.getMessage()));
                    future.complete(false); // Kettle is offline
                }

                @Override
                public void cancelled() {
                    future.complete(false); // Kettle is offline
                }
            });
        } catch (Exception ex) {
            Logger.warnFor(KettleService.class,
                    String.format("CheckKettleStatusAsync - Kettle offline because request to %s failed: %s | %s",
                            KETTLE_API_URL, ex.getClass().getSimpleName(), ex.getMessage()));
            future.complete(false);
        }
        
        return future;
    }
}