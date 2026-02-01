package com.example.asyncawaittask;

import java.util.concurrent.CompletableFuture;

public interface IKettleService {
    CompletableFuture<Boolean> checkKettleStatusAsync();
}