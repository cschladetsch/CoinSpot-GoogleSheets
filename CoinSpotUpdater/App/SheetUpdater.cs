using System;
using System.Configuration;
using System.Collections.Generic;

using CoinSpotApi.Dto;

namespace CryptoHelper.App
{
    partial class SheetUpdater
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

                UpdateSummary(value, date, time);
                UpdatePricesTable(value, date);

                Program.Line("Updated SpreadSheet");
            }
            catch (Exception e)
            {
                Program.Colored(() => Program.Line($"Error updating: {e.Message}"), ConsoleColor.Red);
            }
        }

        private RecordedHoldings GetSpreadsheetBalances()
        {
            var inSpreadSheet = _googleSheetsService.GetRange(BalancesTable);
            var recorded = new RecordedHoldings();
            foreach (var item in inSpreadSheet)
            {
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

        public string UpdateBalanceSheet()
        {
            var balances = _coinspotService.GetMyBalances();
            var inSpreadSheet = GetSpreadsheetBalances();
            var prices = _coinspotService.GetAllPrices();

            var diff = GetDifferences(balances, inSpreadSheet);
            WriteChanges(prices, diff);
            return "Balance sheet updated";
        }

        private void WriteChanges(CoinSpotAllPrices prices, Differences diff)
        {
            foreach (var del in diff.Delete)
            {
                //_googleSheetsService.DeleteMatching(BalancesTable, 0, del);
            }
            foreach (var add in diff.New)
            {
                //_googleSheetsService.AppendMatching(BalancesTable, 0, add, )
            }
            foreach (var upd in diff.Update)
            {
                //_googleSheetsService.UpdateMatching(BalancesTable, 0, upd, ...)
            }
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
                CheckBalance(balances, recorded, diff, balance);
            }
        }

        private static void CheckBalance(CoinSpotBalances balances, RecordedHoldings recorded, Differences diff, Dictionary<string, CoinSpotHolding> balance)
        {
            foreach (var holding in balance)
            {
                UpdateBalanceAgainstRecorded(balances, recorded, diff, holding);
            }
        }

        private static void UpdateBalanceAgainstRecorded(CoinSpotBalances balances, RecordedHoldings recorded, Differences diff, KeyValuePair<string, CoinSpotHolding> holding)
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
