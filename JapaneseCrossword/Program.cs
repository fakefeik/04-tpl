using System.IO;

namespace JapaneseCrossword
{
    class Program
    {
        static void Main(string[] args)
        {
            ICrosswordSolver solver = new MultithreadedCrosswordSolver();
            var inputFilePath = @"TestFiles\car.txt";
            var outputFilePath = Path.GetRandomFileName();
            var correctOutputFilePath = @"TestFiles\Car.solved.txt";
            var solutionStatus = solver.Solve(inputFilePath, outputFilePath);
        }
    }
}
