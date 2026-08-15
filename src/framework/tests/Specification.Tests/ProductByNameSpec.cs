using Light.Specification;

namespace Specification.Tests;

public class ProductByNameSpec : Specification<Product>
{
    public ProductByNameSpec(string name)
    {
        Where(x => x.ProductName == name);
    }
}