using System.Collections.Generic;

namespace LeosFunctions
{
    public class FileProcessorResult
    {
        public IEnumerable<string> AllEngineers { get; set; }
        public ICollection<string> CodeMonkeys { get; set; }
        public ICollection<string> MicroserviceSuperstars { get; set; }
        public Dictionary<string, int> Levels { get; set; }
    }
}