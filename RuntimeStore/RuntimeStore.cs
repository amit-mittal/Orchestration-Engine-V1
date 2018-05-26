using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using TenantUpdateActor.Interfaces;

namespace RuntimeStore
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class RuntimeStore : StatefulService, IRuntimeStore
    {
        public RuntimeStore(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            //return new[] { new ServiceInstanceListener(context => this.CreateServiceRemotingListener(context)) };
            return new[]
            {
                new ServiceReplicaListener(context => this.CreateServiceRemotingListener(context))
            };
            //return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                /*using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }*/

                // TODO: Do some housekeeping like remove Completed from terminal state queue

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public Task<string> HelloWorldAsync()
        {
            return Task.FromResult("Hello World!");
        }

        public async Task<bool> AddTenant(long value)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await myDictionary.TryGetValueAsync(tx, value.ToString());

                ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                    result.HasValue ? result.Value.ToString() : "Value does not exist.");

                await myDictionary.AddOrUpdateAsync(tx, value.ToString(), 0, (k, v) => value);

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return true;
        }

        public async Task<bool> RemoveTenant(long value)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                    result.HasValue ? result.Value.ToString() : "Value does not exist.");

                await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (k, v) => 0);

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return true;
        }

        public async Task<bool> UpdateTenant()
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                    result.HasValue ? result.Value.ToString() : "Value does not exist.");

                await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (k, v) => ++v);

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return true;
        }

        public async Task<long> GetTenant(long id)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            long value = -1;

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await myDictionary.TryGetValueAsync(tx, id.ToString());

                ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                    result.HasValue ? result.Value.ToString() : "Value does not exist.");

                if(result.HasValue)
                    value = result.Value;

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return value;
        }

        public async Task<List<long>> GetTenants()
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            List<long> all = new List<long>();

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await myDictionary.CreateEnumerableAsync(tx);
                var e = result.GetAsyncEnumerator();

                while (e.MoveNextAsync(new CancellationToken()).Result)
                {
                    all.Add(long.Parse(e.Current.Key));
                }

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return all;
        }

        public async Task<bool> AddTenantInProgress(long tenantId, string state)
        {
            var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<string>>("InProgressQueue");// TODO Decide if queue or dictionary as unique entries

            using (var tx = this.StateManager.CreateTransaction())
            {
                string value = string.Format("{0}:{1}", tenantId, state);
                await queue.EnqueueAsync(tx, value);// TODO Handle exception thrown if fails

                ServiceEventSource.Current.ServiceMessage(this.Context, "Queue Element to InProgressQueue: {0}", value);

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return true;
        }

        public async Task<long> GetTenantInProgress()
        {
            var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<string>>("InProgressQueue");
            long taskId = -1;

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await queue.TryDequeueAsync(tx);

                ServiceEventSource.Current.ServiceMessage(this.Context, "DeQueue Element: {0}", result.HasValue ? result.Value.ToString() : "Value does not exist.");

                if (result.HasValue)
                {
                    string entry = result.Value;
                    taskId = long.Parse(entry.Split(':')[0]);
                }

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return taskId;
        }

        public async Task<bool> TransitionTenantToTerminal(long tenantId, string state)
        {
            var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<string>>("TerminalQueue");

            using (var tx = this.StateManager.CreateTransaction())
            {
                string value = string.Format("{0}:{1}", tenantId, state);
                await queue.EnqueueAsync(tx, value);// TODO Handle exception thrown if fails

                ServiceEventSource.Current.ServiceMessage(this.Context, "EnQueue Element to TerminalQueue: {0}", value);

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return true;
        }

        public async Task<long> GetTenantInTerminalState()
        {
            var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<string>>("TerminalQueue");
            long taskId = -1;

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await queue.TryDequeueAsync(tx);

                ServiceEventSource.Current.ServiceMessage(this.Context, "TerminalQueue, DeQueue Element: {0}", result.HasValue ? result.Value.ToString() : "Value does not exist.");

                if (result.HasValue)
                {
                    string entry = result.Value;
                    taskId = long.Parse(entry.Split(':')[0]);
                }

                // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                // discarded, and nothing is saved to the secondary replicas.
                await tx.CommitAsync();
            }

            return taskId;
        }
    }
}
