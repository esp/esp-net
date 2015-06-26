using System;
using Esp.Net.Pipeline;

namespace Esp.Net
{
    public class Foo
    {
        public Foo()
        {
            var r = new Router<int>(1, null);
            r.ConfigurePipeline();
        } 
    }
}