using System;
using System.Collections.Generic;

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

        static void Main(string[] args)
        {
            Console.WriteLine($"Crypto Updater v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine();
            new Program().Run(args);
        }

        private void Run(string[] args)
        {
            _googleSheetsService = new GoogleSheetsService();
            _coinspotService = new CoinspotService();

            AddActions();
            ShowHelp();
            Console.WriteLine();

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
                Console.Write("> ");
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
                    _commands[input].Action();
                }
                else
                {
                    Console.WriteLine("Type 'help' for a list of commands.");
                }
            }
        }

        private void AddActions()
        {
            _commands["g"] = new Command("g", "Show total gains as a percent of spent", ShowGainPercent);
            _commands["st"] = new Command("st", "Show status of all holdings", ShowStatus);
            _commands["up"] = new Command("up", "Update Google Spreadsheet", UpdateGoogleSpreadSheet);
            _commands["bal"] = new Command("bal", "Show balances of all coins", ShowBalances);
            _commands["q"] = new Command("q", "Quit", () => _quit = true);
            _commands["help"] = new Command("help", "Show help", ShowHelp);
        }

        private void ShowHelp()
        {
            foreach (var kv in _commands)
            {
                var cmd = kv.Value;
                Console.WriteLine($"'{cmd.Text:5}' {cmd.Description}");
            }
        }

        private void ShowGainPercent()
        {
            var entries = _googleSheetsService.GetRange("Summary!G8");
            Console.WriteLine($"Gain= {entries[0][0]:0.##}");
        }

        private void UpdateGoogleSpreadSheet()
        {
            _googleSheetsService.SetValue(TotalValueRange, _coinspotService.GetPortfolioValue());
            _googleSheetsService.SetValue(UpdateDateRange, DateTime.Now.ToString("dd MMM"));
            _googleSheetsService.SetValue(UpdateTimeRange, DateTime.Now.ToShortTimeString());
            Console.WriteLine("Updated SpreadSheet");// https://docs.google.com/spreadsheets/d/1RsgO5LVA55SLz5JdJpK9o9gWNlpm-1TjWm5_IzTEyEU");
        }

        private void ShowBalances()
        {
            var balances = _coinspotService.GetMyBalances();
            Console.WriteLine(balances);
            Console.WriteLine($"TOTAL: ${balances.GetTotal()} AUD");
        }

        private void ShowStatus()
        {
            var entries = _googleSheetsService.GetRange("Summary!G5:G8");
            var spent = entries[0][0];
            var value = entries[1][0];
            var gain = entries[2][0];
            var gainPercent = entries[3][0];
            Console.WriteLine($"Spent= {spent:C}, Value= {value:C}");
            Console.WriteLine($"Gain = {gain:C},  Gain= %{gainPercent:0.##}");
        }

        private void CallCoinSpot(string input)
        {
            var prefix = "/api/ro/";
            var url = input.Substring(5);
            Console.WriteLine(_coinspotService.CallAPI(prefix + url, "{}"));
        }

        private float GetSpreadSheetValue()
        {
            var result = _googleSheetsService.GetRange(TotalValueRange);
            var text = result[0][0].ToString().Substring(1);
            return float.Parse(text);
        }
    }
}