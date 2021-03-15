using System;
using System.Configuration;
using System.Collections.Generic;

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
                UpdateTable(value, date);

                Program.Line("Updated SpreadSheet");
            }
            catch (Exception e)
            {
                Program.Colored(() => Program.Line($"Error updating: {e.Message}"), ConsoleColor.Red);
            }
        }

        public void UpdateSummary(float value, string date, string time)
        {
            _googleSheetsService.SetValue(SpentRange, GetTotalSpent());
            _googleSheetsService.SetValue(UpdateDateRange, date);
            _googleSheetsService.SetValue(UpdateTimeRange, time);
            _googleSheetsService.SetValue(TotalValueRange, value);
        }

        public void UpdateTable(float value, string time)
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
