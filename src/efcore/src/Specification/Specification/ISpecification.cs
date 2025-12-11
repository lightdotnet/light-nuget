using System;
using System.Linq.Expressions;

namespace Light.Specification
{
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>>? Expression { get; }
    }
}
