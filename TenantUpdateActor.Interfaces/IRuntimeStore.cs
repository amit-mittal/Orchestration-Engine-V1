using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace TenantUpdateActor.Interfaces
{
    public interface IRuntimeStore : IService
    {
        Task<string> HelloWorldAsync();

        Task<bool> AddTenant(long value);

        Task<bool> RemoveTenant(long value);

        Task<bool> UpdateTenant();

        Task<long> GetTenant(long id);

        Task<List<long>> GetTenants();

        Task<bool> AddTenantInProgress(long tenantId, string state);

        Task<long> GetTenantInProgress();

        Task<bool> TransitionTenantToTerminal(long tenantId, string newState);

        Task<long> GetTenantInTerminalState();
    }
}