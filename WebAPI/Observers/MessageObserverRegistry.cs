using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;
using WebAPI.Services;
using WebAPI.Repositories;

namespace WebAPI.Observers
{
    public class MessageObserverRegistry : IMessageObserverRegistry
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MockEmailObserver> _emailLogger;
        private readonly IUtilizadorRepository _utilizadorRepository;

        public MessageObserverRegistry(IHubContext<ChatHub> hubContext, IUtilizadorRepository utilizadorRepository, ILogger<MockEmailObserver> emailLogger)
        {
            _emailLogger = emailLogger;
            _hubContext = hubContext;
            _utilizadorRepository = utilizadorRepository;
        }

        public void RegisterObservers(ChatService chatService)
        {
            chatService.RegisterObserver(new SignalRObserver(_hubContext, _utilizadorRepository));
            chatService.RegisterObserver(new MockEmailObserver(_emailLogger));
        }
    }
}
