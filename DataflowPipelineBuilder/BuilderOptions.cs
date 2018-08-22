using System;

namespace DataflowPipelineBuilder
{
    public class BuilderOptions
    {
        public Action<string> InLogger { get; set; }

        public Action<string, TimeSpan> OutLogger { get; set; }
    }
}
