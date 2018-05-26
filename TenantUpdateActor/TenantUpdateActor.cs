using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using TenantUpdateActor.Interfaces;

namespace TenantUpdateActor
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class TenantUpdateActor : Actor, ITenantUpdateActor, IRemindable
    {
        /// <summary>
        /// Initializes a new instance of TenantUpdateActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public TenantUpdateActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization

            ActorEventSource.Current.ActorMessage(this, "Registering the periodic reminder.");

            IActorReminder reminderRegistration = await this.RegisterReminderAsync(
                "FirstReminder",
                BitConverter.GetBytes(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(10));

            ActorEventSource.Current.ActorMessage(this, "Trying to set the workflow state as Begin");

            bool state = await this.StateManager.TryAddStateAsync("WorkflowState", "Begin");
            if (state == false)
            {
                ActorEventSource.Current.ActorMessage(this, "Set WorkflowState failed as it already exists.");
            }

            await this.StateManager.TryAddStateAsync("count", 0);
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName.Equals("FirstReminder"))
            {
                await this.Start();
            }
        }

        public async Task<string> GetWorkflowState()
        {
            string currentState = await this.StateManager.GetStateAsync<string>("WorkflowState");

            ActorEventSource.Current.ActorMessage(this, "Current State of Workflow is {0}", currentState);

            return currentState;
        }

        public Task<bool> Validate()
        {
            // Here we should be initializing all the helper classes and there can in arguments input to it
            throw new NotImplementedException();
        }

        public async Task Execute()
        {
            await Start();
        }

        private async Task<bool> Start()
        {
            string currentState = await this.StateManager.GetStateAsync<string>("WorkflowState");

            if (currentState == "Begin")
            {
                // take some action
                await this.StateManager.TryAddStateAsync("Temp", 0);

                await this.StateManager.AddOrUpdateStateAsync("WorkflowState", "Begin", (key, value) => "InProgress");
            }

            if (currentState == "InProgress")
            {
                // do some other work
                int temp = await this.StateManager.GetStateAsync<int>("Temp");
                ++temp;

                await this.StateManager.AddOrUpdateStateAsync("Temp", 0, (key, value) => temp > value ? temp : value);

                if (temp >= 5)
                {
                    await this.StateManager.AddOrUpdateStateAsync("WorkflowState", "Begin", (key, value) => "Completed");
                }
            }

            if (currentState == "Completed")
            {
                IActorReminder reminder = GetReminder("FirstReminder");
                await UnregisterReminderAsync(reminder);

                ActorEventSource.Current.ActorMessage(this, "Successfully unregistered the reminder.");
            }

            ActorEventSource.Current.ActorMessage(this, "Successfully executed the Actor lifecycle.");

            return true;
        }

        private async Task<bool> StartOld()
        {
            int currentValue = await this.StateManager.GetStateAsync<int>("count");

            if (currentValue > 5)
            {
                IActorReminder reminder = GetReminder("FirstReminder");
                await UnregisterReminderAsync(reminder);

                ActorEventSource.Current.ActorMessage(this, "Successfully unregistered the reminder.");

                return true;
            }

            int newValue = currentValue + 1;
            await this.StateManager.AddOrUpdateStateAsync("count", newValue, (key, value) => newValue);

            ActorEventSource.Current.ActorMessage(this, "Successfully incremented the value of state.");

            return true;
        }

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <returns></returns>
        Task<int> ITenantUpdateActor.GetCountAsync(/*CancellationToken cancellationToken*/)
        {
            return this.StateManager.GetStateAsync<int>("count");
        }

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        Task ITenantUpdateActor.SetCountAsync(int count/*, CancellationToken cancellationToken*/)
        {
            // Requests are not guaranteed to be processed in order nor at most once.
            // The update function here verifies that the incoming count is greater than the current count to preserve order.
            return this.StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value);
        }
    }
}
