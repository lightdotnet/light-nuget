using Light.Specification;
namespace Specification.Tests;

internal class ProductOrderByNameThenIdSpec : Specification<Product>
{
    public ProductOrderByNameThenIdSpec()
    {
        OrderBy(x => x.ProductName!);
        OrderBy(x => (object)x.Id);
    }
}