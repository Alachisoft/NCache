using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Alachisoft.Samples.GuessGameCore.Controllers
{
    public class GuessGameController : Controller
    {
        private const string GuessedNumber = "GuessedNumber";
        private const string History = "History";
        private const string LastValue = "LastValue";
        private const string Victory = "YouWin";
        private const string IsGreater = "IsGreater";

        // GET: /<controller>/
        public IActionResult Index()
        {
            StartNewGame();
            return View();
        }

        public IActionResult Guess(string id)
        {
            ViewData[Victory] = false;
            if (id != null)
            {
                byte[] historyBytes;
                List<int> history = null;
                if (HttpContext.Session.TryGetValue(History, out historyBytes))
                {
                    history = DeserializeHistory(historyBytes);
                    ViewData[History] = history;


                    int? number = HttpContext.Session.GetInt32(GuessedNumber);
                    if (number != null)
                    {
                        ViewData[GuessedNumber] = number;
                        int guessedNumber;
                        if (int.TryParse(id, out guessedNumber))
                        {
                            if (number.Value.Equals(guessedNumber))
                            {
                                ViewData[Victory] = true;
                            }
                            else
                            {
                                if (guessedNumber > number.Value)
                                {
                                    ViewData[IsGreater] = true;
                                }
                                else
                                {
                                    ViewData[IsGreater] = false;
                                }
                            }
                            history?.Add(guessedNumber);
                            ViewData[LastValue] = guessedNumber;
                        }
                    }

                    historyBytes = SerializeHistory(history);
                    HttpContext.Session.Set(History, historyBytes);
                }
				else
				{
					return NewGame();
				}
            }


            return View("Index");
        }

        public IActionResult NewGame()
        {
            HttpContext.Session.Clear();
            StartNewGame();
            return View("Index");
        }

        private void StartNewGame()
        {
            int number = HttpContext.Session.GetInt32(GuessedNumber) ??
                        new Random(DateTime.Now.Millisecond).Next(0, 100);

            ViewData[GuessedNumber] = number;
            HttpContext.Session.SetInt32(GuessedNumber, number);

            byte[] historyBytes;
            List<int> history;
            if (HttpContext.Session.TryGetValue(History, out historyBytes))
            {
                history = DeserializeHistory(historyBytes);
                if (history.Count > 0)
                {
                    ViewData[History] = history;
                    ViewData[LastValue] = history[history.Count - 1];
                }
            }
            else
            {
                history = new List<int>();
                historyBytes = SerializeHistory(history);
                HttpContext.Session.Set(History, historyBytes);
            }
        }

        private byte[] SerializeHistory(List<int> history)
        {
            using (var stream = new MemoryStream())
            {
                WriteInt32(stream, history.Count);
                foreach (var i in history)
                {
                    WriteInt32(stream, i);
                }
                return stream.GetBuffer();
            }
        }

        private List<int> DeserializeHistory(byte[] bytes)
        {
            List<int> array;
            using (var stream = new MemoryStream(bytes))
            {
                int length = ReadInt32(stream);
                array = new List<int>();
                for (int i = 0; i < length; i++)
                    array.Add(ReadInt32(stream));
            }
            return array;
        }

        private static void WriteInt32(MemoryStream stream, int value)
        {
            unchecked
            {
                stream.WriteByte((byte)(value >> 24));
                stream.WriteByte((byte)(value >> 16));
                stream.WriteByte((byte)(value >> 8));
                stream.WriteByte((byte)value);
            }
        }

        private static int ReadInt32(MemoryStream stream)
        {
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            int b3 = stream.ReadByte();
            int b4 = stream.ReadByte();
            return ((b1 << 24) | (b2 << 16) | (b3 << 8) | (b4 << 0));
        }
    }
}
