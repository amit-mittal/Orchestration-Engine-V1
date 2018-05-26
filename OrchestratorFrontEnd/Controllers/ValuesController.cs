using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using TenantUpdateActor.Interfaces;

namespace OrchestratorFrontEnd.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private Uri actorServicePath = new Uri(string.Format("fabric:/{0}/{1}", "Prototype", "TenantUpdateActorService"));

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            //List<ActorInformation> l = this.GetAllActors(actorServicePath).Result;
            List<string> result = new List<string>();

            /*foreach (ActorInformation a in l)
            {
                result.Add(a.ActorId.GetLongId().ToString());
            }

            IRuntimeStore helloWorldClient = ServiceProxy.Create<IRuntimeStore>(new Uri("fabric:/Prototype/RuntimeStore"), new ServicePartitionKey(0));
            string message = helloWorldClient.HelloWorldAsync().Result;

            result.Add(message);*/

            IRuntimeStore helloWorldClient = ServiceProxy.Create<IRuntimeStore>(new Uri("fabric:/Prototype/RuntimeStore"), new ServicePartitionKey(0));
            List<long> message = helloWorldClient.GetTenants().Result;

            foreach (var m in message)
            {
                result.Add(m.ToString());
            }

            return result;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            /*ITenantUpdateActor tenantUpdateActor = ActorProxy.Create<ITenantUpdateActor>(new ActorId(id), actorServicePath);
            Task<int> t1 = tenantUpdateActor.GetCountAsync();
            int k = t1.Result;

            return k.ToString();*/

            IRuntimeStore helloWorldClient = ServiceProxy.Create<IRuntimeStore>(new Uri("fabric:/Prototype/RuntimeStore"), new ServicePartitionKey(0));
            long value = helloWorldClient.GetTenant(id).Result;

            return value.ToString();
        }

        // POST api/values/5
        [HttpPost("{id}")]
        public void Post(int id)
        {
            /*ITenantUpdateActor tenantUpdateActor = ActorProxy.Create<ITenantUpdateActor>(new ActorId(id), actorServicePath);
            Task t = tenantUpdateActor.SetCountAsync(1);
            t.Wait();*/

            IRuntimeStore helloWorldClient = ServiceProxy.Create<IRuntimeStore>(new Uri("fabric:/Prototype/RuntimeStore"), new ServicePartitionKey(0));
            helloWorldClient.AddTenant(id).Wait();
        }

        // PUT api/values/5
        [HttpPut("{tenantId}")]
        public void Put(int tenantId)
        {
            IRuntimeStore helloWorldClient = ServiceProxy.Create<IRuntimeStore>(new Uri("fabric:/Prototype/RuntimeStore"), new ServicePartitionKey(0));
            helloWorldClient.AddTenantInProgress(tenantId, "Begin");
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private async Task<List<ActorInformation>> GetAllActors(Uri servicePath)
        {
            FabricClient.QueryClient query = new FabricClient().QueryManager;
            List<long> partitions = new List<long>();
            string continuation = null;
            do
            {
                ServicePartitionList partitionList =
                    await query.GetPartitionListAsync(servicePath, continuation);

                foreach (Partition item in partitionList)
                {
                    ServicePartitionInformation info = item.PartitionInformation;
                    switch (info.Kind)
                    {
                        case ServicePartitionKind.Int64Range:
                            partitions.Add(((Int64RangePartitionInformation)info).LowKey);
                            break;

                        case ServicePartitionKind.Singleton:
                            partitions.Add(0L);
                            break;

                        default:
                            throw new InvalidOperationException(
                                string.Format(
                                    "Unexpected partition kind.  Found {0}, expected either Int64Range or Singleton",
                                    info.Kind));
                    }
                }
                continuation = partitionList.ContinuationToken;
            } while (!string.IsNullOrEmpty(continuation));

            List<ActorInformation> activeActors = new List<ActorInformation>();
            foreach (long partitionId in partitions)
            {
                IActorService actorServiceProxy = ActorServiceProxy.Create(servicePath, partitionId);
                ContinuationToken continuationToken = null;

                do
                {
                    PagedResult<ActorInformation> page =
                        await actorServiceProxy.GetActorsAsync(continuationToken, CancellationToken.None);
                    activeActors.AddRange(page.Items);
                    continuationToken = page.ContinuationToken;
                } while (continuationToken != null);
            }
            return activeActors;
        }
    }
}
