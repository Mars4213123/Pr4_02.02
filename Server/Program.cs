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
        private static UserRepository _userRepository;
        private static CommandRepository _commandRepository;
        public static IPAddress IpAdress;
        public static int Port;

        static void Main(string[] args)
        {
            _userRepository = new UserRepository();
            _commandRepository = new CommandRepository();


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
            return _userRepository.ValidateUser(login, password);
        }

        public static int GetUserId(string login, string password)
        {
            return _userRepository.GetUserId(login, password);
        }

        public static List<string> GetDirectory(string src)
        {
            List<string> FoldersFiles = new List<string>();

            if (System.IO.Directory.Exists(src))
            {
                string[] dirs = System.IO.Directory.GetDirectories(src);
                foreach (string dir in dirs)
                {
                    string NameDirectory = System.IO.Path.GetFileName(dir);
                    FoldersFiles.Add(NameDirectory);
                }

                string[] files = System.IO.Directory.GetFiles(src);
                foreach (string file in files)
                {
                    string NameFile = System.IO.Path.GetFileName(file);
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
                    string commandName = DataCommand[0];
                    string parameters = DataCommand.Length > 1 ?
                        string.Join(" ", DataCommand.Skip(1)) : "";

                    if (commandName == "connect")
                    {
                        if (DataCommand.Length >= 3 && AutorizationUser(DataCommand[1], DataCommand[2]))
                        {
                            int IdUser = GetUserId(DataCommand[1], DataCommand[2]);
                            viewModelMessage = new ViewModelMessage("authorization", IdUser.ToString());
                            _commandRepository.LogCommand(IdUser, "connect", $"login: {DataCommand[1]}", "Успешная авторизация");
                        }
                        else
                        {
                            viewModelMessage = new ViewModelMessage("message", "Не правильный логин и пароль пользователя.");
                            _commandRepository.LogCommand(-1, "connect", $"login: {DataCommand[1]}", "Ошибка авторизации");
                        }

                        byte[] message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(message);
                    }
                    else if (commandName == "cd")
                    {
                        if (ViewModelSend.Id == -1)
                        {
                            viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            _commandRepository.LogCommand(-1, "cd", parameters, "Ошибка: не авторизован");
                        }
                        else
                        {
                            var user = _userRepository.GetUserById(ViewModelSend.Id);
                            List<string> FoldersFiles = new List<string>();

                            if (DataCommand.Length == 1)
                            {
                                user.CurrentDirectory = user.BaseDirectory;
                                FoldersFiles = GetDirectory(user.CurrentDirectory);
                                _userRepository.UpdateUser(user);
                            }
                            else
                            {
                                string cdfolder = parameters;
                                string newPath = System.IO.Path.Combine(user.CurrentDirectory, cdfolder);

                                if (newPath.StartsWith(user.BaseDirectory))
                                {
                                    if (System.IO.Directory.Exists(newPath))
                                    {
                                        user.CurrentDirectory = newPath;
                                        FoldersFiles = GetDirectory(user.CurrentDirectory);
                                        _userRepository.UpdateUser(user);
                                    }
                                    else
                                    {
                                        viewModelMessage = new ViewModelMessage("message", "Директория не существует");
                                        byte[] messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                                        Handler.Send(messageBytes);
                                        _commandRepository.LogCommand(ViewModelSend.Id, "cd", parameters, "Ошибка: директория не существует");
                                        continue;
                                    }
                                }
                                else
                                {
                                    viewModelMessage = new ViewModelMessage("message", "Доступ запрещен");
                                    byte[] messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                                    Handler.Send(messageBytes);
                                    _commandRepository.LogCommand(ViewModelSend.Id, "cd", parameters, "Ошибка: доступ запрещен");
                                    continue;
                                }
                            }

                            if (FoldersFiles.Count == 0)
                            {
                                viewModelMessage = new ViewModelMessage("message", "Директория пуста или не существует.");
                                _commandRepository.LogCommand(ViewModelSend.Id, "cd", parameters, "Директория пуста");
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("cd", JsonConvert.SerializeObject(FoldersFiles));
                                _commandRepository.LogCommand(ViewModelSend.Id, "cd", parameters, $"Успешно: {FoldersFiles.Count} элементов");
                            }
                        }
                        byte[] message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(message);
                    }
                    else if (commandName == "get")
                    {
                        if (ViewModelSend.Id == -1)
                        {
                            viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            _commandRepository.LogCommand(-1, "get", parameters, "Ошибка: не авторизован");
                        }
                        else
                        {
                            var user = _userRepository.GetUserById(ViewModelSend.Id);
                            string getFile = parameters;
                            string filePath = System.IO.Path.Combine(user.CurrentDirectory, getFile);
                            filePath = System.IO.Path.GetFullPath(filePath);

                            Console.WriteLine($"Поиск файла: {filePath}");

                            if (System.IO.File.Exists(filePath))
                            {
                                try
                                {
                                    byte[] byteFile = System.IO.File.ReadAllBytes(filePath);
                                    viewModelMessage = new ViewModelMessage("file", JsonConvert.SerializeObject(byteFile));
                                    Console.WriteLine($"Файл найден и отправлен: {filePath}");
                                    _commandRepository.LogCommand(ViewModelSend.Id, "get", parameters, $"Успешно: {byteFile.Length} байт");
                                }
                                catch (Exception ex)
                                {
                                    viewModelMessage = new ViewModelMessage("message", $"Ошибка чтения файла: {ex.Message}");
                                    _commandRepository.LogCommand(ViewModelSend.Id, "get", parameters, $"Ошибка: {ex.Message}");
                                }
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", $"Файл не найден: {filePath}");
                                _commandRepository.LogCommand(ViewModelSend.Id, "get", parameters, "Ошибка: файл не найден");
                            }
                        }
                        byte[] responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(responseBytes);
                    }
                    else if (commandName == "set")
                    {
                        if (ViewModelSend.Id == -1)
                        {
                            viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            _commandRepository.LogCommand(-1, "set", parameters, "Ошибка: не авторизован");
                        }
                        else
                        {
                            var user = _userRepository.GetUserById(ViewModelSend.Id);
                            FileInfoFTP SendFileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(ViewModelSend.Message);
                            string filePath = System.IO.Path.Combine(user.CurrentDirectory, SendFileInfo.Name);

                            System.IO.File.WriteAllBytes(filePath, SendFileInfo.Data);
                            viewModelMessage = new ViewModelMessage("message", "Файл загружен");
                            _commandRepository.LogCommand(ViewModelSend.Id, "set", $"file: {SendFileInfo.Name}", $"Успешно: {SendFileInfo.Data.Length} байт");
                        }

                        byte[] message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(message);
                    }
                    else
                    {
                        viewModelMessage = new ViewModelMessage("message", "Неизвестная команда");
                        _commandRepository.LogCommand(ViewModelSend.Id, "unknown", parameters, "Неизвестная команда");

                        byte[] message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(message);
                    }

                    Handler.Close();
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