using Microsoft.Data.Sqlite;
using MABamlai.Model;
using System;
using System.Collections.Generic;
namespace MABamlai.Services
{
    public class DatabaseService
    {
        private string _connectionString = "Data Source=C:\\Users\\almak\\Documents\\computer science\\MABamlai\\MABamlai\\Data\\DataBase.db";
        public List<User> GetAllUsers()
        {
            List<User> usersList = new List<User>();
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM users";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string fullName = reader["FullName"]?.ToString() ?? string.Empty;
                        string userName = reader["UserName"]?.ToString() ?? string.Empty;
                        string password = reader["Password"]?.ToString() ?? string.Empty;
                        int id = Convert.ToInt32(reader["Id"]);
                        int role = Convert.ToInt32(reader["role"]);
                        usersList.Add(new User(id, fullName, userName, password, role));
                    }
                }
            }
            return usersList;
        }
        public List<MissingEquipment> GetAllMissingEquipment()
        {
            List<MissingEquipment> missing = new List<MissingEquipment>();
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM missingProduct";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string name = reader["productMame"]?.ToString() ?? string.Empty;
                        string category = reader["Category"]?.ToString() ?? string.Empty;
                        int amount = Convert.ToInt32(reader["amount"]);
                        missing.Add(new MissingEquipment(name, category, amount));
                    }
                }
            }
            return missing;
        }
        public bool AddMissingEquipment(string name, string category, int amount)
        {
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO missingProduct (productMame, Category, amount) VALUES ($name, $cate, $amount)";
                command.Parameters.AddWithValue("$name", name);
                command.Parameters.AddWithValue("$cate", category);
                command.Parameters.AddWithValue("$amount", amount);
                return command.ExecuteNonQuery() > 0;
            }
        }
        public List<Product> GetAllProducts()
        {
            List<Product> productList = new List<Product>();
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Products";
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string productName = reader["ProductName"]?.ToString() ?? string.Empty;
                        string history = reader["History"]?.ToString() ?? string.Empty;
                        string category = reader["Category"]?.ToString() ?? string.Empty;
                        int amount = Convert.ToInt32(reader["Amount"]);
                        int id = Convert.ToInt32(reader["ID"]);
                        productList.Add(new Product(productName, category, amount, history, id));
                    }
                }
            }
            return productList;
        }
        public Product? GetProductFromId(int id)
        {
            List<Product> allProducts = GetAllProducts();
            for (int i = 0; i < allProducts.Count; i++)
            {
                if (allProducts[i].GetId() == id)
                    return allProducts[i];
            }
            return null;
        }
        public User? CanLogIn(string userName, string password)
        {
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM users WHERE UserName = $user AND Password = $pass LIMIT 1";
                command.Parameters.AddWithValue("$user", userName);
                command.Parameters.AddWithValue("$pass", password);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int id = Convert.ToInt32(reader["Id"]);
                        string fullName = reader["FullName"]?.ToString() ?? string.Empty;
                        int role = Convert.ToInt32(reader["role"]);
                        return new User(id, fullName, userName, password, role);
                    }
                }
            }
            return null;
        }

        public bool Register(string username, string password, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Username and password are required.";
                return false;
            }
            if (password.Trim().Length < 6)
            {
                errorMessage = "Password must be at least 6 characters.";
                return false;
            }
            bool created = TryCreateUser(username.Trim(), password.Trim());
            if (!created)
                errorMessage = "This username already exists.";
            return created;
        }
        public bool TryCreateUser(string username, string password)
        {
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "INSERT INTO users (FullName, UserName, Password, role) VALUES ($fullName, $user, $pass, $role)";
                command.Parameters.AddWithValue("$fullName", username);
                command.Parameters.AddWithValue("$user", username);
                command.Parameters.AddWithValue("$pass", password);
                command.Parameters.AddWithValue("$role", 0);
                return command.ExecuteNonQuery() > 0;
            }
        }

        public bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;
            return CanLogIn(username.Trim(), password.Trim()) != null;
        }
        
        public bool DeleteUser(int id)
        {
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM users WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);
                return command.ExecuteNonQuery() > 0;
            }
        }
        public bool UpdateUser(int id, string fullName, string userName, string password, int role)
        {
            using SqliteConnection connection = new SqliteConnection(_connectionString);
            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE users SET FullName = $fullName, UserName = $user, Password = $pass, role = $role WHERE Id = $id";
            command.Parameters.AddWithValue("$fullName", fullName);
            command.Parameters.AddWithValue("$user", userName);
            command.Parameters.AddWithValue("$pass", password);
            command.Parameters.AddWithValue("$role", role);
            command.Parameters.AddWithValue("$id", id);
            return command.ExecuteNonQuery() > 0;
        }
    }
}