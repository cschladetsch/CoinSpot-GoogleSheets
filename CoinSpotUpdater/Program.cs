using System;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;

using CoinSpotUpdater.GoogleSheets;
using CoinSpotUpdater.CoinSpot;
using System.Threading.Tasks;

namespace CoinSpotUpdater
{
    class Program
    {
        // You will want to change these to match your local setup:
        // I have three main Sheets:
        //      1. Summary. Shows a summary of all spent and holdings
        //      2. Table. This is updated regularly by this app. Contains two tables: total value and total gain %
        //      3. Spent. A table that can be copy-pasted from CoinStop. This contains all your deposits.
        private const string TotalValueRange = "Summary!G6";
        private const string UpdateDateRange = "Summary!G4";
        private const string UpdateTimeRange = "Summary!H4";
        private const string ValueTable = "Table!B2";
        private const string GainsTable = "Table!G2";
        private const string SpentSourceRange = "Spent!G1";

        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();
        private GoogleSheetsService _googleSheetsService;
        private CoinspotService _coinspotService;
        private Timer _timer;
        private bool _quit;

        static void Main(string[] args)
        {
            PrintHeader();
            new Program().Run(args);
        }

        public Program()
        {
            _googleSheetsService = new GoogleSheetsService();
            _coinspotService = new CoinspotService();

            PrepareUpdateTimer();
            AddActions();
            ShowHelp();

            WriteLine();
            ShowStatus();
        }

        private void PrepareUpdateTimer()
        {
            var minutes = int.Parse(ConfigurationManager.AppSettings.Get("updateTimerMinutes"));
            if (minutes > 0)
            {
                WriteLine($"Update timer set for {minutes} minutes");
                _timer = new Timer(TimerCallback, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(minutes));
            }
        }

        private void TimerCallback(object arg)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            WriteLine();
            WriteLine("\nAuto-update:");
            WriteDateTime();
            UpdateGoogleSpreadSheet();
            WritePrompt();
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            WriteLine($"Crypto Updater v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            WriteLine();
        }

        private static void WriteLine()
            => Console.WriteLine();

        private static void WriteLine(object text)
            => Console.WriteLine(text);

        private void Run(string[] args)
        {
            try
            {
                Repl();
            }
            catch (Exception e)
            {
                WriteLine($"Error: {e.Message}");
                Repl();
            }
        }

        private void Repl()
        {
            while (!_quit)
            {
                WritePrompt();
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (input.StartsWith("call"))
                {
                    CallCoinSpot(input);
                    continue;
                }

                if (_commands.TryGetValue(input, out Command cmd))
                {
                    WriteDateTime();
                    WriteColored(_commands[input].Action, ConsoleColor.Yellow);
                }
                else
                {
                    WriteColored(() => WriteLine("Type '?' for a list of commands."), ConsoleColor.Red);
                }
            }
        }

        private void WriteDateTime()
        {
            WriteColored(() => WriteLine(DateTime.Now.ToString("dd MMM yy @HH:mm:ss")), ConsoleColor.Magenta);
        }

        private void WritePrompt()
        {
            WriteColored(() => Console.Write("# "), ConsoleColor.Green);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void WriteColored(Action action, ConsoleColor color)
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
            AddAction("s", "Summary status of all holdings", ShowStatus);
            AddAction("g", "Show gain percent", ShowGainPercent);
            AddAction("u", "Update Google Spreadsheet", UpdateGoogleSpreadSheet);
            AddAction("b", "Balances of all coins", ShowBalances);
            AddAction("a", "Balances and summary", ShowAll);
            AddAction("p", "Get all Prices", ShowAllPrices);
            AddAction("td", "Total Deposits", ShowAllDeposits);
            AddAction("d", "Deposits", ShowDeposits);
            AddAction("buy", "Buy Orders", ShowBuyOrders);
            AddAction("sell", "Sell Orders", ShowSellOrders);
            AddAction("tr", "Transactions", ShowTransactions);
            AddAction("q", "Quit", () => _quit = true);
            AddAction("?", "help", ShowHelp);
        }

        private async void ShowGainPercent()
        {
            var spent = (await _coinspotService.GetAllDeposits()).GetTotalDeposited();
            var value = await _coinspotService.GetPortfolioValue();
            var gain = value - spent;
            var gainPercent = (value / spent - 1.0f) * 100.0f;
            WriteColored(() => WriteLine($"Gain %{gainPercent}"), ConsoleColor.Yellow);
        }

        private async void ShowSellOrders()
            => WriteLine((await GetAllTransactions()).SellOrdersToString());

        private async Task<CoinSpot.Dto.CoinSpotTransactions> GetAllTransactions()
            => await _coinspotService.GetAllTransactions();

        private async void ShowBuyOrders()
            => WriteLine((await GetAllTransactions()).BuyOrdersToString());

        private void ShowTransactions()
            => WriteLine(GetAllTransactions());

        private void ShowDeposits()
            => WriteLine(_coinspotService.GetAllDeposits());

        private async void ShowAllDeposits()
            => WriteLine($"Total deposited: {(await _coinspotService.GetAllDeposits()).GetTotalDeposited()}");

        private void ShowAllPrices()
            => WriteLine(_coinspotService.GetAllPrices());

        private void AddAction(string text, string desciption, Action action)
            => _commands[text] = new Command(text, desciption, action);

        private void ShowAll()
        {
            ShowStatus();
            ShowBalances();
        }

        private void ShowHelp()
        {
            foreach (var kv in _commands)
            {
                var cmd = kv.Value;
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{cmd.Text,6}  ");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                WriteLine($"{cmd.Description}");
                Console.ForegroundColor = color;
            }
        }

        private async void UpdateGoogleSpreadSheet()
        {
            try
            {
                var value = await _coinspotService.GetPortfolioValue();
                var now = DateTime.Now;
                var date = now.ToString("dd MMM yy");
                var time = now.ToLongTimeString();

                UpdateSummary(value, date, time);
                UpdateTable(value, now, time);

                WriteLine("Updated SpreadSheet");
            }
            catch (Exception e)
            {
                WriteColored(() => WriteLine($"Error updating: {e.Message}"), ConsoleColor.Red);
            }
        }

        private void UpdateSummary(float value, string date, string time)
        {
            _googleSheetsService.SetValue(UpdateDateRange, date);
            _googleSheetsService.SetValue(UpdateTimeRange, time);
            _googleSheetsService.SetValue(TotalValueRange, value);
        }

        private void UpdateTable(float value, DateTime now, string time)
        {
            var list = new List<object>
            {
                now.ToString("dd MMM yy"),
                time,
                "=" + SpentSourceRange,
                value,
            };

            var appended = _googleSheetsService.Append(ValueTable, list);
            AppendGainsTable(appended.TableRange);
        }

        internal void AppendGainsTable(string tableRange)
        {
            var last = tableRange.LastIndexOf(':');
            var bottomRight = tableRange.Substring(last + 1);
            int index = 0;
            while (char.IsLetter(bottomRight[index]))
            {
                index++;
            }
            int row = int.Parse(bottomRight.Substring(index)) + 1;
            var list = new List<object>
            {
                $"=E{row}-D{row}",
                $"=G{row}/D{row}"
            };
            _googleSheetsService.Append(GainsTable, list);
        }

        private async void ShowBalances()
        {
            var balances = await _coinspotService.GetMyBalances();
            WriteColored(() => Console.Write(balances), ConsoleColor.Blue);
            WriteColored(() =>
            {
                Console.Write($"TOTAL: ");
                WriteLine($"{balances.GetTotal():C}");
            }, ConsoleColor.Cyan);
        }

        private async void ShowStatus()
        {
            var spent = await _coinspotService.GetAllDeposits();
            var value = await _coinspotService.GetPortfolioValue();
            var total = spent.GetTotalDeposited();
            var gain = value - spent.GetTotalDeposited();
            var gainPercent = (value / total - 1.0f) * 100.0f;

            WriteLine($"Spent = {total:C}");
            WriteLine($"Value = {value:C}");
            WriteLine($"Gain$ = {gain:C}");
            WriteLine($"Gain% = {gainPercent:0.##}");
        }

        private void CallCoinSpot(string input)
        {
            var prefix = "/api/ro/";
            var url = input.Substring(5);
            WriteLine(_coinspotService.PrivateApiCall(prefix + url, "{}"));
        }
    }
}