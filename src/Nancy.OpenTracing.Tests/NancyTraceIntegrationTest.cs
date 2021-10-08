using OpenTracing.Mock;
using Xunit;

namespace Nancy.OpenTracing.Tests
{
    
    
    public class NancyTraceIntegrationTest
    {
        [Fact]
        public void End2EndTest()
        {
            // arrange
            var mockTracer = new MockTracer();
            var bootstrapper = new OpenTracingBootstrapper(mockTracer);
            //var browser = new Browser
        }
    }
}
