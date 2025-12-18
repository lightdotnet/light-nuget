using Light.Specification;

namespace Specification.Tests
{
    public class SpecificationTests
    {
        private static readonly List<Product> _products =
        [
            new() { Id = 1, ProductName = "Product 1" },
            new() { Id = 2, ProductName = "Product 2" },
            new() { Id = 3, ProductName = "Product 3" },
            new() { Id = 4, ProductName = "Product 4" }
        ];

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void Must_Find_Correct_Record(int id)
        {
            var spec = new ProductByIdSpec(id);

            var result = _products.AsQueryable().Where(spec).First();

            result.Id.ShouldBe(id);
            result.ProductName.ShouldBe($"Product {id}");
        }

        [Test]
        public void Must_Filter_Correct_List()
        {
            var idGreaterThan = 2;

            var spec = new ProductHaveIdGreaterThanSpec(idGreaterThan);

            var result = _products.AsQueryable().Where(spec).ToList();

            foreach (var product in result)
            {
                var isIdValid = product.Id > idGreaterThan;

                isIdValid.ShouldBeTrue();
            }
        }
    }
}
