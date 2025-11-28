using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Server.Classes
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string BaseDirectory { get; set; }
        public string CurrentDirectory { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<UserCommand> Commands { get; set; } = new List<UserCommand>();

        public User() { }

        public User(string login, string password, string baseDirectory)
        {
            Login = login;
            Password = password;
            BaseDirectory = baseDirectory;
            CurrentDirectory = baseDirectory;
        }
    }
}