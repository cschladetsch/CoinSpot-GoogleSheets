using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinSpotUpdater
{
    class Commands
    {
        private Program _program;
        private CoinSpot.CoinspotService _coinspotService;
        private GoogleSheets.GoogleSheetsService _googleSheetsService;
        private float _lastDollar;
        private float _lastGainPercent;

        public Commands(Program program)
        {
            _program = program;
            _coinspotService = program.GetCoinspotService();
            _googleSheetsService = program.GetGoogleSheetsService();
        }

        public void Buy(string[] args)
        {
            _coinspotService.Buy(args[0], float.Parse(args[1]));
        }

        public void Sell(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                throw new ArgumentException("Sell epects 2-3 arguments");
            }

            if (args.Length == 2)
            {
                Program.Colored(() => Line(_coinspotService.QuickSell(args[0], float.Parse(args[1]))), ConsoleColor.DarkRed);
            }
            else 
            {
                Program.Colored(() => Line(_coinspotService.Sell(args[0], float.Parse(args[1]), float.Parse(args[2]))), ConsoleColor.DarkRed);
            }
        }

        public void ShowGainPercent(string[] args)
        {
            var spent = _coinspotService.GetAllDeposits().GetTotalDeposited();
            var value = _coinspotService.GetPortfolioValue();
            var gain = value - spent;
            var gainPercent = ((value / spent) - 1.0f) * 100.0f;
            if (_lastDollar == 0)
            {
                _lastGainPercent = gainPercent;
            }
            Program.Colored(() => Line($"Gain %{gainPercent:0.##}"), ConsoleColor.Yellow);
            var diff = (gainPercent - _lastGainPercent);
            Program.Colored(() => Line($"Diff %{diff:0.###}"), diff < 0 ? ConsoleColor.Red : ConsoleColor.Green);
            _lastGainPercent = gainPercent;
        }

        public void ShowSellOrders(string[] args)
            => Line(GetAllTransactions().SellOrdersToString());

        public void ShowBuyOrders(string[] args)
            => Line(GetAllTransactions().BuyOrdersToString());

        public void ShowTransactions(string[] args)
            => Line(GetAllTransactions());

        public void WriteDeposits(string[] args)
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

        public void ShowAllDeposits(string[] args)
            => Line($"Total deposited: {GetTotalSpent()}");

        public void ShowAllPrices(string[] args)
            => Line(_coinspotService.GetAllPrices());

        public CoinSpot.Dto.CoinSpotTransactions GetAllTransactions()
            => _coinspotService.GetAllTransactions();

        public void ShowAll(string[] args)
        {
            ShowStatus(args);
            ShowBalances(args);
        }

        public void UpdateGoogleSpreadSheet(string[] args)
        {
            try
            {
                var value = _coinspotService.GetPortfolioValue();
                var now = DateTime.Now;
                var date = now.ToString("dd MMM yy HH:mm:ss");
                var time = now.ToLongTimeString();

                UpdateSummary(value, date, time);
                UpdateTable(value, date);

                Program.Line("Updated SpreadSheet");
            }
            catch (Exception e)
            {
                Program.Colored(() => Line($"Error updating: {e.Message}"), ConsoleColor.Red);
            }
        }

        public void UpdateSummary(float value, string date, string time)
        {
            _googleSheetsService.SetValue(Program.SpentRange, GetTotalSpent());
            _googleSheetsService.SetValue(Program.UpdateDateRange, date);
            _googleSheetsService.SetValue(Program.UpdateTimeRange, time);
            _googleSheetsService.SetValue(Program.TotalValueRange, value);
        }

        public float GetTotalSpent()
            => _coinspotService.GetAllDeposits().GetTotalDeposited();

        public void UpdateTable(float value, string time)
        {
            var list = new List<object>
            {
                time,
                GetTotalSpent(),
                value,
            };

            var appended = _googleSheetsService.AppendList(Program.ValueTable, list);
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
            _googleSheetsService.AppendList(Program.GainsTable, list);
        }

        public void ShowBalances(string[] args)
        {
            var balances = _coinspotService.GetMyBalances();
            Program.Colored(() => Console.Write(balances), ConsoleColor.Blue);
            Program.Colored(() =>
            {
                Console.Write($"TOTAL: ");
                Program.Line($"{balances.GetTotal():C}");
            }, ConsoleColor.Cyan);
        }
        
        public void ShowStatus(string[] args)
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
            Program.Colored(() => Line($"  Diff$ = {value - _lastDollar:C}"), diffColor);
            Program.Colored(() => Line($"  Diff% = %{gainPercent - _lastGainPercent:0.###}"), diffColor);

            _lastDollar = value;
            _lastGainPercent = gainPercent;
        }

        public static void Line(object text)
            => Console.WriteLine(text);

    }
}
