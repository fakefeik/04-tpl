using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JapaneseCrossword
{
    public class MultithreadedCrosswordSolver : BaseCrosswordSolver
    {
        private void Execute(bool isRow)
        {
            var queueToProcess = isRow ? rowsToProcess : columnsToProcess;
            var queueToUpdate = isRow ? columnsToProcess : rowsToProcess;
            var tasks = new List<Task<IEnumerable<int>>>();
            while (queueToProcess.Count != 0)
            {
                
                int index = queueToProcess.First();
                queueToProcess.Remove(index);

                var processor = new LineProcessor();
                Func<IEnumerable<int>> processorFunc =
                    () => isRow ? processor.ProcessRow(crossword, index) : processor.ProcessColumn(crossword, index);

                tasks.Add(Task.Run(processorFunc));
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
                foreach (var task in tasks)
                {
                    queueToUpdate.UnionWith(task.Result);
                }
            }
            catch (AggregateException e)
            {
                throw new IncorrectCrosswordException(e.InnerException.Message);
            }
        }
        
        protected override void ProcessRows()
        {
            Execute(true);
        }

        protected override void ProcessColumns()
        {
            Execute(false);
        }
    }
}