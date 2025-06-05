public class ChatMessage
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int ProjectId { get; set; }
    public string Role { get; set; } 
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}
