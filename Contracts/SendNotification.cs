namespace Contracts
{
    /// <summary>
    /// SendNotification class defines the structure of the message sent to RabbitMQ.
    /// </summary>
    public class SendNotification
    {
        public Guid CorrelationId { get; set; } // Unique ID for correlating messages
        public byte[] CompressedMessage { get; set; } // The compressed message content
    }
}