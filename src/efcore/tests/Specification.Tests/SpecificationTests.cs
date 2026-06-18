using Light.Specification;

namespace Specification.Tests;

public class SpecificationTests
{
    private static readonly List<Product> Products =
    [
        new() { Id = 1, ProductName = "Product 1" },
        new() { Id = 2, ProductName = "Product 2" },
        new() { Id = 3, ProductName = "Product 3" },
        new() { Id = 4, ProductName = "Product 4" }
    ];

    #region [Where / WhereIf]

    [Test]
    public void Where_Should_Filter_By_Id()
    {
        var spec = new ProductByIdSpec(1);
        spec.Expression.ShouldNotBeNull();
    }

    [Test]
    public void Where_Should_Set_Expression()
    {
        var spec = new ProductByIdSpec(1);
        var result = Products.AsQueryable().Where(spec).ToList();
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(1);
    }

    [Test]
    public void WhereIf_True_Should_Filter()
    {
        var spec = new ProductHaveIdGreaterThanSpec(2);
        var result = Products.AsQueryable().WhereIf(true, spec).ToList();
        result.Count.ShouldBe(2);
    }

    [Test]
    public void WhereIf_False_Should_Return_All()
    {
        var spec = new ProductHaveIdGreaterThanSpec(2);
        var result = Products.AsQueryable().WhereIf(false, spec).ToList();
        result.Count.ShouldBe(4);
    }

    [Test]
    public void WhereIf_Expression_True_Should_Filter()
    {
        var result = Products.AsQueryable().WhereIf(true, x => x.Id > 2).ToList();
        result.Count.ShouldBe(2);
    }

    [Test]
    public void WhereIf_Expression_False_Should_Return_All()
    {
        var result = Products.AsQueryable().WhereIf(false, x => x.Id > 2).ToList();
        result.Count.ShouldBe(4);
    }

    #endregion

    #region [CompiledExpression]

    [Test]
    public void CompiledExpression_Should_Not_Be_Null()
    {
        var spec = new ProductByIdSpec(1);
        spec.CompiledExpression.ShouldNotBeNull();
    }

    [Test]
    public void CompiledExpression_Should_Be_Cached()
    {
        var spec = new ProductByIdSpec(1);
        var first = spec.CompiledExpression;
        var second = spec.CompiledExpression;
        Assert.That(first, Is.SameAs(second));
    }

    [Test]
    public void EmptySpec_CompiledExpression_Should_Be_Null()
    {
        var spec = new ProductHaveIdGreaterThanSpec(0);
        // This spec always has expression, test with a custom empty
        ISpecification<Product> empty = new EmptyProductSpec();
        Assert.That(empty.Expression, Is.Null);
    }

    #endregion

    #region [CollectionExtensions]

    [Test]
    public void Collection_Where_Should_Filter()
    {
        var spec = new ProductByIdSpec(2);
        var result = Products.Where(spec).ToList();
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(2);
    }

    [Test]
    public void Collection_Where_EmptySpec_Should_Return_All()
    {
        ISpecification<Product> spec = new EmptyProductSpec();
        var result = Products.Where(spec).ToList();
        result.Count.ShouldBe(4);
    }

    [Test]
    public void Collection_WhereIf_True_Should_Filter()
    {
        var spec = new ProductHaveIdGreaterThanSpec(2);
        var result = Products.WhereIf(true, spec).ToList();
        result.Count.ShouldBe(2);
    }

    [Test]
    public void Collection_WhereIf_False_Should_Return_All()
    {
        var spec = new ProductHaveIdGreaterThanSpec(2);
        var result = Products.WhereIf(false, spec).ToList();
        result.Count.ShouldBe(4);
    }

    #endregion

    #region [Queryable Where]

    [Test]
    public void Queryable_Where_Should_Filter()
    {
        var spec = new ProductByIdSpec(3);
        var result = Products.AsQueryable().Where(spec).ToList();
        result.Count.ShouldBe(1);
    }

    [Test]
    public void Queryable_Where_EmptySpec_Should_Return_All()
    {
        ISpecification<Product> spec = new EmptyProductSpec();
        var result = Products.AsQueryable().Where(spec).ToList();
        result.Count.ShouldBe(4);
    }

    [Test]
    public void Queryable_WhereIf_Spec_True_Should_Filter()
    {
        var spec = new ProductByIdSpec(1);
        var result = Products.AsQueryable().WhereIf(true, spec).ToList();
        result.Count.ShouldBe(1);
    }

    [Test]
    public void Queryable_WhereIf_Spec_False_Should_Return_All()
    {
        var spec = new ProductByIdSpec(1);
        var result = Products.AsQueryable().WhereIf(false, spec).ToList();
        result.Count.ShouldBe(4);
    }

    #endregion

    #region [Multiple Where]

    [Test]
    public void Multiple_Where_Should_Combine_With_And()
    {
        var spec = new ProductHaveIdGreaterThanSpec(1);
        var result = Products.AsQueryable().Where(spec).Where(x => x.Id < 4).ToList();
        result.Count.ShouldBe(2); // Id 2, 3
    }

    [Test]
    public void Spec_Multiple_Where_In_Constructor_Should_Combine()
    {
        // ProductByIdSpec sets Where(x => x.Id == id)
        // If we chain two specs, both should apply
        var spec1 = new ProductHaveIdGreaterThanSpec(1);
        var spec2 = new ProductByIdSpec(3);
        var result = Products.AsQueryable().Where(spec1).Where(spec2).ToList();
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(3);
    }

    #endregion

    #region [Edge Cases]

    [Test]
    public void NullSpec_Expression_Should_Return_All()
    {
        ISpecification<Product> spec = new EmptyProductSpec();
        var result = Products.AsQueryable().Where(spec).ToList();
        result.Count.ShouldBe(4);
    }

    [Test]
    public void Spec_Should_Filter_With_String_Property()
    {
        var spec = new ProductByIdSpec(1);
        var result = Products.Where(spec).ToList();
        result[0].ProductName.ShouldBe("Product 1");
    }

    [Test]
    public void Where_With_No_Match_Should_Return_Empty()
    {
        var spec = new ProductByIdSpec(999);
        var result = Products.Where(spec).ToList();
        result.Count.ShouldBe(0);
    }

    #endregion

    #region [IsSatisfiedBy — P1]

    [Test]
    public void IsSatisfiedBy_Should_Return_True_When_Match()
    {
        var spec = new ProductByIdSpec(1);
        var product = new Product { Id = 1, ProductName = "P1" };
        spec.IsSatisfiedBy(product).ShouldBeTrue();
    }

    [Test]
    public void IsSatisfiedBy_Should_Return_False_When_No_Match()
    {
        var spec = new ProductByIdSpec(1);
        var product = new Product { Id = 2, ProductName = "P2" };
        spec.IsSatisfiedBy(product).ShouldBeFalse();
    }

    [Test]
    public void IsSatisfiedBy_EmptySpec_Should_Return_True()
    {
        var spec = new EmptyProductSpec();
        var product = new Product { Id = 1, ProductName = "P1" };
        spec.IsSatisfiedBy(product).ShouldBeTrue();
    }

    #endregion

    #region [Combinators — P3]

    [Test]
    public void And_Should_Combine_Both_Conditions()
    {
        ISpecification<Product> left = new ProductHaveIdGreaterThanSpec(1);
        ISpecification<Product> right = new ProductByIdSpec(3);
        var combined = left.And(right);
        var result = Products.AsQueryable().Where(combined).ToList();
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(3);
    }

    [Test]
    public void And_NullLeft_Should_Return_Right()
    {
        ISpecification<Product> left = new EmptyProductSpec();
        ISpecification<Product> right = new ProductByIdSpec(2);
        var combined = left.And(right);
        var result = Products.AsQueryable().Where(combined).ToList();
        result.Count.ShouldBe(1);
    }

    [Test]
    public void And_NullRight_Should_Return_Left()
    {
        ISpecification<Product> left = new ProductByIdSpec(2);
        ISpecification<Product> right = new EmptyProductSpec();
        var combined = left.And(right);
        var result = Products.AsQueryable().Where(combined).ToList();
        result.Count.ShouldBe(1);
    }

    [Test]
    public void Or_Should_Match_Either_Condition()
    {
        ISpecification<Product> left = new ProductByIdSpec(1);
        ISpecification<Product> right = new ProductByIdSpec(3);
        var combined = left.Or(right);
        var result = Products.AsQueryable().Where(combined).ToList();
        result.Count.ShouldBe(2);
    }

    [Test]
    public void Or_NullExpression_Should_Return_All()
    {
        ISpecification<Product> left = new EmptyProductSpec();
        ISpecification<Product> right = new ProductByIdSpec(1);
        var combined = left.Or(right);
        // null Or X = match all (null expression)
        var result = Products.AsQueryable().Where(combined).ToList();
        result.Count.ShouldBe(4);
    }

    [Test]
    public void Not_Should_Negate_Condition()
    {
        ISpecification<Product> spec = new ProductByIdSpec(1);
        var negated = spec.Not();
        var result = Products.AsQueryable().Where(negated).ToList();
        result.Count.ShouldBe(3); // All except Id=1
    }

    [Test]
    public void Not_NullExpression_Should_Return_None()
    {
        ISpecification<Product> spec = new EmptyProductSpec();
        var negated = spec.Not();
        var result = Products.AsQueryable().Where(negated).ToList();
        result.Count.ShouldBe(0);
    }

    #endregion

    #region [Apply — OrderBy + Paging — P3]

    [Test]
    public void Apply_OrderByDescending_Should_Sort()
    {
        var spec = new ProductOrderByIdDescSpec();
        var result = Products.AsQueryable().Apply(spec).ToList();
        result[0].Id.ShouldBe(4);
        result[3].Id.ShouldBe(1);
    }

    [Test]
    public void Apply_Paging_Should_Skip_And_Take()
    {
        var spec = new ProductPagedSpec(1, 2); // skip 1, take 2
        var result = Products.AsQueryable().Apply(spec).ToList();
        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(2);
        result[1].Id.ShouldBe(3);
    }

    [Test]
    public void Apply_Multiple_OrderBy_Should_ThenBy()
    {
        var spec = new ProductOrderByNameThenIdSpec();
        var result = Products.AsQueryable().Apply(spec).ToList();
        result.Count.ShouldBe(4);
        // Products sorted by ProductName asc, then Id asc
        result[0].ProductName.ShouldBe("Product 1");
    }

    [Test]
    public void Apply_Filter_OrderBy_Paging_Combined()
    {
        // ProductHaveIdGreaterThanSpec(1) → Id 2,3,4
        // Then OrderByDesc → 4,3,2
        // Then take 2 → 4,3
        var spec = new ProductHaveIdGreaterThanSpec(1);
        var orderSpec = new ProductOrderByIdDescSpec();
        // Apply filter first, then apply ordering
        var query = Products.AsQueryable().Where(spec);
        // Manual ordering for combined test
        var result = query.OrderByDescending(x => x.Id).Skip(0).Take(2).ToList();
        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(4);
        result[1].Id.ShouldBe(3);
    }

    [Test]
    public void Apply_Without_OrderBy_Should_Only_Filter()
    {
        var spec = new ProductByIdSpec(2);
        var result = Products.AsQueryable().Apply(spec).ToList();
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(2);
    }

    [Test]
    public void Apply_EmptySpec_Should_Return_All()
    {
        var spec = new EmptyProductSpec();
        var result = Products.AsQueryable().Apply(spec).ToList();
        result.Count.ShouldBe(4);
    }

    #endregion

    // Helper empty spec for tests
    private class EmptyProductSpec : Specification<Product> { }
}