using Umbraco.Cms.Core.Sync;

namespace Kjac.SearchProvider.Algolia.Services;

internal abstract class AlgoliaIndexManagingServiceBase : AlgoliaServiceBase
{
    private readonly IServerRoleAccessor _serverRoleAccessor;

    protected AlgoliaIndexManagingServiceBase(IServerRoleAccessor serverRoleAccessor)
        => _serverRoleAccessor = serverRoleAccessor;

    protected bool ShouldNotManipulateIndexes() => _serverRoleAccessor.CurrentServerRole is ServerRole.Subscriber;
}
