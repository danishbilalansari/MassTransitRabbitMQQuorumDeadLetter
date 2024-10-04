using Consumer;
using Contracts;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SagaConsumer
{
    public class Program
    {
        /// <summary>
        /// Main entry point of the application. Configures the bus and starts the saga consumer.
        /// </summary>
        public static async Task Main(string[] args)
        {
            // Create and build the Host that manages dependency injection and service configuration
            var host = CreateHostBuilder(args).Build();

            // Retrieve the configured MassTransit bus from the service provider
            var busControl = host.Services.GetRequiredService<IBusControl>();

            // Start the bus, which initializes and connects to RabbitMQ
            await busControl.StartAsync();
            try
            {
                Console.WriteLine("Saga Consumer is running..."); // Log the consumer status
                Console.ReadLine(); // Keep the application running, wait for user input to terminate
            }
            finally
            {
                // Stop the bus and gracefully disconnect from RabbitMQ
                await busControl.StopAsync();
            }
        }

        /// <summary>
        /// Host builder method to configure the service collection, add MassTransit, and set up RabbitMQ.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application</param>
        /// <returns>An IHostBuilder configured to run the saga consumer</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register MassTransit and configure it for RabbitMQ and saga state machine
                    services.AddMassTransit(x =>
                    {
                        // Add the saga state machine with in-memory saga state repository
                        x.AddSagaStateMachine<NotificationStateMachine, NotificationSagaState>()
                         .InMemoryRepository(); // Store saga state in-memory for simplicity

                        // Configure RabbitMQ as the transport for MassTransit
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            // Define RabbitMQ host connection details
                            cfg.Host("rabbitmq://localhost", h => { });

                            // Set kebab-case naming convention for queues, exchanges, and routing keys
                            cfg.MessageTopology.SetEntityNameFormatter(new KebabCaseEntityNameFormatter());

                            // Define the receive endpoint for consuming messages from the quorum queue
                            cfg.ReceiveEndpoint("notification-saga-quorum-queue", e =>
                            {
                                e.PrefetchCount = 10; // Limit the number of prefetched messages to improve flow control

                                // Configure the queue as a quorum queue for high availability
                                e.SetQueueArgument("x-queue-type", "quorum");

                                // Configure dead-letter exchange for handling failed message processing
                                e.SetQueueArgument("x-dead-letter-exchange", "dead-letter-exchange");

                                // Set the TTL (time-to-live) for messages in the queue (1 minute in this case)
                                e.SetQueueArgument("x-message-ttl", 60000);

                                // Set the expiration time for the queue (1 hour of inactivity before deletion)
                                e.SetQueueArgument("x-expires", 3600000);

                                // Limit the maximum number of messages in the queue to 1000
                                e.SetQueueArgument("x-max-length", 1000);

                                // Limit the total size of messages in the queue to 10 MB
                                e.SetQueueArgument("x-max-length-bytes", 10485760);

                                // Configure the saga state machine for processing messages
                                e.ConfigureSaga<NotificationSagaState>(context, sagaCfg =>
                                {
                                    // Apply retry policy: retry 3 times with 5-second intervals in case of message failures
                                    sagaCfg.Message<SendNotification>(x => x.UseRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))));
                                });

                                // Enable in-memory outbox pattern to ensure message consistency and prevent message loss
                                e.UseInMemoryOutbox();
                            });
                        });
                    });

                    // Configure MassTransit host options with start and stop timeouts
                    services.Configure<MassTransitHostOptions>(options =>
                    {
                        options.WaitUntilStarted = true; // Wait for the bus to fully start
                        options.StartTimeout = TimeSpan.FromMilliseconds(60000); // Align with the message TTL (1 minute)
                        options.StopTimeout = TimeSpan.FromMilliseconds(3600000); // Align with queue expiration time (1 hour)
                    });
                });
    }
}