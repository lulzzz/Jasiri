﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing;
using OpenTracing.Propagation;

namespace Jasiri
{
    public class Tracer : ITracer
    {
        public Func<DateTimeOffset> Clock { get; }

        public Func<ulong> NewId { get; }

        public Endpoint HostEndpoint { get; }

        public ISampler Sampler { get; }
        public IReporter Reporter { get; }

        public IPropagationRegistry PropagationRegistry { get; }
        public ISpanBuilder BuildSpan(string operationName)
            => new SpanBuilder(this, operationName);

        public Tracer(TraceOptions options)
        {
            Clock = options.Clock;
            NewId = options.NewId ?? RandomLongGenerator.NewId;
            HostEndpoint = options.Endpoint ?? Util.GetHostEndpoint();
            Sampler = options.Sampler ?? new ConstSampler(false);
            Reporter = options.Reporter ?? NullReporter.Instance;
            PropagationRegistry = options.PropagationRegistry;
        }

        public ISpanContext Extract<TCarrier>(Format<TCarrier> format, TCarrier carrier)
        {
            ISpanContext context = null;
            if (PropagationRegistry.TryGet(format, out var propagator))
                context = propagator.Extract(carrier);
            return context;
        }

        public void Inject<TCarrier>(ISpanContext spanContext, Format<TCarrier> format, TCarrier carrier)
        {
            if (PropagationRegistry.TryGet(format, out var propagator))
                propagator.Inject(spanContext, carrier);
            else
                throw new NotImplementedException($"Propagator for format {format.Name} not found");
        }

        internal void Report(Span span)
        {
            if (!span.TypedContext.Sampled)
                return; //no need to report
            Reporter.Report(span);
        }
    }
}
