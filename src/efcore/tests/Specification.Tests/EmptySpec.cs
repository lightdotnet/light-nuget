using Light.Specification;

namespace Specification.Tests
{
    /// <summary>
    /// A specification that does not call Where() — Expression should be null
    /// </summary>
    internal class EmptySpec : Specification<Product>
    {
        public EmptySpec()
        {
            // Intentionally empty — no Where() called
        }
    }
}