using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class ViewModelSend
    {
        public string Message { get; set; }
        public int Id { get; set; }

        public ViewModelSend(string message, int id)
        {
            Message = message;
            Id = id;
        }
    }
}
