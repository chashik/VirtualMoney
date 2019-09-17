using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VirtualMoney
{
    public class Repository
    {
        private readonly HashSet<User> _users;
        private readonly object _usersLock;

        private readonly object _transactionsLock;
        private readonly HashSet<Transaction> _transactions;

        public Repository()
        {
            _users = new HashSet<User>();
            _usersLock = new object();

            _transactions = new HashSet<Transaction>();
            _transactionsLock = new object();
        }

        public IEnumerable<User> Users // Safe enumerable without secret's hash
        {
            get => _users.Select(p => new User { Login = p.Login });
        }

        public IEnumerable<Transaction> Transactions(string login) => 
            _transactions.Where(p => p.Payer == login || p.Correspondent == login);

        private string HashString(string source)
        {
            var bytes = Encoding.Default.GetBytes(string.Join("%", "pepper", source, "salt"));

            using (var md5 = MD5.Create())
            using (var stream = new MemoryStream(bytes))
            {
                bytes = md5.ComputeHash(stream);
                var builder = new StringBuilder();

                foreach (var b in bytes)
                    builder.Append(b.ToString("X2"));

                return builder.ToString();
            }
        }

        public bool CreateUser(string login, string secret)
        {
            lock (_usersLock)
                return _users.Where(p => p.Login == login).Count() == 0
                    && _users.Add(new User { Login = login, SecretHash = HashString(secret) })
                    && _transactions.Add( // some "unclean" logic, single responsibility failed)))
                        new Transaction
                        {
                            Payer = login,
                            Correspondent = login,
                            Amount = 500,
                            DateTime = DateTime.Now
                        });
        }

        public bool TryTransaction(Order order, out Transaction transaction)
        {
            lock (_transactionsLock)
                if (ValidateOrder(order))
                {
                    transaction = new Transaction
                    {
                        Payer = order.Payer,
                        Correspondent = order.Correspondent,
                        Amount = order.Amount,
                        DateTime = DateTime.Now
                    };

                    return _transactions.Add(transaction);
                }
                else
                {
                    transaction = null;
                    return false;
                }
        }

        private bool ValidateOrder(Order order)
        {
            var payer = _users.Single(p => p.Login == order.Payer);

            return
                UserExists(order.Payer) 
                && UserExists(order.Correspondent)
                && HashString(order.Secret) == payer.SecretHash 
                && (Balance(payer) - order.Amount) < 0;
        }

        private bool UserExists(string login) => _users.Where(p => p.Login == login).Count() > 0;

        private int Balance(User user)
        {
            var tIncome = Task.Run(() => _transactions.Where(p => p.Correspondent == user.Login).Select(p => p.Amount).Sum());

            var tOutcome = Task.Run(() => _transactions.Where(p => p.Payer == user.Login).Select(p => p.Amount).Sum());

            return tIncome.Result - tOutcome.Result;
        }
    }

    public class User
    {
        public string Login { get; set; }

        public string SecretHash { get; set; }
    }

    public class Transaction
    {
        public string Payer { get; set; }

        public string Correspondent { get; set; }

        public int Amount { get; set; }

        public DateTime DateTime { get; set; }
    }

    public class Order
    {
        public string Payer { get; set; }

        public string Secret { get; set; }

        public string Correspondent { get; set; }

        public int Amount { get; set; }
    }
}
