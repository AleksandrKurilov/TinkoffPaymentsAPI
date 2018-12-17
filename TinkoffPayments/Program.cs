using System;
using System.Collections.Generic;
using System.Threading;

namespace TinkoffPayments
{
    internal class Program
    {
        private static void Main()
        {
            ProductsCollection productsCollection = new ProductsCollection();
            Console.Clear();

           Product p = productsCollection.Products[new Random().Next(0, 3)];
           var payResult = TinkoffPayment.SendPayment($"newtest{new Random().Next(1, 100000)}",
                 p.Description, p.Price, "Mail@Mail.ru", "", "https://google.com");
           PrintProduct(payResult);

                //Проверяем состояние покупки каждую минуту, всего проверяем 5 раз.
          for (var x = 0; x <= 5; x++) {
            Thread.Sleep(60000);
            var checkResult = TinkoffPayment.CheckPaymentStatus(payResult.PaymentId);
            PrintCheckResult(checkResult);
          }

          Console.ReadKey();
        }

        private static void PrintProduct(TinkoffPayment.TinkoffPaymentOperationResult payResult)
        {
            Console.WriteLine("##################################");
            Console.WriteLine($"Result: {payResult.PaymentResult}");
            Console.WriteLine("");
            Console.WriteLine($"Messages: {payResult.Messages}");
            Console.WriteLine("");
            Console.WriteLine($"PaymentId: {payResult.PaymentId}");
            Console.WriteLine("");
            Console.WriteLine($"Url: {payResult.Messages}");
            Console.WriteLine("##################################");
            Console.WriteLine("");
        }

        private static void PrintCheckResult(TinkoffPayment.TinkoffPaymentOperationResult checkResult)
        {
            Console.WriteLine("##################################");
            Console.WriteLine($"Result: {checkResult.PaymentResult}");
            Console.WriteLine("");
            Console.WriteLine($"Messages: {checkResult.Messages}");
            Console.WriteLine("##################################");
            Console.WriteLine("");
        }

        public class ProductsCollection
        {
            public ProductsCollection()
            {
                Products = new List<Product>()
                {
                    new Product("Хлеб", 220, "Свежий хлеб"),
                    new Product("Молоко", 320, "Свежее молоко"),
                    new Product("Велосипед", 140, "Спортивный велосипед..."),
                    new Product("Цемент", 320, "Просто цемент"),
                };

            }
            public List<Product> Products { get; set; }

        }

        public class Product
        {
            public Product(string productName, int price, string description)
            {

                Price = price;
                Description = description;
                ProductName = productName;
            }

            public string ProductName { get; set; }
            public int Price { get; }///Цена в копейках
            public string Description { get; }
        }
    }
}
