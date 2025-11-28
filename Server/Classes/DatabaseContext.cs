using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Server.Classes
{
    public class DatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserCommand> UserCommands { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = "server=MySQL-8.0;port=3306;database=ftp_server;user=root;password=;";
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
}