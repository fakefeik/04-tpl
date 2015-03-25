using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseCrossword
{
    // I will surely need this
    public class IncorrectCrosswordException : Exception
    {
        public IncorrectCrosswordException(string message) : base(message) { }
        public IncorrectCrosswordException() { }
    }
    // ...and this
    public class BadInputFileException : Exception
    {
        public BadInputFileException(string message) : base(message) { }
        public BadInputFileException() { }
    }
}
