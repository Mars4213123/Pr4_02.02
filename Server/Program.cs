using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;
using Server.Classes;

namespace Server
{
    class Program
    {
        public static List<User> Users = new List<User>();
        public static IPAddress IpAdress;
        public static int Port;
        static void Main(string[] args)
        {

            Users.Add(new User("asd", "asdfg123", @"C:\"));
            Console.WriteLine("Введите IP адрес сервера: ");
            string sIpAdress = Console.ReadLine();
            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();
            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IpAdress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Данные успешно введены. Запускаю сервер.");
                StartServer();
            }
            Console.Read();
        }
        public static bool AutorizationUser(string login, string password)
        {
            User user = Users.Find(x => x.login == login && x.password == password);
            return user != null;
        }
        public static List<string> GetDirectory(string src)
        {
            List<string> FoldersFiles = new List<string>();

            if (Directory.Exists(src))
            {
                string[] dirs = Directory.GetDirectories(src);
                foreach (string dir in dirs)
                {
                    string NameDirectory = Path.GetFileName(dir);
                    FoldersFiles.Add(NameDirectory);
                }

                string[] files = Directory.GetFiles(src);
                foreach (string file in files)
                {
                    string NameFile = Path.GetFileName(file);
                    FoldersFiles.Add(NameFile);
                }
            }

            return FoldersFiles;
        }
        public static void StartServer()
        {
            IPEndPoint endpoint = new IPEndPoint(IpAdress, Port);
            Socket slistener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            slistener.Bind(endpoint);
            slistener.Listen(10);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Сервер запущен.");

            while (true)
            {
                try
                {
                    Socket Handler = slistener.Accept();
                    string Data = null;
                    byte[] bytes = new byte[10485760];
                    int bytesRec = Handler.Receive(bytes);
                    Data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    Console.Write("Сообщение от пользователя: " + Data + "\n");
                    

                    string Reply = "";

                    ViewModelMessage viewModelMessage;
                    ViewModelSend ViewModelSend = JsonConvert.DeserializeObject<ViewModelSend>(Data);

                    if (ViewModelSend == null) continue;

                    string[] DataCommand = ViewModelSend.Message.Split(' ');
                    if (DataCommand[0] == "connect")
                    {
                        if (AutorizationUser(DataCommand[1], DataCommand[2]))
                        {
                            int IdUser = Users.FindIndex(x => x.login == DataCommand[1] && x.password == DataCommand[2]);
                            viewModelMessage = new ViewModelMessage("authorization", IdUser.ToString());
                        }
                        else
                        {
                            viewModelMessage = new ViewModelMessage("message", "Не правильный логин и пароль пользователя.");
                        }


                        byte[] message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(message);
                    }
                    else if (DataCommand[0] == "cd")
                    {
                        if (ViewModelSend.Id == -1)
                        {
                            viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                        }
                        else
                        {
                            List<string> FoldersFiles = new List<string>();

                            if (DataCommand.Length == 1)
                            {
                                Users[ViewModelSend.Id].temp_src = Users[ViewModelSend.Id].src;
                                FoldersFiles = GetDirectory(Users[ViewModelSend.Id].temp_src);
                            }
                            else
                            {
                                string cdfolder = "";
                                for (int i = 1; i < DataCommand.Length; i++)
                                {
                                    if (cdfolder == "")
                                        cdfolder += DataCommand[i];
                                    else
                                        cdfolder += " " + DataCommand[i];
                                }

                                string newPath = Path.Combine(Users[ViewModelSend.Id].temp_src, cdfolder);
                                if (newPath.StartsWith(Users[ViewModelSend.Id].src))
                                {
                                    Users[ViewModelSend.Id].temp_src = newPath;
                                    FoldersFiles = GetDirectory(Users[ViewModelSend.Id].temp_src);
                                }
                                else
                                {
                                    viewModelMessage = new ViewModelMessage("message", "Доступ запрещен");
                                    byte[] messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                                    Handler.Send(messageBytes);
                                    continue;
                                }
                            }

                            if (FoldersFiles.Count == 0)
                                viewModelMessage = new ViewModelMessage("message", "Директория пуста или не существует.");
                            else
                                viewModelMessage = new ViewModelMessage("cd", JsonConvert.SerializeObject(FoldersFiles));
                            
                        }
                        byte[] message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(message);
                    }
                    else if (DataCommand[0] == "get")
                    {
                        if (ViewModelSend.Id == -1)
                        {
                            viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                        }
                        else
                        {
                            string getFile = "";
                            for (int i = 1; i < DataCommand.Length; i++)
                            {
                                if (getFile == "")
                                    getFile += DataCommand[i];
                                else
                                    getFile += " " + DataCommand[i];
                            }

                            string filePath = Path.Combine(Users[ViewModelSend.Id].temp_src, getFile);
                            filePath = Path.GetFullPath(filePath);

                            Console.WriteLine($"Поиск файла: {filePath}");

                            if (File.Exists(filePath))
                            {
                                try
                                {
                                    byte[] byteFile = File.ReadAllBytes(filePath);
                                    viewModelMessage = new ViewModelMessage("file", JsonConvert.SerializeObject(byteFile));
                                    Console.WriteLine($"Файл найден и отправлен: {filePath}");
                                }
                                catch (Exception ex)
                                {
                                    viewModelMessage = new ViewModelMessage("message", $"Ошибка чтения файла: {ex.Message}");
                                }
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", $"Файл не найден: {filePath}");
                                var availableFiles = Directory.GetFiles(Users[ViewModelSend.Id].temp_src);
                                Console.WriteLine($"Доступные файлы в {Users[ViewModelSend.Id].temp_src}: {string.Join(", ", availableFiles)}");
                            }
                        }
                        byte[] responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(responseBytes);
                    }
                    else
                    {
                        if (ViewModelSend.Id == -1)
                            viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                        else {
                            FileInfoFTP SendFileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(ViewModelSend.Message);
                            File.WriteAllBytes(Users[ViewModelSend.Id].temp_src + @"\" + SendFileInfo.Name, SendFileInfo.Data);

                            viewModelMessage = new ViewModelMessage("message", "Файл загружен");
                        }

                        byte[] message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(message);
                    }
                }
                catch (Exception exp)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Что-то случилось: " + exp.Message);
                }
            }
        }
    }
}
