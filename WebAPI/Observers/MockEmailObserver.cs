using WebAPI.Entities;

namespace WebAPI.Observers
{
    public class MockEmailObserver(ILogger<MockEmailObserver> logger) : IMessageObserver
    {
        public async Task NotifyAsync(Mensagem mensagem)
        {
            // Demonstration observer only; no email is sent.
            logger.LogDebug("Mock email notification observed; no email was sent.");
            await Task.CompletedTask;
        }
    }
}
