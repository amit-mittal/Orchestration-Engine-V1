using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using TenantUpdateActor.Interfaces;

namespace TenantUpdateOrchestrator
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class TenantUpdateOrchestrator : StatefulService
    {
        private Uri actorServicePath = new Uri(string.Format("fabric:/{0}/{1}", "Prototype", "TenantUpdateActorService"));

        public TenantUpdateOrchestrator(StatefulServiceContext context)
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
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Implement it with thread pool.
            IRuntimeStore runtimeStoreClient = ServiceProxy.Create<IRuntimeStore>(new Uri("fabric:/Prototype/RuntimeStore"), new ServicePartitionKey(0));

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await this.RunAsyncInternal(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        private async Task RunAsyncInternal(CancellationToken cancellationToken)
        {
            IRuntimeStore runtimeStoreClient = ServiceProxy.Create<IRuntimeStore>(new Uri("fabric:/Prototype/RuntimeStore"), new ServicePartitionKey(0));

            ServiceEventSource.Current.ServiceMessage(this.Context, "Service has woken up and executing now.");

            // Get the tenant id from Runtime store
            long actorId = await runtimeStoreClient.GetTenantInProgress();

            if (actorId == -1)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "No tenant found to work on.");
                return;
            }

            ServiceEventSource.Current.ServiceMessage(this.Context, "Got tenant {0} to work on.", actorId);

            ITenantUpdateActor tenantUpdateActor = ActorProxy.Create<ITenantUpdateActor>(new ActorId(actorId), actorServicePath);
            string currentState = await tenantUpdateActor.GetWorkflowState();

            ServiceEventSource.Current.ServiceMessage(this.Context, "Current state of tenant {0} is {1}", actorId, currentState);

            if (currentState == "Completed")// any terminal
            {
                // Move tenant id in runtime store to Terminal Queue
                ServiceEventSource.Current.ServiceMessage(this.Context, "Moving the tenant to terminal state.");
                await runtimeStoreClient.TransitionTenantToTerminal(actorId, "Completed");

                // Delete actor
                ServiceEventSource.Current.ServiceMessage(this.Context, "Deleting the actor {0} in service.", actorId);
                ActorId actorToDelete = new ActorId(actorId);
                IActorService myActorServiceProxy = ActorServiceProxy.Create(actorServicePath, actorToDelete);
                await myActorServiceProxy.DeleteActorAsync(actorToDelete, cancellationToken);
            }
            else
            {
                // TODO Call Execute, mainly to make sure that it is doing work and if needed, move to alerted state
                /*ServiceEventSource.Current.ServiceMessage(this.Context, "Executing the actor {0} in service.", actorId);
                await tenantUpdateActor.Execute();*/

                // Re-put to the in-progress queue
                await runtimeStoreClient.AddTenantInProgress(actorId, "InProgress");
            }

            // If workflow running for long and still, not complete, set to terminal state
        }
    }
}
