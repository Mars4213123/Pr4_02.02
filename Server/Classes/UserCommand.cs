using System;
using System.ComponentModel.DataAnnotations;

namespace Server.Classes
{
    public class UserCommand
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Command { get; set; }
        public string Parameters { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.Now;
        public string Result { get; set; }

        public UserCommand() { }

        public UserCommand(int userId, string command, string parameters, string result)
        {
            UserId = userId;
            Command = command;
            Parameters = parameters;
            Result = result;
        }
    }
}