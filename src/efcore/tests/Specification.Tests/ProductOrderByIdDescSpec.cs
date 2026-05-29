using Light.Specification;
namespace Specification.Tests;

internal class ProductOrderByIdDescSpec : Specification<Product>
{
    public ProductOrderByIdDescSpec()
    {
        OrderByDescending(x => (object)x.Id);
    }
}