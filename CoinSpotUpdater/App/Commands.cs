using System;
using System.Collections.Generic;

namespace CryptoHelper.App
{
    using CoinSpotApi;

    class Commands
    {
        internal CoinspotService _coinspotService;
        internal SheetUpdater _sheetUpdater;

        private float _lastDollar;
        private float _lastGainPercent;

        public Commands()
        {
            _coinspotService = new CoinspotService();
            _sheetUpdater = new SheetUpdater(_coinspotService);
        }

        public void Buy(params string[] args)
            => _coinspotService.Buy(args[0], float.Parse(args[1]));

        public void ShowSellOrders(params string[] args)
            => Line(GetAllTransactions().SellOrdersToString());

        public void ShowBuyOrders(params string[] args)
            => Line(GetAllTransactions().BuyOrdersToString());

        public void ShowTransactions(params string[] args)
            => Line(GetAllTransactions());

        public void ShowAllDeposits(params string[] args)
            => Line($"Total deposited: {GetTotalSpent()}");

        public void ShowAllPrices(params string[] args)
            => Line(_coinspotService.GetAllPrices());

        public void BrowseSheet(params string[] args)
            => _sheetUpdater.Browse();

        public CoinSpotApi.Dto.CoinSpotTransactions GetAllTransactions()
            => _coinspotService.GetAllTransactions();

        public float GetTotalSpent()
            => _coinspotService.GetAllDeposits().GetTotalDeposited();

        public void UpdateGoogleSpreadSheet(params string[] args)
            => _sheetUpdater.UpdateGoogleSpreadSheet(args);

        public void Sell(params string[] args)
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

        public void ShowGainPercent(params string[] args)
        {
            var spent = _coinspotService.GetAllDeposits().GetTotalDeposited();
            var value = _coinspotService.GetPortfolioValue();
            var gain = value - spent;
            var gainPercent = ((value / spent) - 1.0f) * 100.0f;
            if (_lastDollar == 0)
            {
                _lastGainPercent = gainPercent;
            }
            var color = gainPercent < 0 ? ConsoleColor.Red : ConsoleColor.Green;
            Colored(() => Line($"Gain %{gainPercent:0.##}"), color);
            var diff = (gainPercent - _lastGainPercent);
            Colored(() => Line($"Diff %{diff:0.###}"), color);
            _lastGainPercent = gainPercent;
        }

        public void WriteDeposits(params string[] args)
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
            _sheetUpdater.Append("Spent!A1", items);
        }

        public void ShowAll(params string[] args)
        {
            ShowStatus(args);
            ShowBalances(args);
        }

        public void ShowBalances(params string[] args)
        {
            var balances = _coinspotService.GetMyBalances();
            Colored(() => Console.Write(balances), ConsoleColor.Blue);
            Colored(() =>
            {
                Console.Write($"TOTAL: ");
                Line($"{balances.GetTotal():C}");
            }, ConsoleColor.Cyan);
        }
        
        public void ShowStatus(params string[] args)
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

            PrintStatus(spent, value, gain, gainPercent);

            _lastDollar = value;
            _lastGainPercent = gainPercent;
        }

        private void PrintStatus(float spent, float value, float gain, float gainPercent)
        {
            Line($"Spent   = {spent:C}");
            Line($"Value   = {value:C}");
            Line($"Gain$   = {gain:C}");
            Line($"Gain%   = %{gainPercent:0.##}");

            var diffDollar = value - _lastDollar;
            var diffGain = gainPercent - _lastGainPercent;
            var diffColor = CalcDiffColor(diffGain);

            Colored(() => Line($"  Diff$ = {value - _lastDollar:C}"), diffColor);
            Colored(() => Line($"  Diff% = %{gainPercent - _lastGainPercent:0.###}"), diffColor);
        }

        private static ConsoleColor CalcDiffColor(float diffGain)
        {
            ConsoleColor diffColor;
            var neg = diffGain < 0;

            if (Math.Abs(diffGain) < 0.05f)
                diffColor = ConsoleColor.DarkGray;
            else if (neg && diffGain > -0.10f)
                diffColor = ConsoleColor.DarkRed;
            else if (!neg && diffGain < 0.25f)
                diffColor = ConsoleColor.DarkGreen;
            else
                diffColor = diffGain < 0 ? ConsoleColor.Red : ConsoleColor.Green;

            return diffColor;
        }

        private void Colored(Action action, ConsoleColor color)
            => Program.Colored(action, color);

        public static void Line(object text)
            => Console.WriteLine(text);
    }
}
