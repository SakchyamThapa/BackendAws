public interface IChatRepository
{
    Task<IEnumerable<ChatMessage>> GetChatHistoryAsync(int projectId, string userId);
    Task SaveChatMessageAsync(ChatMessage message);
}
