using Microsoft.Data.Sqlite;
using MABamlai.Model;
using System;
using System.Collections.Generic;

namespace MABamlai.Services
{
    public class DatabaseService
    {
        // מיקום הדאתא בייס - איפה שהוא שמור
        private string _connectionString = "Data Source=C:\\Users\\almak\\Documents\\computer science\\MABamlai\\MABamlai\\Data\\DataBase.db";

        // הפונקציה שמחזירה את כל המשתמשים שיש ברשימה
        public List<User> GetAllUsers()
        {
            //יצירת רשימה מסוג משתמש (אובייקט שיצרתי בתקיית מודלס
            List<User> usersList = new List<User>();

            //יצירת החיבור
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                //פתיחת החיבור 
                connection.Open();
                SqliteCommand command = connection.CreateCommand();

                // שאילתה שהוספת את כל המשתמשים מהטבלה ומכניסה אותם לאובייקטים בתוך הרשימה

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

        // החזרת רשימה של כל הציוד שחסר
        public List<MissingEquipment> GetAllMissingEquipment()
        {
            // יצירת הרשימה 
            List<MissingEquipment> missing = new List<MissingEquipment>();
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                // פתיחת החיבור
                connection.Open();
                SqliteCommand command = connection.CreateCommand();

                // לאסוף את כל המידע מהטבלה והכנסה ללתוך הרשימה
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

        // הוספת ציוד שחסר לטבלה
        public bool AddMissingEquipment(string name, string category, int amount)
        {
            //פתיחת חיבור והוספה לטבלה 
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

        // החזרת רשימה של כל המוצרים הקיימים במערכת
        public List<Product> GetAllProducts()
        {
            // יצירת רשימת מוצרים ריקה
            List<Product> productList = new List<Product>();
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                // פתיחת החיבור לטבלה
                connection.Open();
                SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Products";

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    // מעבר שורה שורה על כל המוצרים שהדאתא בייס החזיר
                    while (reader.Read())
                    {
                        string productName = reader["ProductName"]?.ToString() ?? string.Empty;
                        string history = reader["History"]?.ToString() ?? string.Empty;
                        string category = reader["Category"]?.ToString() ?? string.Empty;
                        int amount = Convert.ToInt32(reader["Amount"]);
                        int id = Convert.ToInt32(reader["ID"]);

                        // יצירת אובייקט מוצר חדש והוספה שלו לרשימה
                        productList.Add(new Product(productName, category, amount, history, id));
                    }
                }
            }
            return productList;
        }

        // סריקת הרשימה של המוצרים ומציאה מוצר לפי ID
        // כשיש אובייקט עם סימן שאלה זה אומר או שזה יחזיר את האובייקט או ריק
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

        // בדיקה האם משתמש יכול להתחבר למערכת לפי שם וסיסמה
        public User? CanLogIn(string userName, string password)
        {
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                // פתיחת חיבור
                connection.Open();
                SqliteCommand command = connection.CreateCommand();

                // שאילתה שמחפשת משתמש עם השם והסיסמה המדויקים, ומגבילה את התוצאה למשתמש אחד בלבד
                command.CommandText = "SELECT * FROM users WHERE UserName = $user AND Password = $pass LIMIT 1";
                command.Parameters.AddWithValue("$user", userName);
                command.Parameters.AddWithValue("$pass", password);

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    // אם נמצא משתמש כזה - ניצור אובייקט שלו ונחזיר אותו
                    if (reader.Read())
                    {
                        int id = Convert.ToInt32(reader["Id"]);
                        string fullName = reader["FullName"]?.ToString() ?? string.Empty;
                        int role = Convert.ToInt32(reader["role"]);
                        return new User(id, fullName, userName, password, role);
                    }
                }
            }
            // אם לא מצאנו אף אחד, נחזיר ריק (נאל)
            return null;
        }

        // הרשמת משתמש חדש למערכת כולל בדיקות תקינות לפרטים
        public bool Register(string username, string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            // בדיקה שלא השאירו ריק
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Username and password are required.";
                return false;
            }

            // בדיקה שהסיסמה מספיק ארוכה 
            if (password.Trim().Length < 6)// בלי רווחים בהתחלה ובסוף
            {
                errorMessage = "Password must be at least 6 characters.";
                return false;
            }

            // ניסיון ליצור את המשתמש בדאתא בייס
            bool created = TryCreateUser(username.Trim(), password.Trim());

            // אם היצירה נכשלה, זה אומר ששם המשתמש כבר תפוס
            if (!created)
                errorMessage = "This username already exists.";

            return created;
        }

        // פונקציית עזר שמנסה להכניס את המשתמש החדש לטבלת המשתמשים
        public bool TryCreateUser(string username, string password)
        {
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();

                // פקודת הכנסה לטבלה
                command.CommandText = "INSERT INTO users (FullName, UserName, Password, role) VALUES ($fullName, $user, $pass, $role)";
                command.Parameters.AddWithValue("$fullName", username);
                command.Parameters.AddWithValue("$user", username);
                command.Parameters.AddWithValue("$pass", password);
                command.Parameters.AddWithValue("$role", 0);

                // מחזיר אמת אם השורה התווספה בהצלחה
                return command.ExecuteNonQuery() > 0;
            }
        }

        // בדיקה האם הפרטים ששם המשתמש הקיש נכונים 
        public bool ValidateCredentials(string username, string password)
        {
            // אם אחד השדות ריק, ישר נחזיר שזה לא תקין
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            // משתמש בפונקציה CanLogIn שכתבנו קודם כדי לראות אם חזר משתמש או נאל
            return CanLogIn(username.Trim(), password.Trim()) != null;
        }

        // מחיקת משתמש מהטבלה לפי ה-ID שלו
        public bool DeleteUser(int id)
        {
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                SqliteCommand command = connection.CreateCommand();

                // פקודה שמוחקת את השורה של המשתמש הספציפי
                command.CommandText = "DELETE FROM users WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);

                // מחזיר אמת אם המחיקה הצליחה
                return command.ExecuteNonQuery() > 0;
            }
        }

        // עדכון פרטים של משתמש קיים בטבלה לפי ה-ID שלו
        public bool UpdateUser(int id, string fullName, string userName, string password, int role)
        {
            using SqliteConnection connection = new SqliteConnection(_connectionString);
            connection.Open();
            SqliteCommand command = connection.CreateCommand();

            // פקודת עדכון לכל השדות של המשתמש שנבחר
            command.CommandText = "UPDATE users SET FullName = $fullName, UserName = $user, Password = $pass, role = $role WHERE Id = $id";
            command.Parameters.AddWithValue("$fullName", fullName);
            command.Parameters.AddWithValue("$user", userName);
            command.Parameters.AddWithValue("$pass", password);
            command.Parameters.AddWithValue("$role", role);
            command.Parameters.AddWithValue("$id", id);

            // מחזיר אמת אם העדכון הצליח
            return command.ExecuteNonQuery() > 0;
        }

        // הוספת מוצר חדש לטבלת המוצרים
        public bool AddProduct(string productName, string category, int amount, string history)
        {
            using (SqliteConnection connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (SqliteCommand command = connection.CreateCommand())
                {
                    // פקודה שמכניסה את כל פרטי המוצר החדש לטבלה
                    command.CommandText = "INSERT INTO Products (ProductName, Category, Amount, History) VALUES ($name, $cate, $amount, $history)";

                    command.Parameters.AddWithValue("$name", productName);
                    command.Parameters.AddWithValue("$cate", category);
                    command.Parameters.AddWithValue("$amount", amount);
                    command.Parameters.AddWithValue("$history", history);

                    // מחזיר אמת אם המוצר נוסף בהצלחה
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}