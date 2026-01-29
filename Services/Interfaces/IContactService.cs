using Fruitables.Models;

namespace Fruitables.Services.Interfaces;

public interface IContactService
{
    Task<ContactMessage> SendMessageAsync(string name, string email, string message);
    Task<List<ContactMessage>> GetAllMessagesAsync();
    Task MarkAsReadAsync(int id);
}
