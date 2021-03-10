using System;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;

using CoinSpotUpdater.GoogleSheets;
using CoinSpotUpdater.CoinSpot;

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
            UpdateGoogleSpreadSheet();
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
            try
            {
                Repl();
            }
            catch (Exception e)
            {
                Line($"Error: {e.Message}");
                Repl();
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

                if (_commands.TryGetValue(input, out Command cmd))
                {
                    WriteDateTime();
                    Colored(_commands[input].Action, ConsoleColor.Yellow);
                }
                else
                {
                    Colored(() => Line("Type '?' for a list of commands."), ConsoleColor.Red);
                }
            }
        }

        private void WriteDateTime()
        {
            Colored(() => Line(DateTime.Now.ToString("dd MMM yy @HH:mm:ss")), ConsoleColor.Magenta);
        }

        private void Prompt()
        {
            Colored(() => Console.Write("# "), ConsoleColor.Green);
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
            Action("s", "Summary status of all holdings", ShowStatus);
            Action("g", "Show gain percent", ShowGainPercent);
            Action("u", "Update Google Spreadsheet", UpdateGoogleSpreadSheet);
            Action("b", "Balances of all coins", ShowBalances);
            Action("a", "Balances and summary", ShowAll);
            Action("p", "Get all Prices", ShowAllPrices);
            Action("td", "Total Deposits", ShowAllDeposits);
            Action("d", "Deposits", ShowDeposits);
            Action("buy", "Buy Orders", ShowBuyOrders);
            Action("sell", "Sell Orders", ShowSellOrders);
            Action("tr", "Transactions", ShowTransactions);
            Action("q", "Quit", () => _quit = true);
            Action("?", "help", ShowHelp);
        }

        private void ShowGainPercent()
        {
            var spent = _coinspotService.GetAllDeposits().GetTotalDeposited();
            var value = _coinspotService.GetPortfolioValue();
            var gain = value - spent;
            var gainPercent = (value / spent - 1.0f) * 100.0f;
            Colored(() => Line($"Gain %{gainPercent}"), ConsoleColor.Yellow);
        }

        private void ShowSellOrders()
            => Line(GetAllTransactions().SellOrdersToString());

        private void ShowBuyOrders()
            => Line(GetAllTransactions().BuyOrdersToString());

        private void ShowTransactions()
            => Line(GetAllTransactions());

        private void ShowDeposits()
            => Line(_coinspotService.GetAllDeposits());

        private void ShowAllDeposits()
            => Line($"Total deposited: {_coinspotService.GetAllDeposits().GetTotalDeposited()}");

        private void ShowAllPrices()
            => Line(_coinspotService.GetAllPrices());

        private CoinSpot.Dto.CoinSpotTransactions GetAllTransactions()
            => _coinspotService.GetAllTransactions();

        private CoinSpot.Dto.CoinSpotTransactions GetAllTransactions()
            => _coinspotService.GetAllTransactions();

        private void Action(string text, string desciption, Action action)
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
                Line($"{cmd.Description}");
                Console.ForegroundColor = color;
            }
        }

        private void UpdateGoogleSpreadSheet()
        {
            try
            {
                var value = _coinspotService.GetPortfolioValue();
                var now = DateTime.Now;
                var date = now.ToString("dd MMM yy");
                var time = now.ToLongTimeString();

                UpdateSummary(value, date, time);
                UpdateTable(value, now, time);

                Line("Updated SpreadSheet");
            }
            catch (Exception e)
            {
                Colored(() => Line($"Error updating: {e.Message}"), ConsoleColor.Red);
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

        private void ShowBalances()
        {
            var balances = _coinspotService.GetMyBalances();
            Colored(() => Console.Write(balances), ConsoleColor.Blue);
            Colored(() =>
            {
                Console.Write($"TOTAL: ");
                Line($"{balances.GetTotal():C}");
            }, ConsoleColor.Cyan);
        }

        private void ShowStatus()
        {
            var spent = _coinspotService.GetAllDeposits().GetTotalDeposited();
            var value = _coinspotService.GetPortfolioValue();
            var gain = value - spent;
            var gainPercent = (value / spent - 1.0f) * 100.0f;

            Line($"Spent = {spent:C}");
            Line($"Value = {value:C}");
            Line($"Gain$ = {gain:C}");
            Line($"Gain% = {gainPercent:0.##}");
        }
    }
}