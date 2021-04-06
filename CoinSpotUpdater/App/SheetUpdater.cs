using System;
using System.Configuration;
using System.Collections.Generic;

using Newtonsoft.Json;
using CoinSpotApi.Dto;

namespace CryptoHelper.App
{
    class SheetUpdater
    {
        // You will want to change these in `App.Config` to match your local setup in App.config.
        private static string SpentRange;
        private static string UpdateDateRange;
        private static string TotalValueRange;
        private static string UpdateTimeRange;
        private static string ValueTable;
        private static string GainsTable;
        private static string BalancesTable;

        private CoinSpotApi.CoinspotService _coinspotService;
        private GoogleSheetsApi.GoogleSheetsService _googleSheetsService;

        public SheetUpdater(CoinSpotApi.CoinspotService coinspotService)
        {
            _coinspotService = coinspotService;
            _googleSheetsService = new GoogleSheetsApi.GoogleSheetsService();

            GetSettings();
        }

        private void GetSettings()
        {
            SpentRange = GetAppSetting("SpentRange");
            UpdateDateRange = GetAppSetting("UpdateDateRange");
            TotalValueRange = GetAppSetting("TotalValueRange");
            UpdateTimeRange = GetAppSetting("UpdateTimeRange");
            ValueTable = GetAppSetting("ValueTable");
            GainsTable = GetAppSetting("GainsTable");
            BalancesTable = GetAppSetting("BalancesTable");
        }

        internal void Browse()
        {
            _googleSheetsService.Browse();
        }

        public static string GetAppSetting(string key)
            => ConfigurationManager.AppSettings.Get(key);

        public void UpdateGoogleSpreadSheet(params string[] args)
        {
            try
            {
                var value = _coinspotService.GetPortfolioValue();
                var now = DateTime.Now;
                var date = now.ToString("dd MMM yy HH:mm:ss");
                var time = now.ToLongTimeString();

                //UpdateSummary(value, date, time);
                //UpdatePricesTable(value, date);
                UpdateBalancesTable();

                Program.Line("Updated SpreadSheet");
            }
            catch (Exception e)
            {
                Program.Colored(() => Program.Line($"Error updating: {e.Message}"), ConsoleColor.Red);
            }
        }

        class RecordedHolding
        {
            public string Coin;
            public float Holding;
            public float BuyInPrice;
            public float CurrentPrice;

            public RecordedHolding(IList<object> list)
            {
                Coin = (string)list[0];
                Holding = float.Parse((string)list[1]);
                BuyInPrice = float.Parse((string)list[2]);
                CurrentPrice = float.Parse((string)list[3]);
            }

            public override string ToString()
            {
                return $"{Coin} {Holding} {BuyInPrice} {CurrentPrice}";
            }
        }

        class RecordedHoldings
        {
            public Dictionary<string, RecordedHolding> Holdings = new Dictionary<string, RecordedHolding>();

            public void Add(RecordedHolding holding)
            {
                var coin = holding.Coin;
                if (!Holdings.TryGetValue(coin, out var value))
                {
                    Holdings.Add(coin, holding);
                }
                Holdings[coin] = holding;
            }
        }

        private RecordedHoldings GetSpreadsheetBalances()
        {
            var inSpreadSheet = _googleSheetsService.GetRange(BalancesTable);
            var recorded = new RecordedHoldings();
            foreach (var item in inSpreadSheet)
            {
                var coin = (string)item[0];
                recorded.Add(new RecordedHolding(item));
            }
            return recorded;
        }

        class Differences
        {
            public List<string> New = new List<string>();       // doesn't exist in spreadsheet
            public List<string> Update = new List<string>();    // exists in spreadsheet and balances
            public List<string> Delete = new List<string>();    // exists in spreadsheet, not in balances
        }

        private void UpdateBalancesTable()
        {
            var balances = _coinspotService.GetMyBalances();
            var inSpreadSheet = GetSpreadsheetBalances();
            var prices = _coinspotService.GetAllPrices();

            var diff = GetDifferences(balances, inSpreadSheet);
        }

        private static Differences GetDifferences(CoinSpotBalances balances, RecordedHoldings recorded)
        {
            var diff = new Differences();
            AddRemoved(balances, recorded, diff);
            DiffExisting(balances, recorded, diff);
            return diff;
        }

        private static void AddRemoved(CoinSpotBalances balances, RecordedHoldings recorded, Differences diff)
        {
            foreach (var holding in recorded.Holdings)
            {
                var coin = holding.Key;
                if (!balances.HasCoin(coin))
                {
                    diff.Delete.Add(coin);
                }
            }
        }

        private static void DiffExisting(CoinSpotBalances balances, RecordedHoldings recorded, Differences diff)
        {
            foreach (var balance in balances.balances)
            {
                foreach (var holding in balance)
                {
                    var coin = holding.Key;
                    if (recorded.Holdings.ContainsKey(coin))
                    {
                        diff.Update.Add(coin);
                    }
                    else if (!balances.HasCoin(coin))
                    {
                        diff.Delete.Add(coin);
                    }
                    else
                    {
                        diff.New.Add(coin);
                    }
                }
            }
        }

        public void UpdateSummary(float value, string date, string time)
        {
            _googleSheetsService.SetValue(SpentRange, GetTotalSpent());
            _googleSheetsService.SetValue(UpdateDateRange, date);
            _googleSheetsService.SetValue(UpdateTimeRange, time);
            _googleSheetsService.SetValue(TotalValueRange, value);
        }

        public void UpdatePricesTable(float value, string time)
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

        private float GetTotalSpent()
            => _coinspotService.GetAllDeposits().GetTotalDeposited();

        private void AppendGainsTable(string tableRange)
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

        public void Append(string v, List<IList<object>> items)
            => _googleSheetsService.Append(v, items);
    }
}
