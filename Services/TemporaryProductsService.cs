using MABamlai.Model;

namespace MABamlai.Services
{
    public class TemporaryProductsService
    {
        private readonly List<Product> temporaryProducts = new List<Product>();
        private readonly object productsLock = new object();

        public void Add(Product product)
        {
            lock (productsLock)
            {
                temporaryProducts.Add(product);
            }
        }

        public List<Product> GetAll()
        {
            lock (productsLock)
            {
                return new List<Product>(temporaryProducts);
            }
        }
    }
}
