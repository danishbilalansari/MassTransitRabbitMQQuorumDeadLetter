using MassTransit;

namespace Consumer
{
    /// <summary>
    /// NotificationSagaState represents the state of the saga that handles notifications.
    /// </summary>
    public class NotificationSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; } // Unique identifier for the saga instance
        public string CurrentState { get; set; } // The current state of the saga
        public string NotificationMessage { get; set; } // The decompressed notification message
    }
}
