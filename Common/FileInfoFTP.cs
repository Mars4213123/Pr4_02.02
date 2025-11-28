using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace Common
{
    public class FileInfoFTP
    {
        public byte[] Data { get; set; }
        public string Name { get; set; }

        public FileInfoFTP(byte[] data, string name)
        {
            Data = data;
            Name = name;
        }
    }
}

