using System;
using System.Collections.Generic;
using System.Linq;

namespace CoffeeShopManagement
{
    #region Core Domain Classes
    public interface IOrderable
    {
        string Name { get; }
        decimal Price { get; }
        int Stock { get; }
        void AdjustStock(int quantity);
        void Display();
    }

    public abstract class ProductBase : IOrderable
    {
        public string Name { get; private set; }
        public decimal Price { get; private set; }
        public int Stock { get; private set; }

        protected ProductBase(string name, decimal price, int stock)
        {
            Name = name;
            Price = price;
            Stock = stock;
        }

        public virtual void AdjustStock(int quantity)
        {
            if (Stock + quantity < 0)
                throw new InvalidOperationException("Insufficient stock");
            Stock += quantity;
        }

        public abstract void Display();

        public virtual void AddToOrder(Order order)
        {
            if (Stock <= 0)
                throw new InvalidOperationException("Product out of stock");

            order.AddProduct(this);
            AdjustStock(-1);
        }
    }

    public class Beverage : ProductBase
    {
        public string Size { get; private set; }

        public Beverage(string name, decimal price, int stock, string size)
            : base(name, price, stock)
        {
            Size = size;
        }

        public override void Display()
        {
            Console.WriteLine($"{Name} ({Size}): ${Price} (Stock: {Stock})");
        }
    }

    public class Food : ProductBase
    {
        public bool IsVegetarian { get; private set; }

        public Food(string name, decimal price, int stock, bool isVegetarian)
            : base(name, price, stock)
        {
            IsVegetarian = isVegetarian;
        }

        public override void Display()
        {
            var vegStatus = IsVegetarian ? "Vegetarian" : "Non-vegetarian";
            Console.WriteLine($"{Name} [{vegStatus}]: ${Price} (Stock: {Stock})");
        }
    }

    public class Order
    {
        private List<IOrderable> _products = new List<IOrderable>();
        public DateTime OrderDate { get; private set; } = DateTime.Now;
        public decimal TotalPrice => _products.Sum(p => p.Price);

        public void AddProduct(IOrderable product)
        {
            _products.Add(product);
        }

        public void DisplayOrder()
        {
            Console.WriteLine("Order Summary:");
            foreach (var product in _products)
            {
                Console.WriteLine($"- {product.Name}: ${product.Price}");
            }
            Console.WriteLine($"Total Price: ${TotalPrice}");
            Console.WriteLine($"Order Date: {OrderDate}");
        }
    }

    public abstract class User
    {
        public string Username { get; private set; }
        private string _password;

        protected User(string username, string password)
        {
            Username = username;
            _password = password;
        }

        public abstract string Role { get; }

        public bool Authenticate(string password) => _password == password;
    }

    public class Admin : User
    {
        public Admin(string username, string password) : base(username, password) { }
        public override string Role => "admin";
    }

    public class Customer : User
    {
        public Customer(string username, string password) : base(username, password) { }
        public override string Role => "customer";
    }
    #endregion

    #region Coffee Shop System
    public class CoffeeShop
    {
        private List<ProductBase> _menu = new List<ProductBase>();
        private List<Order> _orderHistory = new List<Order>();
        private List<User> _users = new List<User>();

        public IEnumerable<User> Users => _users.AsReadOnly();
        public IEnumerable<ProductBase> Menu => _menu.AsReadOnly();

        public CoffeeShop()
        {
            // Add default users
            _users.Add(new Admin("admin", "admin123"));
            _users.Add(new Customer("customer", "customer123"));
        }

        public void AddProduct(ProductBase product)
        {
            _menu.Add(product);
        }

        public void UpdateProduct(string name, decimal price, int stock)
        {
            var product = _menu.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (product != null)
            {
                product.AdjustStock(stock - product.Stock);
                _menu.Remove(product);
                _menu.Add(ProductFactory.CreateProduct(product.GetType().Name, name, price, stock, ""));
            }
        }

        public void RemoveProduct(string name)
        {
            var product = _menu.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (product != null)
            {
                _menu.Remove(product);
            }
        }

        public void DisplayMenu()
        {
            Console.WriteLine("Coffee Shop Menu:");
            foreach (var product in _menu)
            {
                product.Display();
            }
        }

        public void DisplayOrderHistory()
        {
            Console.WriteLine("Order History:");
            foreach (var order in _orderHistory)
            {
                order.DisplayOrder();
            }
        }

        public void ProcessOrder(Order order)
        {
            _orderHistory.Add(order);
        }

        public void AddUser(User user)
        {
            _users.Add(user);
        }

        public User AuthenticateUser(string username, string password)
        {
            return _users.FirstOrDefault(u => u.Username == username && u.Authenticate(password));
        }
    }
    #endregion

    #region Factories
    public static class ProductFactory
    {
        public static ProductBase CreateProduct(string type, string name, decimal price, int stock, string additionalInfo)
        {
            return type switch
            {
                nameof(Beverage) => new Beverage(name, price, stock, additionalInfo),
                nameof(Food) => new Food(name, price, stock, bool.Parse(additionalInfo)),
                _ => throw new ArgumentException("Invalid product type")
            };
        }
    }
    #endregion

    #region Program
    class Program
    {
        static void Main(string[] args)
        {
            CoffeeShop coffeeShop = new CoffeeShop();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Coffee Shop Management System ===");
                Console.WriteLine("1. Login");
                Console.WriteLine("2. Exit");
                Console.Write("Choose an option: ");

                var choice = Console.ReadLine();

                if (choice == "1")
                {
                    HandleLogin(coffeeShop);
                }
                else if (choice == "2")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid choice! Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static void HandleLogin(CoffeeShop coffeeShop)
        {
            Console.Clear();
            Console.WriteLine("=== Login ===");
            string username = PromptForInput("Username: ");
            string password = PromptForInput("Password: ");

            var user = coffeeShop.AuthenticateUser(username, password);
            if (user == null)
            {
                Console.WriteLine("Invalid credentials! Press any key to continue...");
                Console.ReadKey();
                return;
            }

            HandleUserSession(coffeeShop, user);
        }

        static void HandleUserSession(CoffeeShop coffeeShop, User user)
        {
            bool sessionActive = true;
            while (sessionActive)
            {
                Console.Clear();
                Console.WriteLine($"Logged in as: {user.Username} ({user.Role})");
                Console.WriteLine("1. Place Order");
                Console.WriteLine("2. View Order History");

                if (user.Role == "admin")
                {
                    Console.WriteLine("3. Manage Menu");
                    Console.WriteLine("4. Add User");
                }

                Console.WriteLine("5. Logout");
                Console.Write("Choose an option: ");

                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            PlaceOrder(coffeeShop);
                            break;
                        case "2":
                            ViewOrderHistory(coffeeShop);
                            break;
                        case "3" when user.Role == "admin":
                            ManageMenu(coffeeShop);
                            break;
                        case "4" when user.Role == "admin":
                            AddUser(coffeeShop);
                            break;
                        case "5":
                            sessionActive = false;
                            break;
                        default:
                            Console.WriteLine("Invalid choice! Press any key to continue...");
                            Console.ReadKey();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static void PlaceOrder(CoffeeShop coffeeShop)
        {
            Order order = new Order();
            while (true)
            {
                Console.Clear();
                coffeeShop.DisplayMenu();
                Console.WriteLine("\nEnter product name to add to order (or 'done' to finish):");
                var input = Console.ReadLine();

                if (input?.ToLower() == "done") break;

                var product = coffeeShop.Menu.FirstOrDefault(p =>
                    p.Name.Equals(input, StringComparison.OrdinalIgnoreCase));

                if (product == null)
                {
                    Console.WriteLine("Product not found!");
                }
                else if (product.Stock <= 0)
                {
                    Console.WriteLine("Product out of stock!");
                }
                else
                {
                    try
                    {
                        product.AddToOrder(order);
                        Console.WriteLine($"{product.Name} added to order!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }

                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }

            coffeeShop.ProcessOrder(order);
            Console.Clear();
            order.DisplayOrder();
            Console.WriteLine("Press any key to return to menu...");
            Console.ReadKey();
        }

        static void ViewOrderHistory(CoffeeShop coffeeShop)
        {
            Console.Clear();
            coffeeShop.DisplayOrderHistory();
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        static void ManageMenu(CoffeeShop coffeeShop)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Menu Management ===");
                Console.WriteLine("1. Add Product");
                Console.WriteLine("2. Update Product");
                Console.WriteLine("3. Remove Product");
                Console.WriteLine("4. Back to Main Menu");
                Console.Write("Choose an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddProduct(coffeeShop);
                        break;
                    case "2":
                        UpdateProduct(coffeeShop);
                        break;
                    case "3":
                        RemoveProduct(coffeeShop);
                        break;
                    case "4":
                        return;
                    default:
                        Console.WriteLine("Invalid choice!");
                        break;
                }
            }
        }

        static void AddProduct(CoffeeShop coffeeShop)
        {
            Console.Clear();
            Console.WriteLine("=== Add New Product ===");

            string type = GetProductType();
            string name = PromptForInput("Product name: ");
            decimal price = GetDecimalInput("Price: ");
            int stock = GetIntInput("Stock: ");
            string additional = GetAdditionalInfo(type);

            try
            {
                var product = ProductFactory.CreateProduct(type, name, price, stock, additional);
                coffeeShop.AddProduct(product);
                Console.WriteLine("Product added successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static string GetProductType()
        {
            while (true)
            {
                Console.Write(" Enter product type (Beverage/Food): ");
                string type = Console.ReadLine();
                if (type.Equals("Beverage", StringComparison.OrdinalIgnoreCase) ||
                    type.Equals("Food", StringComparison.OrdinalIgnoreCase))
                {
                    return type;
                }
                Console.WriteLine("Invalid product type. Please enter 'Beverage' or 'Food'.");
            }
        }

        static string GetAdditionalInfo(string type)
        {
            if (type.Equals("Beverage", StringComparison.OrdinalIgnoreCase))
            {
                return PromptForInput("Size: ");
            }
            else if (type.Equals("Food", StringComparison.OrdinalIgnoreCase))
            {
                return PromptForInput("Is it vegetarian? (true/false): ");
            }
            return string.Empty;
        }

        static void UpdateProduct(CoffeeShop coffeeShop)
        {
            Console.Clear();
            Console.WriteLine("=== Update Product ===");
            string name = PromptForInput("Enter product name to update: ");
            decimal price = GetDecimalInput("New Price: ");
            int stock = GetIntInput("New Stock: ");

            try
            {
                coffeeShop.UpdateProduct(name, price, stock);
                Console.WriteLine("Product updated successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void RemoveProduct(CoffeeShop coffeeShop)
        {
            Console.Clear();
            Console.WriteLine("=== Remove Product ===");
            string name = PromptForInput("Enter product name to remove: ");

            try
            {
                coffeeShop.RemoveProduct(name);
                Console.WriteLine("Product removed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void AddUser(CoffeeShop coffeeShop)
        {
            Console.Clear();
            Console.WriteLine("=== Add New User ===");
            string username = PromptForInput("Username: ");
            string password = PromptForInput("Password: ");
            string role = PromptForInput("Role (admin/customer): ");

            User newUser = role.Equals("admin", StringComparison.OrdinalIgnoreCase)
                ? new Admin(username, password)
                : new Customer(username, password);

            coffeeShop.AddUser(newUser);
            Console.WriteLine("User  added successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static decimal GetDecimalInput(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (decimal.TryParse(Console.ReadLine(), out decimal result))
                {
                    return result;
                }
                Console.WriteLine("Invalid input. Please enter a valid decimal number.");
            }
        }

        static int GetIntInput(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int result))
                {
                    return result;
                }
                Console.WriteLine("Invalid input. Please enter a valid integer.");
            }
        }

        static string PromptForInput(string message)
        {
            Console.Write(message);
            return Console.ReadLine();
        }
    }
    #endregion
}