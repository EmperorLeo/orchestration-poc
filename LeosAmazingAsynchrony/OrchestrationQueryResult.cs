using System;
using System.Collections.Generic;

namespace LeosAmazingAsynchrony
{
    public class OrchestrationQueryResult
    {
        public Guid InstanceId { get; set; }
        public string RuntimeStatus { get; set; }
        public string Input { get; set; }
        public string CustomStatus { get; set; }
        public string Output { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastUpdatedTime { get; set; }
    }
}