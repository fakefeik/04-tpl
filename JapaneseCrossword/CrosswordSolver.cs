using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JapaneseCrossword
{
    public class CrosswordSolver : BaseCrosswordSolver
    {
        private LineProcessor processor = new LineProcessor();

        protected override void ProcessRows()
        {
            while (rowsToProcess.Count != 0)
            {
                var index = rowsToProcess.First();
                rowsToProcess.Remove(index);
                var update = processor.ProcessRow(crossword, index);
                columnsToProcess.UnionWith(update);
            }
        }

        protected override void ProcessColumns()
        {
            while (columnsToProcess.Count != 0)
            {
                var index = columnsToProcess.First();
                columnsToProcess.Remove(index);
                var update = processor.ProcessColumn(crossword, index);
                rowsToProcess.UnionWith(update);
            }
        }
    }
}