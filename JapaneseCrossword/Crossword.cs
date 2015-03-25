using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JapaneseCrossword
{
    public enum CellStatus
    {
        Undefined,
        Blank,
        Colored,
    }

    public class Crossword
    {
        public CellStatus[][] Map { get; private set; }
        public List<List<int>> Rows { get; private set; }
        public List<List<int>> Columns { get; private set; } 

        public Crossword(List<List<int>> rows, List<List<int>> columns)
        {
            Rows = rows;
            Columns = columns;
            Map = new CellStatus[rows.Count][];
            for (int i = 0; i < rows.Count; i++)
                Map[i] = new CellStatus[columns.Count];
        }

        /// <exception cref="T:BadInputFileException">BadInputFileException</exception>
        /// <exception cref="T:IncorrectCrosswordException">IncorrectCrosswordException</exception>
        public static Crossword FromFile(string filename)
        {
            try
            {
                var lines = File.ReadAllLines(filename);
                if (!lines[0].StartsWith("rows"))
                    throw new IncorrectCrosswordException("Could not find rows in file.");
                var rowsCount = int.Parse(lines[0].Split(':')[1]);
                var rows = new List<List<int>>();
                for (int i = 1; i <= rowsCount; i++)
                    rows.Add(lines[i].Split().Select(int.Parse).ToList());
                
                if (!lines[rowsCount + 1].StartsWith("columns"))
                    throw new IncorrectCrosswordException("Could not find columns in file.");
                var columnsCount = int.Parse(lines[rowsCount + 1].Split(':')[1]);
                var columns = new List<List<int>>();
                for (int i = 0; i < columnsCount; i++)
                    columns.Add(lines[rowsCount + i + 2].Split().Select(int.Parse).ToList());

                if (rows.Count == 0 || columns.Count == 0)
                {
                    throw new IncorrectCrosswordException("Crossword rows or columns count is 0.");
                }
                return new Crossword(rows, columns);
            }
            catch (Exception e)
            {
                throw new BadInputFileException(e.Message);
            }
        }

        public CellStatus[] GetLine(bool isRow, int index)
        {
            var res = new CellStatus[isRow ? Columns.Count : Rows.Count];
            for (int i = 0; i < res.Length; i++)
                res[i] = Map[isRow ? index : i][isRow ? i : index];
            return res;
        }

        public void SetLine(bool isRow, int index, CellStatus[] statuses)
        {
            for (int i = 0; i < statuses.Length; i++)
                Map[isRow ? index : i][isRow ? i : index] = statuses[i];
        }
    }
}
