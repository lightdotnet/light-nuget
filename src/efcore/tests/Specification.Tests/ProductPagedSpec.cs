using Light.Specification;
namespace Specification.Tests;

internal class ProductPagedSpec : Specification<Product>
{
    public ProductPagedSpec(int skip, int take)
    {
        OrderBy(x => (object)x.Id);
        ApplyPaging(skip, take);
    }
}