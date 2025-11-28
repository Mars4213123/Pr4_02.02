using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Server.Classes
{
    public class UserRepository : IDisposable
    {
        private readonly DatabaseContext _context;

        public UserRepository()
        {
            _context = new DatabaseContext();
            _context.Database.EnsureCreated();
        }

        public User GetUserById(int id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id);
        }

        public bool ValidateUser(string login, string password)
        {
            return _context.Users.Any(u => u.Login == login && u.Password == password);
        }

        public int GetUserId(string login, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
            return user?.Id ?? -1;
        }

        public void UpdateUser(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void UpdateUserCurrentDirectory(int userId, string currentDirectory)
        {
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.CurrentDirectory = currentDirectory;
                _context.SaveChanges();
            }
        }


        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}