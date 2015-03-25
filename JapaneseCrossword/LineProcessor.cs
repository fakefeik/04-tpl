using System.Collections.Generic;

namespace JapaneseCrossword
{
    public class LineProcessor
    {
        private CellStatus[] line;
        private List<int> lineInfo;

        private bool[] canBeBlank;
        private bool[] canBeColored;

        private bool TryBlock(int block, int start)
        {
            for (int i = start; i < start + lineInfo[block]; i++)
                if (line[i] == CellStatus.Blank) return false;
            if (block < lineInfo.Count - 1)
            {
                var res = false;
                for (int startNext = start + lineInfo[block] + 1;
                    startNext < line.Length - lineInfo[block + 1] + 1;
                    startNext++)
                {
                    if (line[startNext - 1] == CellStatus.Colored) break;
                    if (!TryBlock(block + 1, startNext)) continue;
                    res = true;
                    for (int i = start; i < start + lineInfo[block]; i++)
                        canBeColored[i] = true;
                    for (int i = start + lineInfo[block]; i < startNext; i++)
                        canBeBlank[i] = true;
                }
                return res;
            }
            for (int i = start + lineInfo[block]; i < line.Length; i++)
                if (line[i] == CellStatus.Colored) return false;
            for (int i = start; i < start + lineInfo[block]; i++)
                canBeColored[i] = true;
            for (int i = start + lineInfo[block]; i < line.Length; i++)
                canBeBlank[i] = true;
            return true;
        }

        /// <exception cref="T:IncorrectCrosswordException">IncorrectCrosswordException</exception>
        private IEnumerable<int> ProcessLine(Crossword crossword, bool isRow, int index)
        {
            lock (crossword)
            {
                line = crossword.GetLine(isRow, index);
            }

            lineInfo = isRow ? crossword.Rows[index] : crossword.Columns[index];
            canBeBlank = new bool[line.Length];
            canBeColored = new bool[line.Length];
            for (int i = 0; i < line.Length - lineInfo[0] + 1; i++)
            {
                if (i > 0 && line[i - 1] == CellStatus.Colored) break;
                if (!TryBlock(0, i)) continue;
                for (int j = 0; j < i; j++)
                    canBeBlank[j] = true;
            }

            var newLine = new CellStatus[line.Length];
            for (int i = 0; i < line.Length; i++)
            {
                if (!canBeBlank[i] && !canBeColored[i])
                    throw new IncorrectCrosswordException((isRow ? "Row" : "Column") + " with index " + index + ": " + i + "th cell can't be blank and colored at the same time.");
                if (canBeBlank[i] != canBeColored[i])
                    newLine[i] = canBeBlank[i] ? CellStatus.Blank : CellStatus.Colored;
                if (line[i] != CellStatus.Undefined && newLine[i] != CellStatus.Undefined && newLine[i] != line[i])
                    throw new IncorrectCrosswordException((isRow ? "Row" : "Column") + " with index " + index + ": " + "Updated " + i + "th cell that was already defined.");
            }
            var res = new List<int>();
            for (int i = 0; i < line.Length; i++)
                if (newLine[i] != line[i])
                    res.Add(i);

            lock (crossword)
            {
                crossword.SetLine(isRow, index, newLine);
            }

            return res;
        }

        public IEnumerable<int> ProcessRow(Crossword crossword, int index)
        {
            return ProcessLine(crossword, true, index);
        }

        public IEnumerable<int> ProcessColumn(Crossword crossword, int index)
        {
            return ProcessLine(crossword, false, index);
        } 
    }
}
