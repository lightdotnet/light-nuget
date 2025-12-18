using Light.Specification;

namespace Specification.Tests
{
    internal class ProductHaveIdGreaterThanSpec : Specification<Product>
    {
        public ProductHaveIdGreaterThanSpec(int id)
        {
            Where(x => x.Id > id);
        }
    }
}
