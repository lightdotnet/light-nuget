using Light.Specification;

namespace Specification.Tests
{
    /// <summary>
    /// Tests combining multiple Where calls (AndAlso behavior)
    /// </summary>
    internal class ProductByIdAndNameSpec : Specification<Product>
    {
        public ProductByIdAndNameSpec(int id, string name)
        {
            Where(x => x.Id == id);
            Where(x => x.ProductName == name);
        }
    }
}