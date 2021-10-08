using System;
using System.Collections.Generic;
using Nancy.Bootstrapper;
using Nancy.Extensions;
using Nancy.TinyIoc;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using OpenTracing.Util;

namespace Nancy.OpenTracing
{
    public class OpenTracingBootstrapper : DefaultNancyBootstrapper
    {
        public const string OTContextItem = "TraceContext";
        
        public static readonly Func<NancyContext, string> DefaultOperationNameFormatter = context =>
        {
            return $"HTTP {context.Request.Method} {context.Request.Path}";
        };

        /// <summary>
        /// Create an instance of this <see cref="DefaultNancyBootstrapper"/> using a specific <see cref="ITracer"/>
        /// </summary>
        /// <param name="tracer">The tracer that will be used to correlate all HTTP activity.</param>
        /// <param name="operationNameFormatter">Used to format the names of HTTP methods into <see cref="ISpan"/> operation names.</param>
        public OpenTracingBootstrapper(ITracer tracer, Func<NancyContext, string> operationNameFormatter)
        {
            Tracer = tracer;
            _operationNameFormatter = operationNameFormatter ?? DefaultOperationNameFormatter;
        }
        
        public OpenTracingBootstrapper(ITracer tracer) : this(tracer, DefaultOperationNameFormatter)
        {
        }

        /// <summary>
        /// Uses OpenTracing's default tracer if none is provided.
        /// </summary>
        public OpenTracingBootstrapper() : this(GlobalTracer.Instance)
        {
            
        }

        /// <summary>
        /// The Tracer used by the HTTP application.
        /// </summary>
        public ITracer Tracer { get; }

        private readonly Func<NancyContext, string> _operationNameFormatter;
        
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            pipelines.BeforeRequest += (ctx) =>
            {
                var spanBuilder = Tracer.BuildSpan(_operationNameFormatter(ctx));

                try
                {
                    var parentContext = Tracer.Extract(BuiltinFormats.HttpHeaders,
                        new RequestHeadersExtractAdapter(ctx.Request.Headers));
                    spanBuilder = spanBuilder.AsChildOf(parentContext);
                }
                catch (Exception)
                {
                    // ignore extractor errors
                }

                var scope = spanBuilder.WithTag(Tags.Component, "HttpIn")
                    .WithTag(Tags.SpanKind, Tags.SpanKindServer)
                    .WithTag(Tags.HttpMethod, ctx.Request.Method)
                    .WithTag(Tags.HttpUrl, GetDisplayUrl(ctx.Request.Url))
                    .StartActive();
                
                ctx.Items.Add(OTContextItem, scope);

                return null;
            };

            pipelines.AfterRequest += ctx =>
            {
                // don't attempt to process trace where there is none
                if (!ctx.Items.ContainsKey(OTContextItem)) return;

                using (var scope = (IScope) ctx.Items[OTContextItem])
                {
                    scope.Span.SetTag(Tags.HttpStatus, (int)ctx.Response.StatusCode);
                }

                ctx.Items.Remove(OTContextItem);
            };
            
            pipelines.OnError += (ctx, ex) =>
            {
                // don't attempt to process trace where there is none
                if (!ctx.Items.ContainsKey(OTContextItem)) return null;

                var scope = (IScope) ctx.Items[OTContextItem];
                scope.Span.SetTag(Tags.Error, true);
                scope.Span.Log(new Dictionary<string, object>(3)
                {
                    { LogFields.Event, Tags.Error.Key },
                    { LogFields.ErrorKind, ex.GetType().Name },
                    { LogFields.ErrorObject, ex }
                });

                return null;
            };

            // make the ITracer available for use inside modules
            container.Register<ITracer>(Tracer);
            
            base.ApplicationStartup(container, pipelines);
        }
        
        private static string GetDisplayUrl(Url request)
        {
            return $"{request.Scheme}://{request.HostName}{request.BasePath}{request.Path}{request.Query}";
        }
    }
}
