using Light.Specification;

namespace Specification.Tests
{
    internal class ProductByIdSpec : Specification<Product>
    {
        public ProductByIdSpec(int id)
        {
            Where(x => x.Id == id);
        }
    }
}
