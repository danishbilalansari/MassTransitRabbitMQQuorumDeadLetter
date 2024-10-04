using Contracts;
using MassTransit;
using System.IO.Compression;

namespace Consumer
{
    /// <summary>
    /// NotificationStateMachine defines the saga's state transitions and events.
    /// </summary>
    public class NotificationStateMachine : MassTransitStateMachine<NotificationSagaState>
    {
        public State Sent { get; private set; } // The state indicating that the message has been sent
        public Event<SendNotification> SendNotificationEvent { get; private set; } // The event triggered when a notification is sent

        public NotificationStateMachine()
        {
            InstanceState(x => x.CurrentState); // Define the current state of the saga

            // Define the event to handle incoming SendNotification messages
            Event(() => SendNotificationEvent, x => x.CorrelateById(context => context.Message.CorrelationId));

            // Define the state transitions for the saga
            Initially(
                When(SendNotificationEvent)
                    .Then(context =>
                    {
                        // Decompress the message and log it to the console
                        var decompressedMessage = Decompress(context.Data.CompressedMessage);
                        context.Instance.NotificationMessage = decompressedMessage;
                        Console.WriteLine($"[Saga] Notification Received: {decompressedMessage}");
                    })
                    .TransitionTo(Sent) // Move to the "Sent" state once the message is processed
            );

            SetCompletedWhenFinalized(); // Mark the saga as complete once it's done
        }

        /// <summary>
        /// Decompresses a byte array message using GZip compression to get the original message.
        /// </summary>
        /// <param name="compressed">The compressed byte array</param>
        /// <returns>Decompressed string message</returns>
        public static string Decompress(byte[] compressed)
        {
            using (var memoryStream = new MemoryStream(compressed))
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzipStream))
            {
                return reader.ReadToEnd(); // Return the decompressed message
            }
        }
    }
}
