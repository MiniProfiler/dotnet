using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Http;
using Sample.WebApiSelfHost.Models;
using StackExchange.Profiling;

namespace Sample.WebApiSelfHost.Controllers
{
    public class ProductsController : ApiController
    {
        private Product[] products = new Product[]
        {
            new Product { Id = 1, Name = "Tomato Soup", Category = "Groceries", Price = 1 },
            new Product { Id = 2, Name = "Yo-yo", Category = "Toys", Price = 3.75M },
            new Product { Id = 3, Name = "Hammer", Category = "Hardware", Price = 16.99M }
        };

        public IEnumerable<Product> Get()
        {
            using (MiniProfiler.Current.Step("Loading all products"))
            {
                // Introduce a random delay to simulate database query time.
                Thread.Sleep(new Random().Next(25, 125));

                return products;
            }
        }

        public Product Get(int id)
        {
            using (MiniProfiler.Current.Step("Loading product"))
            {
                // Introduce a random delay to simulate database query time.
                Thread.Sleep(new Random().Next(5, 60));

                var product = products.SingleOrDefault(p => p.Id == id);

                if (product == null)
                {
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                }

                return product;
            }
        }
    }
}