using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Samples.CustomEvents.Utility
{
    class UI
    {
        int _maxDisplayMessages = 10;
        public string _currentUser;
        Queue<Msg> _messageList;
        public string CurrentUser
        {
            get { return _currentUser; }
            set { _currentUser = value; }
        }
        public enum dCode
        {
            SignIn = 0,
            Message = 1
        }
        public UI()
        {
            _messageList = new Queue<Msg>(_maxDisplayMessages);

        }
        public void Display(dCode code , Msg msg , List<string> connectedUsers)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("================");
            Console.WriteLine("NCACHE CHAT ROOM");
            Console.WriteLine("================");
            Console.WriteLine("");
            Console.ResetColor();
            switch (code)
            {
                case dCode.SignIn:
                    Console.WriteLine("Please write your screen name: ");
                    return;
                case dCode.Message:
                    
                    Console.WriteLine("Online Users : ");
                    string users = "";
                    foreach (var user in connectedUsers)
                    {
                        users = users + user + " ";
                    }
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(users);
                    Console.ResetColor();
                    Console.WriteLine("________________________________________________________");
                    _messageList.Enqueue(msg);
                    if(_messageList.Count > _maxDisplayMessages)
                    {
                        _messageList.Dequeue();
                    }
                    foreach (var message in _messageList)
                    {
                        if (message.From == _currentUser)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                        }
                        Console.WriteLine(message.From + " : "+ (string)message.Data);
                        
                    }
                    Console.ResetColor();
                    Console.WriteLine("________________________________________________________");
                    Console.WriteLine();
                    Console.WriteLine("Type exit to exit the chat room");
                    Console.WriteLine(_currentUser + ":");
                    break;

            }
        }

    }
}
