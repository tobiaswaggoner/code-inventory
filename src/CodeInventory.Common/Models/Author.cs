namespace CodeInventory.Common.Models;

public class Author
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    public ICollection<Commit> Commits { get; set; } = new List<Commit>();
}