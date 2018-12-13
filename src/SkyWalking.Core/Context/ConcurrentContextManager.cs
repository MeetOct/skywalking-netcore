/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using SkyWalking.Context.Trace;
using System.Collections.Generic;
using System.Threading;

namespace SkyWalking.Context
{
    /// <summary>
    /// Context manager controls the whole context of tracing. Since .NET server application runs as same as Java,
    /// We also provide the CONTEXT propagation based on ThreadLocal mechanism.
    /// Meaning, each segment also related to singe thread.
    /// </summary>
    public class ConcurrentContextManager : ITracingContextListener, IIgnoreTracerContextListener
    {
        static ConcurrentContextManager()
        {
            var manager = new ConcurrentContextManager();
            ConcurrentTraceContext.ListenerManager.Add(manager);
            IgnoredTracerContext.ListenerManager.Add(manager);
        }

        private static readonly AsyncLocal<IConcurrentTraceContext> _context = new AsyncLocal<IConcurrentTraceContext>();

        private static IConcurrentTraceContext GetOrCreateContext(string operationName, bool forceSampling)
        {
            var context = _context.Value;
            if (context == null)
            {
                if (string.IsNullOrEmpty(operationName))
                {
                    // logger.debug("No operation name, ignore this trace.");
                    _context.Value = new ConcurrentIgnoredTraceContext();
                }
                else
                {
                    if (RuntimeEnvironment.Instance.Initialized)
                    {
//                        var suffixIdx = operationName.LastIndexOf('.');
//                        if (suffixIdx > -1 && AgentConfig.IgnoreSuffix.Contains(operationName.Substring(suffixIdx)))
//                        {
//                            _context.Value = new IgnoredTracerContext();
//                        }
//                        else
//                        {
                            var sampler = DefaultSampler.Instance;
                            if (forceSampling || sampler.Sampled())
                            {
                                _context.Value = new ConcurrentTraceContext();
                            }
                            else
                            {
                                _context.Value = new ConcurrentIgnoredTraceContext();
                            }
//                        }
                    }
                    else
                    {
                        _context.Value = new ConcurrentIgnoredTraceContext();
                    }
                }
            }

            return _context.Value;
        }

        private static IConcurrentTraceContext Context => _context.Value;

        public static string GlobalTraceId
        {
            get
            {
                if (_context.Value != null)
                {
                    return _context.Value.GetReadableGlobalTraceId();
                }

                return "N/A";
            }
        }

        public static IContextSnapshot Capture(string activityId) => _context.Value?.Capture(activityId);

        public static IDictionary<string, object> ContextProperties => _context.Value?.Properties;

        public static ISpan CreateEntrySpan(string operationName, IContextCarrier carrier,string activityId)
        {
            var samplingService = DefaultSampler.Instance;
            if (carrier != null && carrier.IsValid)
            {
                samplingService.ForceSampled();
                var context = GetOrCreateContext(operationName, true);
                var span = context.CreateEntrySpan(operationName, activityId);
                context.Extract(carrier, activityId);
                return span;
            }
            else
            {
                var context = GetOrCreateContext(operationName, false);

                return context.CreateEntrySpan(operationName, activityId);
            }
        }

        public static ISpan CreateLocalSpan(string operationName, string activityId)
        {
            var context = GetOrCreateContext(operationName, false);
            return context.CreateLocalSpan(operationName, activityId);
        }

        public static ISpan CreateExitSpan(string operationName, IContextCarrier carrier, string remotePeer, string activityId)
        {
            var context = GetOrCreateContext(operationName, false);
            var span = context.CreateExitSpan(operationName, remotePeer, activityId);
            context.Inject(carrier, activityId);
            return span;
        }

        public static ISpan CreateExitSpan(string operationName, string remotePeer, string activityId)
        {
            var context = GetOrCreateContext(operationName, false);
            var span = context.CreateExitSpan(operationName, remotePeer, activityId);
            return span;
        }

        public static void Inject(IContextCarrier carrier, string activityId)
        {
            Context?.Inject(carrier, activityId);
        }

        public static void Extract(IContextCarrier carrier, string activityId)
        {
            Context?.Extract(carrier, activityId);
        }

        public static void Continued(IContextSnapshot snapshot, string activityId)
        {
            if (snapshot.IsValid && !snapshot.IsFromCurrent)
            {
                Context?.Continued(snapshot, activityId);
            }
        }

        public static void StopSpan(string activityId)
        {
            StopSpan(ActiveSpan(activityId), activityId);
        }

        public static ISpan ActiveSpan(string activityId)
        {
            return Context?.ActiveSpan(activityId);
        }

        public static void StopSpan(ISpan span, string activityId)
        {
            Context?.StopSpan(span, activityId);
        }

        public void AfterFinished(ITraceSegment traceSegment)
        {
            _context.Value = null;
        }

        public void AfterFinish(ITracerContext tracerContext)
        {
            _context.Value = null;
        }

        public void AfterFinish(IConcurrentTraceContext tracerContext)
        {
            _context.Value = null;
        }
    }
}