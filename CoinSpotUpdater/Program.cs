using System;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;

using CoinSpotUpdater.GoogleSheets;
using CoinSpotUpdater.CoinSpot;
using System.Linq;

namespace CoinSpotUpdater
{
    class Program
    {
        // You will want to change these to match your local setup in App.config.
        public static string SpentRange;
        public static string UpdateDateRange;
        public static string TotalValueRange;
        public static string UpdateTimeRange;
        public static string ValueTable;
        public static string GainsTable;

        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();
        private GoogleSheetsService _googleSheetsService;
        private CoinspotService _coinspotService;
        private Timer _timer;
        private bool _quit;
        private Commands _command;

        static void Main(string[] args)
        {
            PrintHeader();
            new Program().Run(args);
        }

        public static string FromAppSettings(string key)
            => ConfigurationManager.AppSettings.Get(key);

        public Program()
        {
            GetSettings();

            _googleSheetsService = new GoogleSheetsService();
            _coinspotService = new CoinspotService();

            _command = new Commands(this);

            PrepareUpdateTimer();
            AddActions();
            ShowHelp(null);

            WriteLine();
            _command.ShowBalances(null);
            Colored(() => _command.ShowStatus(null), ConsoleColor.Yellow);
        }

        internal CoinspotService GetCoinspotService() => _coinspotService;
        internal GoogleSheetsService GetGoogleSheetsService() => _googleSheetsService;

        public void ShowHelp(string[] args)
        {
            foreach (var kv in _commands)
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

        private void GetSettings()
        {
            SpentRange = FromAppSettings("SpentRange");
            UpdateDateRange = FromAppSettings("UpdateDateRange");
            TotalValueRange = FromAppSettings("TotalValueRange");
            UpdateTimeRange = FromAppSettings("UpdateTimeRange");
            ValueTable = FromAppSettings("ValueTable");
            GainsTable = FromAppSettings("GainsTable");
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
            _command.UpdateGoogleSpreadSheet(null);
            Prompt();
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Line($"Crypto Updater v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            WriteLine();
        }

        private static void WriteLine()
            => Console.WriteLine();

        public static void Line(object text)
            => Console.WriteLine(text);

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

        private void Repl()
        {
            while (!_quit)
            {
                Prompt();
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                var split = input.Split(' ');
                var cmd = split[0];
                if (_commands.TryGetValue(cmd, out Command command))
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
        }

        private void WriteDateTime()
            => Colored(() => Line(DateTime.Now.ToString("dd MMM yy @HH:mm:ss")), ConsoleColor.Magenta);

        private void Prompt()
        {
            Colored(() => Console.Write("» "), ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.White;
        }

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

        private void AddActions()
        {
            AddAction("s", "Summary status of all holdings", _command.ShowStatus);
            AddAction("g", "Show gain percent", _command.ShowGainPercent);
            AddAction("u", "Update Google Spreadsheet", _command.UpdateGoogleSpreadSheet);
            AddAction("b", "Balances of all coins", _command.ShowBalances);
            AddAction("a", "Balances and summary", _command.ShowAll);
            AddAction("p", "Get all Prices", _command.ShowAllPrices);
            AddAction("td", "Total Deposits", _command.ShowAllDeposits);
            AddAction("wd", "Write Deposits - clear table first!", _command.WriteDeposits);
            AddAction("buy_orders", "Buy Orders", _command.ShowBuyOrders);
            AddAction("sell_orders", "Sell Orders", _command.ShowSellOrders);
            AddAction("sell", "Sell 'coin' 'aud' ['rate']", _command.Sell);
            AddAction("buy", "Buy 'coin' 'aud'", _command.Buy);
            AddAction("tr", "Transactions", _command.ShowTransactions);
            AddAction("q", "Quit", (string[] args) => _quit = true);
            AddAction("?", "help", ShowHelp);
        }

        private void AddAction(string text, string desciption, Action<string[]> action)
            => _commands[text] = new Command(text, desciption, action);

    }
}