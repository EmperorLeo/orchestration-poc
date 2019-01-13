using System;

namespace LeosAmazingAsynchrony
{
    public class OrchestrationStartResult
    {
        public Guid Id { get; set; }
        public string StatusQueryGetUri { get; set; }
        public string SendEventPostUri { get; set; }
        public string TerminatePostUri { get; set; }
        public string RewindPostUri { get; set; }
    }
}