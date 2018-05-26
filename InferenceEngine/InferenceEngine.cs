using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using TenantUpdateActor.Interfaces;

namespace InferenceEngine
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class InferenceEngine : StatelessService
    {
        public InferenceEngine(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

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

            ServiceEventSource.Current.ServiceMessage(this.Context, "InferenceEngine has woken up and executing now.");

            // Get the tenant id from Runtime store
            long actorId = await runtimeStoreClient.GetTenantInTerminalState();

            if (actorId == -1)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "InferenceEngine: No tenant found to infer for.");
                return;
            }

            ServiceEventSource.Current.ServiceMessage(this.Context, "InferenceEngine: Got tenant {0} to work on.", actorId);

            // If workflow running for long and still, not complete, set to terminal state
        }
    }
}
