using System.Text;

namespace GoogleSheetsApi
{
    public class Cell
    { 
        public string Row;
        public int Column;

        public Cell() { }

        public Cell(string text)
        {
            var index = 0;
            while (char.IsLetter(text[index]))
                index++;
            Row = text.Substring(0, index);
            if (index < text.Length)
                Column = int.Parse(text.Substring(index));
        }

        public Cell(string row, int column)
        {
            Row = row;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Row}:{Column}";
        }
    }
}
