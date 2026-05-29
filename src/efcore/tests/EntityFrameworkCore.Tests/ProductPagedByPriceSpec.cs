using Light.Specification;
namespace EntityFrameworkCore.Tests;

internal class ProductPagedByPriceSpec : Specification<Product>
{
    public ProductPagedByPriceSpec(int skip, int take)
    {
        Where(x => x.IsActive);
        OrderBy(x => (object)x.Price);
        ApplyPaging(skip, take);
    }
}