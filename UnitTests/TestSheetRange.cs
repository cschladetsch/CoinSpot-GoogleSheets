using GoogleSheetsApi;
using CoinSpotApi;

using NUnit.Framework;

namespace UnitTests
{
    public class TestSheetRange
    {
        [Test]
        public void TestCell()
        {
            var cell = new Cell("A1");
            Assert.AreEqual("A", cell.Row);
            Assert.AreEqual(1, cell.Column);
        }

        [Test]
        public void TestHalf()
        {
            var range = new Range("Sheet!A1");
            Assert.AreEqual("Sheet", range.SpreadSheet);
            Assert.AreEqual("A", range.Start.Row);
            Assert.AreEqual(1, range.Start.Column);
        }

        [Test]
        public void TestFull()
        {
            var range = new Range("Sheet!A1:B2");
            Assert.AreEqual("Sheet", range.SpreadSheet);
            Assert.AreEqual("A", range.Start.Row);
            Assert.AreEqual(1, range.Start.Column);
            Assert.AreEqual("B", range.End.Row);
            Assert.AreEqual(2, range.End.Column);
        }
    }
}