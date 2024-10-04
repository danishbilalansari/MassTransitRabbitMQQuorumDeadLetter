using Contracts;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO.Compression;

namespace Producer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var busControl = host.Services.GetRequiredService<IBusControl>();

            await busControl.StartAsync(); // Start the MassTransit bus

            try
            {
                // Create a send endpoint targeting the topic exchange.
                var sendEndpoint = await busControl.GetSendEndpoint(new Uri("rabbitmq://localhost/notification-exchange"));

                // Send a batch of 10 compressed notification messages.
                for (int i = 0; i < 10; i++)
                {
                    var message = $"Hello quorum queue with saga, Message {i}";
                    var compressedMessage = Compress(message); // Compress the message content

                    // Send the compressed message as part of the SendNotification class.
                    await sendEndpoint.Send(new SendNotification
                    {
                        CorrelationId = Guid.NewGuid(),
                        CompressedMessage = compressedMessage
                    });

                    Console.WriteLine($"Sent message {i}"); // Log message to console
                }
            }
            finally
            {
                await busControl.StopAsync(); // Stop the bus when done
            }
        }

        /// <summary>
        /// Compresses a string message using GZip compression to reduce size.
        /// </summary>
        /// <param name="str">The string to compress</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] Compress(string str)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                using (var writer = new StreamWriter(gzipStream))
                {
                    writer.Write(str); // Write the string to the GZip stream
                }
                return memoryStream.ToArray(); // Return the compressed byte array
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMassTransit(x =>
                    {
                        // Configure RabbitMQ with the required host and exchange setup.
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.Host("rabbitmq://localhost", h => { }); // Connect to RabbitMQ instance

                            // Set kebab-case naming convention for RabbitMQ message topology.
                            cfg.MessageTopology.SetEntityNameFormatter(new KebabCaseEntityNameFormatter());
                        });
                    });

                    // Add the MassTransit hosted service, so the bus is started and stopped correctly.
                    services.AddMassTransitHostedService();
                });
    }
}
