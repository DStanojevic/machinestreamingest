using MachineDataApi.Instrumentation;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace MachineDataApi.Db
{
    public class DbConnectionInterceptor : IDbConnectionInterceptor
    {
        public void ConnectionClosed(DbConnection connection, ConnectionEndEventData eventData)
        {
            InstrumentationConstants.DbConnectionCounter.Add(-1, new KeyValuePair<string, object?>("database", "machinedatadb"));
        }

        public Task ConnectionClosedAsync(DbConnection connection, ConnectionEndEventData eventData)
        {
            InstrumentationConstants.DbConnectionCounter.Add(-1, new KeyValuePair<string, object?>("database", "machinedatadb"));
            return Task.CompletedTask;
        }

        public InterceptionResult ConnectionClosing(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            return new InterceptionResult();
        }

        public ValueTask<InterceptionResult> ConnectionClosingAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            return ValueTask.FromResult(new InterceptionResult());
        }

        public void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData)
        {
            //nothing here
        }

        public Task ConnectionFailedAsync(DbConnection connection, ConnectionErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            InstrumentationConstants.DbConnectionCounter.Add(1, new KeyValuePair<string, object?>("database", "machinedatadb"));
        }

        public Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            InstrumentationConstants.DbConnectionCounter.Add(1, new KeyValuePair<string, object?>("database", "machinedatadb"));
            return Task.CompletedTask;
        }

        public InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            return new InterceptionResult();
        }

        public ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new InterceptionResult());
        }
    }
}
