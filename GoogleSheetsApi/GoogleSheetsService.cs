using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;

namespace GoogleSheetsApi
{
    // see https://developers.google.com/sheets/api/quickstart/dotnet
    public class GoogleSheetsService
    {
        // If modifying these scopes, delete your previously saved credentials
        private readonly string[] Scopes = { SheetsService.Scope.Spreadsheets, SheetsService.Scope.Drive };
        private readonly string ApplicationName = "CoinSpot Sheets Updater";
        private readonly string _spreadSheetId;
        private SheetsService _sheetsService;

        public GoogleSheetsService()
        {
            ConnectToSpreadSheetService();
            _spreadSheetId = ConfigurationManager.AppSettings.Get("spreadSheetId");
        }

        private void ConnectToSpreadSheetService()
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            _sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public void Browse()
        {
            Process.Start($"https://docs.google.com/spreadsheets/d/{_spreadSheetId}");
        }

        public bool SetValue(string range, object value)
        {
            var rect = new List<IList<object>>();
            var columns = new List<object> { value };
            rect.Add(columns);
            return SetRange(range, rect);
        }

        public void BatchWrite(IList<IList<object>> ranges)
        {
            //BatchUpdateValuesRequest body = new BatchUpdateValuesRequest();
            //body.ValueInputOption = SpreadsheetsResource.ValuesResource.BatchUpdateRequest

            //_sheetsService.Spreadsheets.Values.BatchUpdate(body, _spreadSheetId);
            throw new NotImplementedException();
        }

        public bool SetRange(string range, IList<IList<object>> values)
        {
            var body = new ValueRange() { Values = values };
            var request = _sheetsService.Spreadsheets.Values.Update(body, _spreadSheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            var response = request.Execute();
            return response.UpdatedRange == range;
        }

        public IList<IList<object>> GetRange(string range)
        {
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadSheetId, range);
            var response = request.Execute();
            return response.Values;
        }

        public AppendValuesResponse Append(string range, IList<IList<object>> values)
        {
            var body = new ValueRange() { Values = values };
            var request = _sheetsService.Spreadsheets.Values.Append(body, _spreadSheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            return request.Execute();
        }

        public AppendValuesResponse AppendList(string range, IList<object> values)
        {
            return Append(range, new List<IList<object>> { values });
        }
    }
}
