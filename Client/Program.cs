using System;
using System.Collections.Generic;
using System.Net;
using Common;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace Client
{
    public class Program
    {
        public static IPAddress IpAdress;
        public static int Port;
        public static int Id = -1;
        public static bool CheckCommand(string message)
        {
            bool BCommand = false;
            string[] DataMessage = message.Split(' ');

            if (DataMessage.Length > 0)
            {
                if (DataMessage[0] == "connect")
                {
                    if (DataMessage.Length == 3) BCommand = true;
                    else {
                        Console.WriteLine("Использование: connect [login] [password]\nПример: connect User1 P@sswOrd");
                    }
                }
                else if (DataMessage[0] == "cd")
                    BCommand = true;
                else if (DataMessage[0] == "get")
                {
                    if (DataMessage.Length != 1)
                        BCommand = true;
                    else {
                        Console.WriteLine("Использование: get [NameFile]\nПример: get Test.txt");
                    }
                }
                else if (DataMessage[0] == "set")
                {
                    if (DataMessage.Length != 1) BCommand = true;
                    else {
                        Console.WriteLine("Использование: set [NameFile]\nПример: set Test.txt");
                    }
                }
            }

            return BCommand;
        }
        public static void ConnectServer()
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IpAdress, Port);
                Socket socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                socket.Connect(endPoint);
                if (socket.Connected == false)
                {
                    Console.WriteLine("Подключение не удалось");
                    socket.Close();
                    return;
                }
                Console.ForegroundColor = ConsoleColor.White;
                string message = Console.ReadLine();
                if (CheckCommand(message) == false)
                {
                    socket.Close();
                    return;
                }

                ViewModelSend viewModelSend = new ViewModelSend(message, Id);
                string[] DataMessage = message.Split(' ');

                if (message.Split(' ')[0] == "set")
                {
                    string NameFile = "";
                    for (int i = 1; i < DataMessage.Length; i++)
                        if (NameFile == "")
                            NameFile += DataMessage[i];
                        else
                            NameFile += " " + DataMessage[i];
                    if (File.Exists(NameFile) == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Указанный файл не существует");

                        socket.Close();
                        return;
                    }
                    FileInfo FileInfo = new FileInfo(NameFile);
                    FileInfoFTP NewFileInfo = new FileInfoFTP(File.ReadAllBytes(NameFile), FileInfo.Name);
                    viewModelSend = new ViewModelSend(JsonConvert.SerializeObject(NewFileInfo), Id);
                }
                byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                socket.Send(messageByte);
                byte[] bytes = new byte[10485760];
                int BytesRec = socket.Receive(bytes);
                string messageServer = Encoding.UTF8.GetString(bytes, 0, BytesRec);

                ViewModelMessage viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(messageServer);

                if (viewModelMessage.Command == "authorization")
                    Id = int.Parse(viewModelMessage.Data);
                else if (viewModelMessage.Command == "message")
                    Console.WriteLine(viewModelMessage.Data);
                else if (viewModelMessage.Command == "cd")
                {
                    List<string> FoldersFiles = new List<string>();
                    FoldersFiles = JsonConvert.DeserializeObject<List<string>>(viewModelMessage.Data);
                    foreach (string Name in FoldersFiles)
                        Console.WriteLine(Name);
                }
                else if (viewModelMessage.Command == "file")
                {
                    string getFile = "";
                    for (int i = 1; i < DataMessage.Length; i++)
                        if (getFile == "")
                            getFile += DataMessage[i];
                        else
                            getFile += " " + DataMessage[i];
                    byte[] byteFile = JsonConvert.DeserializeObject<byte[]>(viewModelMessage.Data);
                    File.WriteAllBytes(getFile, byteFile);
                }
                socket.Close();
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Что-то случилось: " + exp.Message);
            }
        }
        static void Main(string[] args)
        {
            Console.Write("Введите IP адрес сервера: ");
            string sIpAddress = Console.ReadLine();
            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();
            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAddress, out IpAdress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Данные успешно введены. Подключаюсь к серверу.");
                while (true)
                {
                    ConnectServer();
                }
            }
        }
    }
}
