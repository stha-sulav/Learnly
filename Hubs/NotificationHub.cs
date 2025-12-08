using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Learnly.Hubs
{
    public class NotificationHub : Hub
    {
        // Optional: Methods for clients to subscribe/unsubscribe to specific groups or topics
        // For simplicity, we'll primarily use Clients.User(userId).SendAsync from the server.

        public async Task SubscribeUser(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            // Optionally, send any pending notifications to the newly subscribed user
        }

        public async Task UnsubscribeUser(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        // The server-side will typically call methods like:
        // Clients.User(userId).SendAsync("ReceiveNotification", payload);
        // or
        // Clients.Group(groupId).SendAsync("ReceiveNotification", payload);
        // or
        // Clients.All.SendAsync("ReceiveNotification", payload);
    }
}
