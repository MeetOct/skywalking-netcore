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

using SkyWalking.Components;
using SkyWalking.Context;
using SkyWalking.Context.Tag;
using SkyWalking.Context.Trace;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace SkyWalking.Diagnostics.SqlClient
{
    public class SqlClientTracingDiagnosticProcessor : ITracingDiagnosticProcessor
    {
        private const string TRACE_ORM = "TRACE_ORM";
        public string ListenerName { get; } = SqlClientDiagnosticStrings.DiagnosticListenerName;

        private static string ResolveOperationName(SqlCommand sqlCommand)
        {
            var commandType = sqlCommand.CommandText?.Split(' ');
            return $"{SqlClientDiagnosticStrings.SqlClientPrefix}{commandType?.FirstOrDefault()}";
        }

        [DiagnosticName(SqlClientDiagnosticStrings.SqlBeforeExecuteCommand)]
        public void BeforeExecuteCommand([Property(Name = "Command")] SqlCommand sqlCommand)
        {
            if (ConcurrentContextManager.ContextProperties.ContainsKey(TRACE_ORM))
            {
                return;
            }
            var peer = sqlCommand.Connection.DataSource;
            var span = ConcurrentContextManager.CreateExitSpan(ResolveOperationName(sqlCommand), peer,Activity.Current.Id, Activity.Current.ParentId);
            span.SetLayer(SpanLayer.DB);
            span.SetComponent(ComponentsDefine.SqlClient);
            Tags.DbType.Set(span, "Sql");
            Tags.DbInstance.Set(span, sqlCommand.Connection.Database);
            Tags.DbStatement.Set(span, sqlCommand.CommandText);
            //todo Tags.DbBindVariables
        }

        [DiagnosticName(SqlClientDiagnosticStrings.SqlAfterExecuteCommand)]
        public void AfterExecuteCommand()
        {
            if (ConcurrentContextManager.ContextProperties.ContainsKey(TRACE_ORM))
            {
                return;
            }
            ConcurrentContextManager.StopSpan(Activity.Current.Id);
        }

        [DiagnosticName(SqlClientDiagnosticStrings.SqlErrorExecuteCommand)]
        public void ErrorExecuteCommand([Property(Name = "Exception")] Exception ex)
        {
            if (ConcurrentContextManager.ContextProperties.ContainsKey(TRACE_ORM))
            {
                return;
            }
            var span = ConcurrentContextManager.ActiveSpan(Activity.Current.Id);
            span?.ErrorOccurred();
            span?.Log(ex);
            ContextManager.StopSpan(span);
        }
    }
}