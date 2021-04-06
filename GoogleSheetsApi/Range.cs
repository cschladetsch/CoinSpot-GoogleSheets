namespace GoogleSheetsApi
{
    public class Range
    {
        public Cell Start;
        public Cell End;
        public string SpreadSheet;

        public Range() { }
        public Range(Cell cell) { Start = cell;}
        public Range(Cell start, Cell end) { Start = start; End = end; }
        public Range(string spreadSheet, Cell start, Cell end) { SpreadSheet = spreadSheet; Start = start; End = end; }

        public Range(string text)
        {
            var bang = text.IndexOf('!');
            var offset = 0;
            if (bang != -1)
            {
                SpreadSheet = text.Substring(0, bang);
                offset = bang + 1;
            }
            var colon = text.IndexOf(':', offset);
            if (colon != -1)
            {
                Start = new Cell(text.Substring(offset, colon - offset));
                offset = colon + 1;
                End  = new Cell(text.Substring(offset));
                return;
            }

            Start = new Cell(text.Substring(offset));
        }
    }
}

