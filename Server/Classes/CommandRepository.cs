using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Classes
{
    public class CommandRepository : IDisposable
    {
        private readonly DatabaseContext _context;

        public CommandRepository()
        {
            _context = new DatabaseContext();
        }

        public void LogCommand(int userId, string command, string parameters, string result)
        {
            try
            {
                var userCommand = new UserCommand(userId, command, parameters, result);
                _context.UserCommands.Add(userCommand);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при логировании команды: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}