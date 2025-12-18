using Light.Extensions;

namespace Shared
{
    public record Product(int Id, string Name, DateTime Created);

    public record Search() : IPage
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class ProductService
    {
        private readonly List<Product> _products = [];

        public ProductService()
        {
            for (int i = 1; i <= 100; i++)
            {
                _products.Add(new(i, $"Product {i}", DateTime.Now));
            }
        }

        public static Func<string, Product, bool> SearchFunc =>
            (searchValue, e) =>
                e.Id.ToString().Contains(searchValue, StringComparison.OrdinalIgnoreCase)
                || e.Name.Contains(searchValue, StringComparison.OrdinalIgnoreCase)
                || e.Created.ToString().Contains(searchValue, StringComparison.OrdinalIgnoreCase);

        public void AddProduct(Product product)
        {
            _products.Add(product);
        }

        public async Task<IEnumerable<Product>> GetAll()
        {
            await Task.Delay(500);
            //throw new Exception("connection error");
            return _products.AsEnumerable();
        }

        public async Task<Result<IEnumerable<Product>>> GetResult()
        {
            await Task.Delay(500);

            //return Result<IEnumerable<Product>>.Error("connection error");
            return Result<IEnumerable<Product>>.Success(_products.AsEnumerable());
        }

        public async Task<PagedResult<Product>> Search(Search search)
        {
            var result = _products.ToPagedResult(search.Page, search.PageSize);

            await Task.Delay(500);

            //throw new Exception("connection error");

            return result;
        }
    }
}
