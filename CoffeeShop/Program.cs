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
        int Id { get; }
        string Category { get; }
    }

    public abstract class ProductBase : IOrderable
    {
        private static int _idCounter = 1;
        public int Id { get; protected set; }
        public string Name { get; protected set; }
        public decimal Price { get; set; }
        public int Stock { get; protected set; }
        public abstract string Category { get; }

        protected ProductBase(string name, decimal price, int stock)
        {
            Id = _idCounter++;
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

        public virtual void AddToOrder(Order order, int quantity = 1)
        {
            if (Stock < quantity)
                throw new InvalidOperationException("Product out of stock");

            order.AddProduct(this, quantity);
            AdjustStock(-quantity);
        }
    }

    public class Coffee : ProductBase
    {
        public override string Category => "Coffee";
        public string Size { get; private set; }

        public Coffee(string name, decimal price, int stock, string size)
            : base(name, price, stock)
        {
            Size = size;
        }

        public override void Display()
        {
            Console.WriteLine($"{Name} ({Size})");
        }
    }

    public class Snack : ProductBase
    {
        public override string Category => "Snack";
        public bool IsSweet { get; private set; }

        public Snack(string name, decimal price, int stock, bool isSweet)
            : base(name, price, stock)
        {
            IsSweet = isSweet;
        }

        public override void Display()
        {
            Console.WriteLine($"{Name} [{(IsSweet ? "Sweet" : "Non-sweet")}]");
        }
    }

    public class Beverage : ProductBase
    {
        public override string Category => "Beverage";
        public bool IsCold { get; private set; }

        public Beverage(string name, decimal price, int stock, bool isCold)
            : base(name, price, stock)
        {
            IsCold = isCold;
        }

        public override void Display()
        {
            Console.WriteLine($"{Name} ({(IsCold ? "Cold" : "Hot")})");
        }
    }

    public class Order
    {
        private List<OrderItem> _items = new List<OrderItem>();
        public DateTime OrderDate { get; private set; } = DateTime.Now;
        public decimal TotalPrice => _items.Sum(i => i.Subtotal);
        public int ItemCount => _items.Sum(i => i.Quantity);
        public User User { get; set; }

        public void AddProduct(IOrderable product, int quantity)
        {
            var existing = _items.FirstOrDefault(i => i.Product.Id == product.Id);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                _items.Add(new OrderItem(product, quantity));
            }
        }

        public void DisplayOrder()
        {
            Console.WriteLine($"\nOrder Date: {OrderDate:g}");
            Console.WriteLine($"Customer: {User.Username}");
            Console.WriteLine("Items:");
            foreach (var item in _items)
            {
                Console.WriteLine($"- {item.Product.Name} x{item.Quantity} @ {item.Product.Price:C}");
            }
            Console.WriteLine($"Total: {TotalPrice:C}");
        }
    }

    public class OrderItem
    {
        public IOrderable Product { get; }
        public int Quantity { get; set; }
        public decimal Subtotal => Product.Price * Quantity;

        public OrderItem(IOrderable product, int quantity)
        {
            Product = product;
            Quantity = quantity;
        }
    }

    public abstract class User
    {
        private static int _idCounter = 1;
        public int Id { get; }
        public string Username { get; }
        public int LoyaltyPoints { get; protected set; }
        private string _password;
        public abstract string Role { get; }

        protected User(string username, string password)
        {
            Id = _idCounter++;
            Username = username;
            _password = password;
            LoyaltyPoints = 0;
        }

        public bool Authenticate(string password) => _password == password;

        public virtual void AddLoyaltyPoints(decimal amountSpent)
        {
            LoyaltyPoints += (int)(amountSpent / 5);
        }

        public void UpdatePassword(string newPassword)
        {
            _password = newPassword;
        }
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

        public override void AddLoyaltyPoints(decimal amountSpent)
        {
            base.AddLoyaltyPoints(amountSpent);
            Console.WriteLine($"Earned {LoyaltyPoints} loyalty points!");
        }
    }
    #endregion

    #region Factory Pattern Implementation
    public interface IProductFactory
    {
        ProductBase CreateProduct(string name, decimal price, int stock, string additionalParam);
    }

    public class CoffeeFactory : IProductFactory
    {
        public ProductBase CreateProduct(string name, decimal price, int stock, string size)
        {
            return new Coffee(name, price, stock, size);
        }
    }

    public class SnackFactory : IProductFactory
    {
        public ProductBase CreateProduct(string name, decimal price, int stock, string isSweet)
        {
            bool sweet = bool.Parse(isSweet);
            return new Snack(name, price, stock, sweet);
        }
    }

    public class BeverageFactory : IProductFactory
    {
        public ProductBase CreateProduct(string name, decimal price, int stock, string isCold)
        {
            bool cold = bool.Parse(isCold);
            return new Beverage(name, price, stock, cold);
        }
    }

    public static class ProductFactory
    {
        private static readonly Dictionary<string, IProductFactory> _factories = new Dictionary<string, IProductFactory>
        {
            { "Coffee", new CoffeeFactory() },
            { "Snack", new SnackFactory() },
            { "Beverage", new BeverageFactory() }
        };

        public static ProductBase CreateProduct(string type, string name, decimal price, int stock, string additionalParam)
        {
            if (_factories.TryGetValue(type, out var factory))
            {
                return factory.CreateProduct(name, price, stock, additionalParam);
            }
            throw new ArgumentException("Invalid product type");
        }
    }
    #endregion

    #region Coffee Shop System
    public class CoffeeShopSystem
    {
        private List<ProductBase> _menu = new List<ProductBase>();
        private List<Order> _orders = new List<Order>();
        private List<User> _users = new List<User>();
        private int _orderCounter = 1;

        public IEnumerable<User> Users => _users.AsReadOnly();
        public CoffeeShopSystem()
        {
            InitializeDefaultData();
        }

        private void InitializeDefaultData()
        {
            _users.Add(new Admin("admin", "admin123"));
            _users.Add(new Customer("customer", "customer123"));

            AddProduct(ProductFactory.CreateProduct("Coffee", "Espresso", 2.50m, 10, "Medium"));
            AddProduct(ProductFactory.CreateProduct("Snack", "Croissant", 3.00m, 15, "true"));
            AddProduct(ProductFactory.CreateProduct("Beverage", "Iced Tea", 2.00m, 20, "true"));
        }

        public void AddProduct(ProductBase product) => _menu.Add(product);

        public void ProcessOrder(Order order, User user)
        {
            if (order.TotalPrice > 0)
            {
                order.User = user;
                _orders.Add(order);
                if (user is Customer customer)
                {
                    customer.AddLoyaltyPoints(order.TotalPrice);
                }
            }
        }

        public User AuthenticateUser(string username, string password)
        {
            return _users.FirstOrDefault(u => u.Username == username && u.Authenticate(password));
        }

        public void DisplayMenu()
        {
            Console.WriteLine("\nCurrent Menu:");
            Console.WriteLine("ID | Category   | Name                | Price   | Stock | Details");
            Console.WriteLine("-------------------------------------------------------------------");
            foreach (var product in _menu.OrderBy(p => p.Id))
            {
                Console.Write($"{product.Id.ToString().PadRight(3)}| ");
                Console.Write($"{product.Category.PadRight(10)}| ");
                Console.Write($"{product.Name.PadRight(20)}| ");
                Console.Write($"{product.Price.ToString("C").PadRight(7)}| ");
                Console.Write($"{product.Stock.ToString().PadRight(5)}| ");
                product.Display();
            }
        }

        public ProductBase GetProductById(int id) => _menu.FirstOrDefault(p => p.Id == id);
        public User GetUserById(int id) => _users.FirstOrDefault(u => u.Id == id);

        public void AddUser(User user) => _users.Add(user);

        public void UpdateProduct(int id, decimal newPrice, int newStock)
        {
            var product = _menu.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                product.Price = newPrice;
                product.AdjustStock(newStock - product.Stock);
            }
        }

        public void RemoveProduct(int id) => _menu.RemoveAll(p => p.Id == id);

        public void DisplayOrderHistory(User currentUser)
        {
            var filteredOrders = currentUser.Role == "admin"
                ? _orders
                : _orders.Where(o => o.User.Id == currentUser.Id).ToList();

            Console.WriteLine("\nOrder History:");
            foreach (var order in filteredOrders)
            {
                order.DisplayOrder();
                Console.WriteLine("-----------------------------");
            }
        }

        public void RemoveUser(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null && user.Role != "admin")
            {
                _users.Remove(user);
            }
        }
    }
    #endregion

    #region User Interface
    class Program
    {
        private static CoffeeShopSystem _shop = new CoffeeShopSystem();

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Coffee Shop Management System ===");
                Console.WriteLine("1. Login");
                Console.WriteLine("2. Exit");
                var choice = GetInput("Choose an option: ");

                if (choice == "1") HandleLogin();
                else if (choice == "2") break;
                else ShowError("Invalid choice!");
            }
        }

        static void HandleLogin()
        {
            Console.Clear();
            Console.WriteLine("=== Login ===");
            string username = GetInput("Username: ");
            string password = GetPassword("Password: ");

            var user = _shop.AuthenticateUser(username, password);
            if (user == null)
            {
                ShowError("Invalid credentials!");
                return;
            }

            HandleUserSession(user);
        }

        static void HandleUserSession(User user)
        {
            bool sessionActive = true;
            while (sessionActive)
            {
                Console.Clear();
                Console.WriteLine($"Logged in as: {user.Username} ({user.Role})");
                Console.WriteLine("1. Place Order");
                Console.WriteLine("2. View Order History");
                Console.WriteLine("3. View Menu");

                if (user.Role == "admin")
                {
                    Console.WriteLine("4. Manage Menu");
                    Console.WriteLine("5. Manage Users");
                }

                Console.WriteLine("6. Logout");
                var choice = GetInput("Choose an option: ");

                try
                {
                    switch (choice)
                    {
                        case "1": PlaceOrder(user); break;
                        case "2": ViewOrderHistory(user); break;
                        case "3": ShowMenu(); break;
                        case "4" when user.Role == "admin": ManageMenu(); break;
                        case "5" when user.Role == "admin": ManageUsers(); break;
                        case "6": sessionActive = false; break;
                        default: ShowError("Invalid choice!"); break;
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Error: {ex.Message}");
                }
            }
        }

        static void ViewOrderHistory(User user)
        {
            Console.Clear();
            _shop.DisplayOrderHistory(user);
            Console.WriteLine("Press any key to return...");
            Console.ReadKey();
        }

        static void ManageMenu()
        {
            while (true)
            {
                Console.Clear();
                _shop.DisplayMenu();
                Console.WriteLine("\n=== Menu Management ===");
                Console.WriteLine("1. Add Product");
                Console.WriteLine("2. Update Product");
                Console.WriteLine("3. Remove Product");
                Console.WriteLine("4. Back to Main Menu");
                var choice = GetInput("Choose an option: ");

                switch (choice)
                {
                    case "1": AddProduct(); break;
                    case "2": UpdateProduct(); break;
                    case "3": RemoveProduct(); break;
                    case "4": return;
                    default: ShowError("Invalid choice!"); break;
                }
            }
        }

        static void ManageUsers()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== User Management ===");
                Console.WriteLine("1. List Users");
                Console.WriteLine("2. Add User");
                Console.WriteLine("3. Update User");
                Console.WriteLine("4. Remove User");
                Console.WriteLine("5. Back to Main Menu");
                var choice = GetInput("Choose an option: ");

                switch (choice)
                {
                    case "1": ListUsers(); break;
                    case "2": AddUser(); break;
                    case "3": UpdateUser(); break;
                    case "4": RemoveUser(); break;
                    case "5": return;
                    default: ShowError("Invalid choice!"); break;
                }
            }
        }

        static void ListUsers()
        {
            Console.Clear();
            Console.WriteLine("ID | Username       | Role       | Loyalty Points");
            Console.WriteLine("------------------------------------------------");
            foreach (var user in _shop.Users)
            {
                Console.WriteLine($"{user.Id.ToString().PadRight(3)}| " +
                                  $"{user.Username.PadRight(15)}| " +
                                  $"{user.Role.PadRight(10)}| " +
                                  $"{user.LoyaltyPoints}");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void AddUser()
        {
            Console.Clear();
            Console.WriteLine("=== Add New User ===");
            string username = GetInput("Username: ");
            string password = GetPassword("Password: ");
            string role = GetInput("Role (admin/customer): ");

            User newUser = role.Equals("admin", StringComparison.OrdinalIgnoreCase)
                ? new Admin(username, password)
                : new Customer(username, password);

            _shop.AddUser(newUser);
            Console.WriteLine("User  added successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void UpdateUser()
        {
            Console.Clear();
            Console.WriteLine("=== Update User ===");
            int userId = GetIntInput("Enter user ID to update: ");
            var user = _shop.GetUserById(userId);

            if (user == null)
            {
                ShowError("User  not found!");
                return;
            }

            Console.WriteLine($"Updating user: {user.Username}");
            string newPassword = GetPassword("New password (leave empty to keep current): ");
            if (!string.IsNullOrEmpty(newPassword))
            {
                user.UpdatePassword(newPassword);
            }

            if (user.Role == "customer")
            {
                string newRole = GetInput("Change role to admin? (y/n): ");
                if (newRole.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    _shop.RemoveUser(userId);
                    _shop.AddUser(new Admin(user.Username, newPassword));
                }
            }

            Console.WriteLine("User  updated successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void RemoveUser()
        {
            Console.Clear();
            Console.WriteLine("=== Remove User ===");
            int userId = GetIntInput("Enter user ID to remove: ");
            _shop.RemoveUser(userId);
            Console.WriteLine("User  removed successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void PlaceOrder(User user)
        {
            Console.Clear();
            Console.WriteLine("=== Place Order ===");
            var order = new Order();
            while (true)
            {
                _shop.DisplayMenu();
                int productId = GetIntInput("Enter product ID to order (0 to finish): ");
                if (productId == 0) break;

                var product = _shop.GetProductById(productId);
                if (product == null)
                {
                    ShowError("Product not found!");
                    continue;
                }

                int quantity = GetIntInput($"Enter quantity for {product.Name}: ");
                try
                {
                    product.AddToOrder(order, quantity);
                }
                catch (InvalidOperationException ex)
                {
                    ShowError(ex.Message);
                }
            }

            _shop.ProcessOrder(order, user);
            Console.WriteLine("Order placed successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void ShowMenu()
        {
            Console.Clear();
            _shop.DisplayMenu();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static string GetInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }

        static string GetPassword(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine(); // In a real application, consider masking the password input
        }

        static int GetIntInput(string prompt)
        {
            Console.Write(prompt);
            return int.TryParse(Console.ReadLine(), out int result) ? result : 0;
        }

        static void ShowError(string message)
        {
            Console.WriteLine($"Error: {message}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
    #endregion
}
