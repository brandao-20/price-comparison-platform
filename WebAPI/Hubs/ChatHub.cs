using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Context;
using WebAPI.Entities;
using WebAPI.Repositories;
using WebAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private readonly IUtilizadorRepository _utilizadorRepository;
        private readonly AppDbContext _context;

        public ChatHub(ChatService chatService, IUtilizadorRepository utilizadorRepository, AppDbContext context)
        {
            _chatService = chatService;
            _utilizadorRepository = utilizadorRepository;
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId != 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(int destinatarioId, string message)
        {
            if (destinatarioId <= 0 || string.IsNullOrWhiteSpace(message) || message.Length > 1000)
                throw new HubException("A recipient and a message of up to 1000 characters are required.");
            var remetenteId = GetUserId();
            var remetente = await _utilizadorRepository.GetByIdAsync(remetenteId);
            if (remetente == null)
            {
                return;
            }

            var mensagem = new Mensagem
            {
                RemetenteId = remetenteId,
                DestinatarioId = destinatarioId,
                Conteudo = message,
                DataEnvio = DateTime.UtcNow
            };

            await _chatService.SendMessageAsync(mensagem);
        }

        private int GetUserId()
        {
            if (!int.TryParse(Context.User?.FindFirst("utilizadorId")?.Value, out var userId) || userId <= 0)
                throw new HubException("The authenticated user is invalid.");
            return userId;
        }

        [Authorize(Roles = "Admin,UserManager")]
        public async Task NotifyPriceChange(int produtoId, decimal preco)
        {
            var favoritos = await _context.Favoritos
                .Where(f => f.ProdutoId == produtoId)
                .Select(f => f.UtilizadorId)
                .ToListAsync();
            foreach (var userId in favoritos)
            {
                await Clients.Group($"user-{userId}").SendAsync("PriceChanged", produtoId, preco);
            }
        }
    }
}
