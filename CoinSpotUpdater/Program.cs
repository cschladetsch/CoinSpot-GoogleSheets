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
        // You will want to change these to match your local setup:
        // I have three main Sheets:
        //      1. Summary. Shows a summary of all spent and holdings
        //      2. Table. This is updated regularly by this app. Contains two tables: total value and total gain %
        private string SpentRange;
        private string UpdateDateRange;
        private string TotalValueRange;
        private string UpdateTimeRange;
        private string ValueTable;
        private string GainsTable;

        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();
        private GoogleSheetsService _googleSheetsService;
        private CoinspotService _coinspotService;
        private Timer _timer;
        private bool _quit;
        private float _lastDollar;
        private float _lastGainPercent;
        private string _bitFinexKey;
        private string _bitFinexSecret;
        private string _service = "cs";
        Bitfinex.Net.BitfinexClient _bitFinexClient;

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

            PrepareUpdateTimer();
            AddActions();
            ShowHelp(null);

            WriteLine();
            ShowBalances(null);
            Colored(() => ShowStatus(null), ConsoleColor.Yellow);
        }

        private void GetSettings()
        {
            SpentRange = FromAppSettings("SpentRange");
            UpdateDateRange = FromAppSettings("UpdateDateRange");
            TotalValueRange = FromAppSettings("TotalValueRange");
            UpdateTimeRange = FromAppSettings("UpdateTimeRange");
            ValueTable = FromAppSettings("ValueTable");
            GainsTable = FromAppSettings("GainsTable");

            try
            {
                _bitFinexKey = FromAppSettings("BitFinexKey");
                _bitFinexSecret = FromAppSettings("BitFinexSecret");
            }
            catch (Exception e)
            {
                Colored(() => Line($"Didn't connect to BitFinex: {e.Message}"), ConsoleColor.Cyan);
            }
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
            UpdateGoogleSpreadSheet(null);
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

        private static void Line(object text)
            => Console.WriteLine(text);

        private void Run(string[] args)
        {
            ChangeToCoinSpot(null);

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
            Colored(() => Console.Write($"{_service} » "), ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void Colored(Action action, ConsoleColor color)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            try
            {
                action();
            }
            finally
            {
                Console.ForegroundColor = currentColor;
            }
        }

        private void AddActions()
        {
            AddAction("bf", "Change to BitFinex", ChangeToBitFinex);
            AddAction("cs", "Change to CoinSpot", ChangeToCoinSpot);
            AddAction("s", "Summary status of all holdings", ShowStatus);
            AddAction("g", "Show gain percent", ShowGainPercent);
            AddAction("u", "Update Google Spreadsheet", UpdateGoogleSpreadSheet);
            AddAction("b", "Balances of all coins", ShowBalances);
            AddAction("a", "Balances and summary", ShowAll);
            AddAction("p", "Get all Prices", ShowAllPrices);
            AddAction("td", "Total Deposits", ShowAllDeposits);
            AddAction("wd", "Write Deposits - clear table first!", WriteDeposits);
            AddAction("buy_orders", "Buy Orders", ShowBuyOrders);
            AddAction("sell_orders", "Sell Orders", ShowSellOrders);
            AddAction("sell", "Sell 'coin' 'aud' ['rate']", Sell);
            AddAction("buy", "Buy 'coin' 'aud'", Buy);
            AddAction("tr", "Transactions", ShowTransactions);
            AddAction("q", "Quit", (string[] args) => _quit = true);
            AddAction("?", "help", ShowHelp);
        }

        private void ChangeToCoinSpot(string[] obj)
        {
            _coinspotService = new CoinspotService();
            try
            {
                _coinspotService.GetPortfolioValue();
            }
            catch (Exception e)
            {
                Colored(() => Line(e.Message) , ConsoleColor.Red);
                return;
            }

            _service = "CoinSpot";
        }

        private void ChangeToBitFinex(string[] obj)
        {
            Bitfinex.Net.Objects.BitfinexClientOptions options = new Bitfinex.Net.Objects.BitfinexClientOptions()
            {
                ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(_bitFinexKey, _bitFinexSecret)
            };
            _bitFinexClient = new Bitfinex.Net.BitfinexClient(options);
            var result = _bitFinexClient.GetAccountInfo();
            if (result.Success)
            {
                _service = "Bitfinex";
            }
        }

        private void Buy(string[] args)
        {
            _coinspotService.Buy(args[0], float.Parse(args[1]));
        }

        private void Sell(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                throw new ArgumentException("Sell epects 2-3 arguments");
            }

            if (args.Length == 2)
            {
                Colored(() => Line(_coinspotService.QuickSell(args[0], float.Parse(args[1]))), ConsoleColor.DarkRed);
            }
            else 
            {
                Colored(() => Line(_coinspotService.Sell(args[0], float.Parse(args[1]), float.Parse(args[2]))), ConsoleColor.DarkRed);
            }
        }

        private void ShowGainPercent(string[] args)
        {
            var spent = _coinspotService.GetAllDeposits().GetTotalDeposited();
            var value = _coinspotService.GetPortfolioValue();
            var gain = value - spent;
            var gainPercent = ((value / spent) - 1.0f) * 100.0f;
            if (_lastDollar == 0)
            {
                _lastGainPercent = gainPercent;
            }
            Colored(() => Line($"Gain %{gainPercent:0.##}"), ConsoleColor.Yellow);
            var diff = (gainPercent - _lastGainPercent);
            Colored(() => Line($"Diff %{diff:0.###}"), diff < 0 ? ConsoleColor.Red : ConsoleColor.Green);
            _lastGainPercent = gainPercent;
        }

        private void ShowSellOrders(string[] args)
            => Line(GetAllTransactions().SellOrdersToString());

        private void ShowBuyOrders(string[] args)
            => Line(GetAllTransactions().BuyOrdersToString());

        private void ShowTransactions(string[] args)
            => Line(GetAllTransactions());

        private void WriteDeposits(string[] args)
        {
            var deposits = _coinspotService.GetAllDeposits();
            Line(deposits);
            var items = new List<IList<object>>();
            foreach (var deposit in deposits.deposits)
            {
                items.Add(new List<object>()
                {
                    deposit.created.ToString("dd MMMM yyyy HH:mm:ss").Replace(".", ""),
                    deposit.amount
                });
            }
            _googleSheetsService.Append("Spent!A1", items);
        }

        private void ShowAllDeposits(string[] args)
            => Line($"Total deposited: {GetTotalSpent()}");

        private void ShowAllPrices(string[] args)
            => Line(_coinspotService.GetAllPrices());

        private CoinSpot.Dto.CoinSpotTransactions GetAllTransactions()
            => _coinspotService.GetAllTransactions();

        private void AddAction(string text, string desciption, Action<string[]> action)
            => _commands[text] = new Command(text, desciption, action);

        private void ShowAll(string[] args)
        {
            ShowStatus(args);
            ShowBalances(args);
        }

        private void ShowHelp(string[] args)
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

        private void UpdateGoogleSpreadSheet(string[] args)
        {
            try
            {
                var value = _coinspotService.GetPortfolioValue();
                var now = DateTime.Now;
                var date = now.ToString("dd MMM yy HH:mm:ss");
                var time = now.ToLongTimeString();

                UpdateSummary(value, date, time);
                UpdateTable(value, date);

                Line("Updated SpreadSheet");
            }
            catch (Exception e)
            {
                Colored(() => Line($"Error updating: {e.Message}"), ConsoleColor.Red);
            }
        }

        private void UpdateSummary(float value, string date, string time)
        {
            _googleSheetsService.SetValue(SpentRange, GetTotalSpent());
            _googleSheetsService.SetValue(UpdateDateRange, date);
            _googleSheetsService.SetValue(UpdateTimeRange, time);
            _googleSheetsService.SetValue(TotalValueRange, value);
        }

        private float GetTotalSpent()
            => _coinspotService.GetAllDeposits().GetTotalDeposited();

        private void UpdateTable(float value, string time)
        {
            var list = new List<object>
            {
                time,
                GetTotalSpent(),
                value,
            };

            var appended = _googleSheetsService.AppendList(ValueTable, list);
            AppendGainsTable(appended.TableRange);
        }

        internal void AppendGainsTable(string tableRange)
        {
            int row = 2;
            if (tableRange != null)
            {
                var last = tableRange.LastIndexOf(':');
                var bottomRight = tableRange.Substring(last + 1);
                int index = 0;
                while (char.IsLetter(bottomRight[index]))
                {
                    index++;
                }
                row = int.Parse(bottomRight.Substring(index)) + 1;
            }

            var list = new List<object>
            {
                $"=D{row}-C{row}",
                $"=F{row}/C{row}"
            };
            _googleSheetsService.AppendList(GainsTable, list);
        }

        private void ShowBalances(string[] args)
        {
            var balances = _coinspotService.GetMyBalances();
            Colored(() => Console.Write(balances), ConsoleColor.Blue);
            Colored(() =>
            {
                Console.Write($"TOTAL: ");
                Line($"{balances.GetTotal():C}");
            }, ConsoleColor.Cyan);
        }
        
        private void ShowStatus(string[] args)
        {
            var spent = GetTotalSpent();
            var value = _coinspotService.GetPortfolioValue();
            var gain = value - spent;
            var gainPercent = ((value / spent) - 1.0f) * 100.0f;

            if (_lastDollar == 0)
            {
                _lastDollar = value;
                _lastGainPercent = gainPercent;
            }

            Line($"Spent   = {spent:C}");
            Line($"Value   = {value:C}");
            Line($"Gain$   = {gain:C}");
            Line($"Gain%   = %{gainPercent:0.##}");

            var diffDollar = value - _lastDollar;
            var diffGain = gainPercent - _lastGainPercent;
            ConsoleColor diffColor = diffDollar < 0 ? ConsoleColor.Red : ConsoleColor.Green;
            Colored(() => Line($"  Diff$ = {value - _lastDollar:C}"), diffColor);
            Colored(() => Line($"  Diff% = %{gainPercent - _lastGainPercent:0.###}"), diffColor);

            _lastDollar = value;
            _lastGainPercent = gainPercent;
        }
    }
}