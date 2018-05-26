using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TenantUpdateActor.Interfaces;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using System.Fabric;
using System.Fabric.Query;
using Microsoft.ServiceFabric.Actors.Query;
using System.Threading;

namespace TenantUpdateOrchestratorFE.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private Uri actorServicePath = new Uri(string.Format("fabric:/{0}/{1}", "Prototype", "TenantUpdateActor"));

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            List<ActorInformation> l = this.GetAllActors(actorServicePath).Result;
            List<string> result = new List<string>();

            foreach(ActorInformation a in l)
            {
                result.Add(a.ActorId.GetStringId());
            }

            return result;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            ITenantUpdateActor tenantUpdateActor = ActorProxy.Create<ITenantUpdateActor>(new ActorId(id), actorServicePath);
            Task<int> t1 = tenantUpdateActor.GetCountAsync();
            int k = t1.Result;

            return k.ToString();
        }

        // POST api/values/5
        [HttpPost("{id}")]
        public void Post(int id)
        {
            ITenantUpdateActor tenantUpdateActor = ActorProxy.Create<ITenantUpdateActor>(new ActorId(id), actorServicePath);
            Task t = tenantUpdateActor.SetCountAsync(1);
            t.Wait();
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
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
