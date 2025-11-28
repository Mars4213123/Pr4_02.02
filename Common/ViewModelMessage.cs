using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class ViewModelMessage
    {
        public string Command { get; set; }
        public string Data { get; set; }

        public ViewModelMessage(string command, string data)
        {
            Command = command;
            Data = data;
        }
    }
}
