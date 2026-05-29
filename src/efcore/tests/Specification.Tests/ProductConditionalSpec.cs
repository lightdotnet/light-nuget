using Light.Specification;

namespace Specification.Tests
{
    /// <summary>
    /// Tests WhereIf conditional filtering
    /// </summary>
    internal class ProductConditionalSpec : Specification<Product>
    {
        public ProductConditionalSpec(int? minId = null, string? name = null)
        {
            WhereIf(minId.HasValue, x => x.Id > minId!.Value);
            WhereIf(name != null, x => x.ProductName == name);
        }
    }
}