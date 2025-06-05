using SonicPoints.Data;
using Microsoft.EntityFrameworkCore;

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _context;

    public ChatRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ChatMessage>> GetChatHistoryAsync(int projectId, string userId)
    {
        return await _context.ChatMessages
            .Where(m => m.ProjectId == projectId && m.UserId == userId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task SaveChatMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
    }
}
