using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Web.Caching;
using System.Collections;
using System.Configuration;
using Alachisoft.NCache.Samples.CustomEvents.Utility;
using Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Samples.CustomEvents
{
    class ChatRoom
    {
        static List<string> _listUsers;
        static string _screenName;
        static string _cacheName;
        static UI _currentConsole;
        static Cache _cache;

        static void Main(string[] args)
        {
            /// Initializing private variables
            _listUsers = new List<string>();
            _screenName = "";
            _currentConsole = new UI();

            InitializeCache();

            /// Attempting to sign until successful
            while(_screenName == "")
            {
                SignIn();
            }

            Messaging();
            Dispose();

        }
        /// <summary>
        /// Inititializes the cache which will be used for chatting
        /// </summary>
        static void InitializeCache()
        {
            _cacheName = ConfigurationManager.AppSettings["CacheID"];
            /// Initialize cache takes a cache-id as argument. Specified cache-id
			/// must be registered. Initializatin won't fail if the cache is not running
			/// and you can start the cache later on while the application is running.
			/// You can use 'NCache Manager Application' or command line tools to 
			/// register and start caches. For more information see NCache help collection.
            
            _cache = NCache.Web.Caching.NCache.InitializeCache(_cacheName);
            _cache.ExceptionsEnabled = true;

        }

        /// <summary>
        /// Initial signing in of the user, takes a unique name and registers the custom events
        /// </summary>
        static void SignIn()
        {
            _currentConsole.Display(Utility.UI.dCode.SignIn, null, null);
            string user = Console.ReadLine();
            if(user.Length < 1)
            {
                return;
            }
            object result = null;
            try
            {
                /// Add the user as a non-evictable entity.
                result = _cache.Add("<user>" + user,
                    DateTime.UtcNow,
                    null,
                    Cache.NoAbsoluteExpiration,
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.NotRemovable
                    );

                /// Send a custom notification so that existing users can know that a new user has
                /// entered the conversation. The notification is sent asynchroneously.
                _cache.RaiseCustomEvent(Utility.Msg.Code.NewUser, user);

                _cache.CustomEvent += new CustomEventCallback(OnChatRoomEvent);
                PopulateUsersList();
                _screenName = user;
                _currentConsole.CurrentUser = user;

            }
            catch (Exception) /// If exceptions are enabled.
			{
            }
            if (result == null)
            {
                Console.WriteLine("The Screen name you have choosen is not available. Please select some other Screen name ! ");
                return;
            }
        }

        /// <summary>
        /// For initial population of online users list
        /// it populates the list with already online users
        /// </summary>
        static void PopulateUsersList()
        {
            _listUsers.Clear();
            try
            {
                IDictionaryEnumerator ide = (IDictionaryEnumerator)_cache.GetEnumerator();
                while (ide.MoveNext())
                {
                    string k = ide.Key as string;
                    if (k != null)
                    {
                        /// check if its a user key!
                        if (k.IndexOf("<user>") == 0)
                            _listUsers.Add(k.Substring(6));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured while scanning for users" + e.ToString());
            }
        }

        /// <summary>
        /// Method which runs infinitely until an exit command is provided
        /// Takes user input for the messages to be sent in the chat
        /// </summary>
        static void Messaging()
        {

            Msg InitialMessage = new Msg("Joined", Msg.Code.NewUser, _screenName, null);
            _currentConsole.Display(UI.dCode.Message, InitialMessage, _listUsers);

            while (true)
            {
                
                string message = Console.ReadLine();

                if (message.Length > 0)
                {
                    try
                    {
                        Utility.Msg msg;
                        if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            _cache.RaiseCustomEvent(Utility.Msg.Code.UserLeft, _screenName);
                            return;
                        }
                        else
                        {
                            msg = new Utility.Msg(message, Utility.Msg.Code.NewUser, _screenName, null);
                            _cache.RaiseCustomEvent(Utility.Msg.Code.Text,
                            Utility.Helper.ToByteBuffer(msg));
                        }

                    }
                    catch (Exception e)
                    {

                    }

                }
            }
        }

        /// <summary>
        /// Cache custom event that is raised everytime a change occurs like
        /// new user joining
        /// user leaving
        /// message recieved
        /// </summary>
        /// <param name="opcode"></param>
        /// <param name="operand"></param>
        static void OnChatRoomEvent(object opcode, object operand)
        {
            Utility.Msg.Code code= (Utility.Msg.Code)opcode;
            Utility.Msg msg;
            switch (code)
            {
                case Utility.Msg.Code.NewUser:
                    string userName = operand as string;
                    if (!_listUsers.Contains(userName))
                    {
                        _listUsers.Add(userName);
                        msg = new Utility.Msg("Just joined the chat room", Utility.Msg.Code.NewUser, userName, null);
                        _currentConsole.Display(Utility.UI.dCode.Message, msg, _listUsers);
                    }
                    break;

                case Utility.Msg.Code.UserLeft:
                    userName = operand as string;
                    if (_listUsers.Contains(userName)) // if user exist in list
                    {
                        _listUsers.Remove(userName);
                        msg = new Utility.Msg("Left the chat room", Utility.Msg.Code.NewUser, userName, null);
                        _currentConsole.Display(Utility.UI.dCode.Message, msg, _listUsers);
                    }
                    break;

                case Utility.Msg.Code.Text:
                    {
                        msg = Utility.Helper.FromByteBuffer(operand as byte[]) as Utility.Msg;
                        if (msg == null) return; /// not a message maybe
                        if ((msg.To == null))
                        {
                            _currentConsole.Display(Utility.UI.dCode.Message, msg, _listUsers);
                        }
                    }
                    break;
            }
        }


        static void Dispose()
        {
            _cache.Remove("<user>" + _screenName);
            _cache.Dispose();
        }
    }
}
