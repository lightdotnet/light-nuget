using Light.Specification;
namespace EntityFrameworkCore.Tests;

internal class ProductOrderByPriceDescSpec : Specification<Product>
{
    public ProductOrderByPriceDescSpec()
    {
        OrderByDescending(x => (object)x.Price);
    }
}