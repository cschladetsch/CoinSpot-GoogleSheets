using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace CoinSpotUpdater
{
    class Program
    {
        private const string TotalValueRange = "Summary!G6";
        private const string UpdateDateRange = "Summary!G4";
        private const string UpdateTimeRange = "Summary!H4";
        private GoogleSheetsService _googleSheetsService;
        private CoinspotService _coinspotService;
        private Dictionary<string, Command> _commands = new Dictionary<string, Command>();
        private bool _quit;
        private Timer _timer;

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
            Console.WriteLine();
        }

        
        private void PrepareUpdateTimer()
        {
            var minutes = int.Parse(ConfigurationManager.AppSettings.Get("updateTimerPeriod"));
            if (minutes > 0)
            {
                _timer = new Timer(TimerCallback, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(minutes));
            }
        }

        private void TimerCallback(object arg)
        {
            Console.WriteLine();
            Console.WriteLine("\nAuto-update:");
            WriteDateTime();
            ShowBalances();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            UpdateGoogleSpreadSheet();
            WritePrompt();
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Crypto Updater v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }

        private void Run(string[] args)
        {
            try
            {
                Repl();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
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
                    WriteColored(() => Console.WriteLine("Type 'help' for a list of commands."), ConsoleColor.Red);
                }
            }
        }

        private void WriteDateTime()
        {
            WriteColored(() => Console.WriteLine(DateTime.Now), ConsoleColor.Magenta);
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
            action();
            Console.ForegroundColor = currentColor;
        }

        private void AddActions()
        {
            AddAction("g", "Show total gains as a percent of spent", ShowGainPercent);
            AddAction("s", "Show summary status of all holdings", ShowStatus);
            AddAction("u", "Update Google Spreadsheet", UpdateGoogleSpreadSheet);
            AddAction("b", "Show balances of all coins", ShowBalances);
            AddAction("q", "Quit", () => _quit = true);
            AddAction("a", "Show balances and summary", ShowAll);
            AddAction("?", "Show help", ShowHelp);
        }

        private void AddAction(string text, string desciption, Action action)
        {
            _commands[text] = new Command(text, desciption, action);
        }

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
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{cmd.Description}");
                Console.ForegroundColor = color;
            }
        }

        private void ShowGainPercent()
        {
            UpdateGoogleSpreadSheet();
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(.5f));
            var entries = _googleSheetsService.GetRange("Summary!G7:G8");
            var dollar = entries[0][0];
            var percent = entries[1][0];
            Console.WriteLine($"Gain {dollar:C}, {percent:0.##}");
        }

        private void UpdateGoogleSpreadSheet()
        {
            float value = _coinspotService.GetPortfolioValue();
            var now = DateTime.Now;
            var date = now.ToString("dd MMM yy");
            var time = now.ToLongTimeString();

            UpdateSummary(value, date, time);
            UpdateTable(value, now, time);

            Console.WriteLine("Updated SpreadSheet");
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
                "=Transactions!$C$1",
                value,
            };

            var appended = _googleSheetsService.Append("Table!B2", list);
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
            _googleSheetsService.Append("Table!G2", list);
        }

        private void ShowBalances()
        {
            var balances = _coinspotService.GetMyBalances();
            WriteColored(() => Console.Write(balances), ConsoleColor.Blue);
            WriteColored(() => { 
                Console.Write($"TOTAL: ");
                Console.WriteLine($"{balances.GetTotal():C} AUD");
            }, ConsoleColor.Cyan);
        }

        private void ShowStatus()
        {
            var entries = _googleSheetsService.GetRange("Summary!G5:G8");
            var spent = entries[0][0];
            var value = entries[1][0];
            var gain = entries[2][0];
            var gainPercent = entries[3][0];
            Console.WriteLine($"Spent = {spent:C}");
            Console.WriteLine($"Value = {value:C}");
            Console.WriteLine($"Gain$ = {gain:C}");
            Console.WriteLine($"Gain% = {gainPercent:0.##}");
        }

        private void CallCoinSpot(string input)
        {
            var prefix = "/api/ro/";
            var url = input.Substring(5);
            Console.WriteLine(_coinspotService.ApiCall(prefix + url, "{}"));
        }

        private float GetSpreadSheetValue()
        {
            var result = _googleSheetsService.GetRange(TotalValueRange);
            var text = result[0][0].ToString().Substring(1);
            return float.Parse(text);
        }
    }
}