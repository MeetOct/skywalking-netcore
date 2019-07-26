﻿/*
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

using System;
using Microsoft.Extensions.DependencyInjection;
using SkyWalking.Utilities.DependencyInjection;

namespace SkyWalking.Diagnostics.EntityFrameworkCore
{
    public static class SkyWalkingBuilderExtensions
    {
        public static SkyWalkingExtensions AddEntityFrameworkCore(this SkyWalkingExtensions extensions, Action<DatabaseProviderBuilder> optionAction)
        {
            if (extensions == null)
            {
                throw new ArgumentNullException(nameof(extensions));
            }

            extensions.Services.AddSingleton<ITracingDiagnosticProcessor, EntityFrameworkCoreTracingDiagnosticProcessor>();
            extensions.Services.AddSingleton<IConcurrentEfCoreSpanFactory, ConcurrentEfCoreSpanFactory>();

            if (optionAction != null)
            {
                var databaseProviderBuilder = new DatabaseProviderBuilder(extensions.Services);
                optionAction(databaseProviderBuilder);
            }

            return extensions;
        }
    }
}