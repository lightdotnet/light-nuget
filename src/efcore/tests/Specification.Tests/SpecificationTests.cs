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

        #region [Original Tests]

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

        #endregion

        #region [Specification — Expression Combining (AndAlso)]

        [Test]
        public void CombinedSpec_Should_Match_Both_Conditions()
        {
            var spec = new ProductByIdAndNameSpec(1, "Product 1");

            var result = _products.AsQueryable().Where(spec).ToList();

            result.ShouldHaveCount(1);
            result[0].Id.ShouldBe(1);
            result[0].ProductName.ShouldBe("Product 1");
        }

        [Test]
        public void CombinedSpec_Should_Return_Empty_When_No_Match()
        {
            var spec = new ProductByIdAndNameSpec(1, "Product 2"); // Id=1 but Name="Product 2" → no match

            var result = _products.AsQueryable().Where(spec).ToList();

            result.ShouldHaveCount(0);
        }

        #endregion

        #region [Specification — WhereIf]

        [Test]
        public void WhereIf_True_Should_Apply_Filter()
        {
            var spec = new ProductConditionalSpec(minId: 2);

            var result = _products.AsQueryable().Where(spec).ToList();

            result.ShouldHaveCount(2); // Id 3, 4
            foreach (var p in result)
            {
                p.Id.ShouldBeGreaterThan(2);
            }
        }

        [Test]
        public void WhereIf_False_Should_Not_Apply_Filter()
        {
            var spec = new ProductConditionalSpec(); // no conditions → all pass

            var result = _products.AsQueryable().Where(spec).ToList();

            result.ShouldHaveCount(4);
        }

        [Test]
        public void WhereIf_Multiple_Conditions()
        {
            var spec = new ProductConditionalSpec(minId: 0, name: "Product 3");

            var result = _products.AsQueryable().Where(spec).ToList();

            result.ShouldHaveCount(1);
            result[0].Id.ShouldBe(3);
        }

        #endregion

        #region [Specification — CompiledExpression]

        [Test]
        public void CompiledExpression_Should_Be_Null_When_No_Where()
        {
            var spec = new EmptySpec();

            spec.Expression.ShouldBeNull();
            spec.CompiledExpression.ShouldBeNull();
        }

        [Test]
        public void CompiledExpression_Should_Not_Be_Null_After_Where()
        {
            var spec = new ProductByIdSpec(1);

            spec.Expression.ShouldNotBeNull();
            spec.CompiledExpression.ShouldNotBeNull();
        }

        [Test]
        public void CompiledExpression_Should_Cache()
        {
            var spec = new ProductByIdSpec(1);

            var first = spec.CompiledExpression;
            var second = spec.CompiledExpression;

            // Same reference = cached
            Assert.That(first, Is.SameAs(second));
        }

        #endregion

        #region [Specification — Null / Empty Expression]

        [Test]
        public void Empty_Spec_QueryableWhere_Should_Return_All()
        {
            var spec = new EmptySpec();

            var result = _products.AsQueryable().Where(spec).ToList();

            result.ShouldHaveCount(4);
        }

        [Test]
        public void Null_Specification_Expression_Should_Return_All_Items()
        {
            ISpecification<Product> spec = new EmptySpec();

            var result = _products.AsQueryable().Where(spec).ToList();

            result.ShouldHaveCount(4);
        }

        #endregion

        #region [CollectionExtensions]

        [Test]
        public void CollectionExtensions_Where_Should_Filter()
        {
            var spec = new ProductByIdSpec(2);

            var result = ((IEnumerable<Product>)_products).Where(spec).ToList();

            result.ShouldHaveCount(1);
            result[0].Id.ShouldBe(2);
        }

        [Test]
        public void CollectionExtensions_WhereIf_True_Should_Filter()
        {
            var spec = new ProductHaveIdGreaterThanSpec(2);

            var result = ((IEnumerable<Product>)_products).WhereIf(true, spec).ToList();

            result.ShouldHaveCount(2);
        }

        [Test]
        public void CollectionExtensions_WhereIf_False_Should_Return_All()
        {
            var spec = new ProductHaveIdGreaterThanSpec(2);

            var result = ((IEnumerable<Product>)_products).WhereIf(false, spec).ToList();

            result.ShouldHaveCount(4);
        }

        [Test]
        public void CollectionExtensions_Empty_Spec_Should_Return_All()
        {
            var spec = new EmptySpec();

            var result = ((IEnumerable<Product>)_products).Where(spec).ToList();

            result.ShouldHaveCount(4);
        }

        #endregion

        #region [QueryableExtensions — WhereIf]

        [Test]
        public void QueryableExtensions_WhereIf_True_Should_Filter()
        {
            var result = _products.AsQueryable()
                .WhereIf(true, x => x.Id > 2)
                .ToList();

            result.ShouldHaveCount(2);
        }

        [Test]
        public void QueryableExtensions_WhereIf_False_Should_Return_All()
        {
            var result = _products.AsQueryable()
                .WhereIf(false, x => x.Id > 2)
                .ToList();

            result.ShouldHaveCount(4);
        }

        [Test]
        public void QueryableExtensions_WhereIf_Spec_True_Should_Filter()
        {
            var spec = new ProductHaveIdGreaterThanSpec(2);

            var result = _products.AsQueryable()
                .WhereIf(true, spec)
                .ToList();

            result.ShouldHaveCount(2);
        }

        [Test]
        public void QueryableExtensions_WhereIf_Spec_False_Should_Return_All()
        {
            var spec = new ProductHaveIdGreaterThanSpec(2);

            var result = _products.AsQueryable()
                .WhereIf(false, spec)
                .ToList();

            result.ShouldHaveCount(4);
        }

        #endregion
    }
}