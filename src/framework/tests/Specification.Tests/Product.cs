namespace Specification.Tests;

public class Product
{
    public int Id { get; set; }
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}