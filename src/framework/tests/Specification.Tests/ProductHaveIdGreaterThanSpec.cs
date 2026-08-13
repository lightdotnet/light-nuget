using Light.Specification;

namespace Specification.Tests;

public class ProductHaveIdGreaterThanSpec : Specification<Product>
{
    public ProductHaveIdGreaterThanSpec(int id)
    {
        Where(x => x.Id > id);
    }
}