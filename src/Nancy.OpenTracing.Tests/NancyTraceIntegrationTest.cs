using System.Linq;
using FluentAssertions;
using Nancy.Testing;
using OpenTracing;
using OpenTracing.Mock;
using Xunit;

namespace Nancy.OpenTracing.Tests
{
    public class OTModule : NancyModule
    {
        private readonly ITracer _tracer;

        public OTModule(ITracer tracer) : base("/test")
        {
            _tracer = tracer;

            Get["/"] = p =>
            {
                using (var t = _tracer.BuildSpan("MyOp").StartActive())
                {
                    return "hello";
                }
            };
        }
    }
    
    public class NancyTraceIntegrationTest
    {
        [Fact]
        public void End2EndTest()
        {
            // arrange
            var mockTracer = new MockTracer();
            var bootstrapper = new OpenTracingBootstrapper(mockTracer);
            var browser = new Browser(bootstrapper);
            
            // act
            var resp = browser.Get("/test");
            
            // assert
            Assert.Equal("hello", resp.Body.AsString());
            var finishedSpans = mockTracer.FinishedSpans();
            finishedSpans.Count.Should().Be(2); // two spans expected
            
            // both spans should be part of the same trace
            finishedSpans.Select(x => x.Context.TraceId).Distinct().Count().Should().Be(1);
        }
    }
}
