namespace MABamlai.Model
{
    public class User
    {
        private int id;
        private string fullName;
        private string userName;
        private string password;
        private int role;

        public User(int id, string fullName, string userName, string password, int role)
        {
            this.id = id;
            this.fullName = fullName;
            this.userName = userName;
            this.password = password;
            this.role = role;
        }

        public User(int id, string fullName, string userName, string password)
        {
            this.id = id;
            this.fullName = fullName;
            this.userName = userName;
            this.password = password;
            this.role = 0;
        }

        public int GetId() { return this.id; }
        public string GetFullName() { return this.fullName; }
        public string GetUserName() { return this.userName; }
        public string GetPassword() { return this.password; }
        public bool GetRole() { return this.role == 1; }
    }
}
