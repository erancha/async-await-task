using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace AsyncAwaitTask
{
   internal sealed class KafkaPipeline
   {
      private const string KafkaBootstrapServers = "ec2-3-71-113-150.eu-central-1.compute.amazonaws.com:19092";
      private const string KafkaTopic = "async-await-task-demo";
      private const int KafkaTopicPartitions = 3;
      private const int KafkaProducerMessageCount = 1_000_000;
      private const int KafkaKeyCount = 10;
      private const int KafkaConsumerCount = KafkaTopicPartitions; //3;
      private const int KafkaProducerBatchSize = 1000;
      private static readonly TimeSpan KafkaDisplayInterval = TimeSpan.FromSeconds(1);
      private static readonly string KafkaConsumerGroupId = $"async-await-task-consumers-{0/*Environment.ProcessId*/}";

      /// <summary>
      /// Orchestrates the end-to-end Kafka demo pipeline: creates/validates the topic, starts producer/consumer workloads,
      /// periodically displays throughput stats, and ensures graceful shutdown.
      /// </summary>
      public async Task RunAsync()
      {
         await EnsureTopicExistsAsync();

         var keyPool = Enumerable.Range(0, KafkaKeyCount)
            .Select(i => $"key-{i:00}")
            .ToArray();

         var keyCounters = new ConcurrentDictionary<string, long>(
            keyPool.Select(k => new KeyValuePair<string, long>(k, 0)));

         var totalConsumed = 0L;
         // Launch per-partition consumers and the UI display loop, each sharing a common cancellation token for orchestrated shutdown.
         using var consumptionCompletedCancellingToken = new CancellationTokenSource();
         using var displayCancellingToken = new CancellationTokenSource();

         var consumerTasks = Enumerable.Range(0, KafkaConsumerCount)
            .Select(workerId => ConsumeLoopAsync(
               workerId,
               keyCounters,
               () => Interlocked.Read(ref totalConsumed),
               count => Interlocked.Add(ref totalConsumed, count),
               consumptionCompletedCancellingToken.Token))
            .ToArray();

         var displayTask = DisplayCountsLoopAsync(
            keyCounters,
            () => Interlocked.Read(ref totalConsumed),
            displayCancellingToken.Token);

         await ProduceMessagesAsync(keyPool);

         // Kick off a combined task for all consumer loops and race it against a strict timeout to avoid hanging forever if consumers stall.
         var allConsumersCompleted = Task.WhenAll(consumerTasks);
         var finishedTask = await Task.WhenAny(allConsumersCompleted, Task.Delay(TimeSpan.FromSeconds(30)));
         if (finishedTask != allConsumersCompleted)
         {
            Logger.WarnFor<KafkaPipeline>("Timed out waiting for consumers, cancelling...");
         }
         consumptionCompletedCancellingToken.Cancel();
         await Task.WhenAll(consumerTasks);

         displayCancellingToken.Cancel();
         await displayTask;

         Logger.InfoFor<KafkaPipeline>("Kafka processing completed.");
         PrintCounters(keyCounters);
      }

      /// <summary>
      /// Creates the Kafka topic if it does not already exist, ensuring the expected partition count is available.
      /// </summary>
      private static async Task EnsureTopicExistsAsync()
      {
         using var adminClient = new AdminClientBuilder(new AdminClientConfig
         {
            BootstrapServers = KafkaBootstrapServers
         }).Build();

         try
         {
            await adminClient.CreateTopicsAsync(new[]
            {
               new TopicSpecification
               {
                  Name = KafkaTopic,
                  NumPartitions = KafkaTopicPartitions,
                  ReplicationFactor = 1 // ReplicationFactor = 1 is appropriate here because we’re connecting to a single Kafka broker (BootstrapServers points to one host). Replication only provides redundancy across multiple brokers; with a single broker there’s nowhere else to place replicas, so specifying a factor greater than 1 would just fail topic creation. Keeping it at 1 avoids unnecessary errors and matches the constraints of the environment.
               }
            });

            Logger.InfoFor<KafkaPipeline>($"Topic '{KafkaTopic}' created with {KafkaTopicPartitions} partitions.");
         }
         catch (CreateTopicsException ex) when (ex.Results.SingleOrDefault()?.Error.Code == ErrorCode.TopicAlreadyExists)
         {
            Logger.InfoFor<KafkaPipeline>($"Topic '{KafkaTopic}' already exists.");
         }
      }

      /// <summary>
      /// Sends the configured number of messages to Kafka using a high-throughput, batched producer.
      /// </summary>
      /// <param name="keyPool">Set of keys used to partition messages across Kafka partitions.</param>
      private static async Task ProduceMessagesAsync(IReadOnlyList<string> keyPool)
      {
         Logger.InfoFor<KafkaPipeline>("Starting producer...");

         using var producer = new ProducerBuilder<string, string>(
            new ProducerConfig
            {
               BootstrapServers = KafkaBootstrapServers,
               ClientId = "async-await-task-producer"
            }).Build();

         var inFlightTasks = new List<Task>(KafkaProducerBatchSize);

         for (var i = 0; i < KafkaProducerMessageCount; i++)
         {
            var key = keyPool[i % keyPool.Count];
            var value = JsonSerializer.Serialize(new
            {
               value1 = $"value-{i}-1",
               value2 = $"value-{i}-2",
               value3 = $"value-{i}-3",
               value4 = $"value-{i}-4",
               value5 = $"value-{i}-5"
            });

            inFlightTasks.Add(producer.ProduceAsync(KafkaTopic, new Message<string, string>
            {
               Key = key,
               Value = value
            }));

            if (inFlightTasks.Count >= KafkaProducerBatchSize)
            {
               await Task.WhenAll(inFlightTasks);
               inFlightTasks.Clear();
            }
         }

         if (inFlightTasks.Count > 0)
         {
            await Task.WhenAll(inFlightTasks);
         }

         producer.Flush(TimeSpan.FromSeconds(5));

         Logger.InfoFor<KafkaPipeline>($"Producer sent {KafkaProducerMessageCount} messages.");
      }

      /// <summary>
      /// Runs a single consumer loop that pulls records, updates per-key counters, and cooperates with cancellation.
      /// </summary>
      /// <param name="workerId">Logical identifier for the consumer instance.</param>
      /// <param name="keyCounters">Shared counter dictionary updated per message.</param>
      /// <param name="getTotal">Callback returning the total processed count.</param>
      /// <param name="addTotal">Callback adding to the total count in a thread-safe way.</param>
      /// <param name="token">Cancellation token that signals shutdown.</param>
      private static Task ConsumeLoopAsync(
         int workerId,
         ConcurrentDictionary<string, long> keyCounters,
         Func<long> getTotal,
         Func<int, long> addTotal,
         CancellationToken token)
      {
         return Task.Run(() =>
         {
            using var consumer = new ConsumerBuilder<string, string>(
               new ConsumerConfig
               {
                  BootstrapServers = KafkaBootstrapServers,
                  GroupId = KafkaConsumerGroupId,
                  EnableAutoCommit = true,
                  AutoOffsetReset = AutoOffsetReset.Earliest
               })
               .Build();

            consumer.Subscribe(KafkaTopic);

            Logger.InfoFor<KafkaPipeline>($"Consumer {workerId} started.");

            try
            {
               while (!token.IsCancellationRequested && getTotal() < KafkaProducerMessageCount)
               {
                  try
                  {
                     var result = consumer.Consume(TimeSpan.FromMilliseconds(250));

                     if (result?.Message is null)
                     {
                        continue;
                     }

                     keyCounters.AddOrUpdate(result.Message.Key, _ => 1, (_, current) => current + 1);
                     var total = addTotal(1);

                     if (total >= KafkaProducerMessageCount)
                     {
                        break;
                     }
                  }
                  catch (ConsumeException ex)
                  {
                     Logger.ErrorFor<KafkaPipeline>($"Consume error (consumer {workerId})", ex);
                  }
               }
            }
            finally
            {
               consumer.Close();
               Logger.InfoFor<KafkaPipeline>($"Consumer {workerId} stopped.");
            }
         }, token);
      }

      /// <summary>
      /// Periodically prints the per-key and total counts until cancellation is requested.
      /// </summary>
      /// <param name="keyCounters">Shared counter dictionary to snapshot.</param>
      /// <param name="getTotal">Callback returning current overall count.</param>
      /// <param name="token">Cancellation token to stop the loop.</param>
      private static async Task DisplayCountsLoopAsync(
         ConcurrentDictionary<string, long> keyCounters,
         Func<long> getTotal,
         CancellationToken token)
      {
         try
         {
            PrintCounters(keyCounters, getTotal());

            while (!token.IsCancellationRequested)
            {
               await Task.Delay(KafkaDisplayInterval, token);
               PrintCounters(keyCounters, getTotal());
            }
         }
         catch (OperationCanceledException)
         {
            // Expected during shutdown.
         }
         finally
         {
            PrintCounters(keyCounters, getTotal());
         }
      }

      /// <summary>
      /// Emits the current per-key counts plus the aggregate total in a single log line.
      /// </summary>
      /// <param name="keyCounters">Source counter dictionary.</param>
      /// <param name="totalOverride">Optional externally computed total.</param>
      private static void PrintCounters(
         ConcurrentDictionary<string, long> keyCounters,
         long? totalOverride = null)
      {
         var snapshot = keyCounters
            .OrderBy(kvp => kvp.Key)
            .ToList();

         var total = totalOverride ?? snapshot.Sum(entry => entry.Value);

         var perKeyLine = string.Join(", ", snapshot.Select(entry => $"{entry.Value,-6}"));

         Logger.InfoFor<KafkaPipeline>($"{perKeyLine} | Total: {total,-6}");
      }
   }
}

