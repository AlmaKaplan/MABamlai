using MABamlai.Model;
using System;

namespace MABamlai.Services
{
    public class UserService
    {
        private User _currentUser;
        public event Action? OnNotify;

        public UserService()
        {
            // אתחול כמשתמש אורח
            _currentUser = new User(0, "guest", "guest", "", 0);
        }

        public void SetUser(User user)
        {
            if (user != null)
            {
                _currentUser = user;
                NotifyStateChanged();
            }
        }

        public User GetUser() => _currentUser;

        public bool IsUserLoggedIn() => _currentUser.GetUserName() != "guest";

        public void Logout()
        {
            _currentUser = new User(0, "guest", "guest", "", 0);
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnNotify?.Invoke();
    }
}
