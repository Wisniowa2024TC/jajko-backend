namespace JajkoBackend.Models;

public class Category
{
    public int Id { get; set; }
    public int? ParentCategory { get; set; }
    public string Name { get; set; } = string.Empty;
}