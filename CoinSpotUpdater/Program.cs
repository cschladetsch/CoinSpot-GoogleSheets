using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;

namespace CryptoHelper
{
    using App;

    class Program
    {
        private bool _quit;
        private Timer _timer;
        private Commands _commands;
        private string _lastCommand;
        private Dictionary<string, Command> _commandMap = new Dictionary<string, Command>();

        static void Main(string[] args)
        {
            PrintHeader();
            new Program().Run(args);
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Line($"Crypto Updater v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            WriteLine();
        }

        public Program()
        {
            _commands = new Commands();
            PrepareUpdateTimer();
            AddActions();
            ShowHelp();
            WriteLine();
            _commands.ShowBalances();
            _commands.ShowStatus();
        }

        private void PrepareUpdateTimer()
        {
            var minutes = int.Parse(ConfigurationManager.AppSettings.Get("updateTimerMinutes"));
            if (minutes > 0)
            {
                Line($"Update timer set for {minutes} minutes");
                _timer = new Timer(TimerCallback, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(minutes));
            }
        }

        private void TimerCallback(object arg)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            WriteLine();
            Line("\nAuto-update:");
            WriteDateTime();
            _commands.UpdateGoogleSpreadSheet();
            Prompt();
        }

        private void Run(string[] args)
        {
            while (!_quit)
            {
                try
                {
                    Repl();
                }
                catch (Exception e)
                {
                    Line($"Error: {e.Message}");
                }
            }
        }

        public void ShowHelp(params string[] args)
        {
            foreach (var kv in _commandMap)
            {
                var cmd = kv.Value;
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{cmd.Text,12}  ");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Line($"{cmd.Description}");
                Console.ForegroundColor = color;
            }
        }

        private void Repl()
        {
            while (!_quit)
            {
                Prompt();
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    if (!string.IsNullOrEmpty(_lastCommand))
                    {
                        input = _lastCommand;
                    }
                    else
                    {
                        continue;
                    }
                }

                ProcessCommand(input);
            }
        }

        private void ProcessCommand(string input)
        {
            _lastCommand = input;
            var split = input.Split(' ');
            var cmd = split[0];
            if (_commandMap.TryGetValue(cmd, out Command command))
            {
                WriteDateTime();
                var args = split.Skip(1).ToArray();
                Colored(() => command.Action(args), ConsoleColor.Yellow);
            }
            else
            {
                Colored(() => Line("Type '?' for a list of commands."), ConsoleColor.Red);
            }
        }

        private void Prompt()
        {
            Colored(() => Console.Write("» "), ConsoleColor.DarkGray);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void AddActions()
        {
            AddAction("s", "Summary status of all holdings", _commands.ShowStatus);
            AddAction("g", "Show gain percent", _commands.ShowGainPercent);
            AddAction("u", "Update Google Spreadsheet", _commands.UpdateGoogleSpreadSheet);
            AddAction("b", "Balances of all coins", _commands.ShowBalances);
            AddAction("a", "Balances and summary", _commands.ShowAll);
            AddAction("p", "Get all Prices", _commands.ShowAllPrices);
            AddAction("o", "Open SpreadSheet", _commands.BrowseSheet);
            AddAction("c", "Open CoinSpot", OpenCoinSpot);
            AddAction("td", "Total Deposits", _commands.ShowAllDeposits);
            AddAction("!wd", "Write Deposits - clear table first!", _commands.WriteDeposits);
            AddAction("buys", "Buy Orders", _commands.ShowBuyOrders);
            AddAction("sells", "Sell Orders", _commands.ShowSellOrders);
            AddAction("buy", "Buy 'coin' 'aud'", _commands.Buy);
            AddAction("sell", "Sell 'coin' 'aud' ['rate']", _commands.Sell);
            AddAction("tr", "Transactions", _commands.ShowTransactions);
            AddAction("q", "Quit", (string[] args) => _quit = true);
            AddAction("?", "help", ShowHelp);
        }

        private void OpenCoinSpot(params string[] obj)
            => Process.Start("https://www.coinspot.com.au/my/dashboard");

        private void AddAction(string text, string desciption, Action<string[]> action)
            => _commandMap[text] = new Command(text, desciption, action);

        public static void Colored(Action action, ConsoleColor color)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            try
            {
                action();
            }
            catch (Exception e)
            {
                Colored(() => Line(e.Message), ConsoleColor.Red);
            }
            finally
            {
                Console.ForegroundColor = currentColor;
            }
        }

        private void WriteDateTime()
            => Colored(() => Line(DateTime.Now.ToString("dd MMM yy @HH:mm:ss")), ConsoleColor.Magenta);

        private static void WriteLine()
            => Console.WriteLine();

        public static void Line(object text)
            => Console.WriteLine(text);
    }
}