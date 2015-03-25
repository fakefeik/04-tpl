using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace JapaneseCrossword
{
    public abstract class BaseCrosswordSolver : ICrosswordSolver
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        protected Crossword crossword;
        protected HashSet<int> rowsToProcess;
        protected HashSet<int> columnsToProcess;

        public SolutionStatus Solve(string inputFilePath, string outputFilePath)
        {
            // the greatest solver
            try
            {
                crossword = Crossword.FromFile(inputFilePath);
            }
            catch (BadInputFileException e)
            {
                logger.Error(e.Message);
                return SolutionStatus.BadInputFilePath;
            }
            catch (IncorrectCrosswordException e)
            {
                logger.Error(e.Message);
                return SolutionStatus.IncorrectCrossword;
            }

            rowsToProcess = new HashSet<int>(Enumerable.Range(0, crossword.Rows.Count));
            columnsToProcess = new HashSet<int>(Enumerable.Range(0, crossword.Columns.Count));

            try
            {
                while (rowsToProcess.Count != 0 || columnsToProcess.Count != 0)
                {
                    ProcessRows();
                    ProcessColumns();
                }
            }
            catch (IncorrectCrosswordException e)
            {
                logger.Error(e.Message);
                return SolutionStatus.IncorrectCrossword;
            }

            var lines = new List<string>();

            for (int i = 0; i < crossword.Map.Length; i++)
            {
                var s = new StringBuilder(crossword.Map.Length);
                for (int j = 0; j < crossword.Map[0].Length; j++)
                    s.Append(crossword.Map[i][j] == CellStatus.Blank
                        ? "."
                        : crossword.Map[i][j] == CellStatus.Colored ? "*" : "?");
                lines.Add(s.ToString());
            }

            try
            {
                File.WriteAllLines(outputFilePath, lines);
            }
            catch (Exception e)
            {
                logger.Error("Could not write to file: " + outputFilePath + "\n\t" + e.Message);
                return SolutionStatus.BadOutputFilePath;
            }
            return crossword.Map.SelectMany(x => x).Any(x => x == CellStatus.Undefined)
                ? SolutionStatus.PartiallySolved
                : SolutionStatus.Solved;
        }

        protected abstract void ProcessRows();
        protected abstract void ProcessColumns();
    }
}
