namespace Sample.AspNetCore.Modules;

public class ProductModuleService
{
    public string GetProductId => Guid.NewGuid().ToString();

    public List<int> Products { get; set; } = [];

    public void AddProduct(int productId) => Products.Add(productId);
}
