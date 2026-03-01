using MediatR;
using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Application.Workspaces.Queries;

public record GetWorkspacesQuery : IRequest<IReadOnlyList<WorkspaceInfo>>;

public class GetWorkspacesHandler : IRequestHandler<GetWorkspacesQuery, IReadOnlyList<WorkspaceInfo>>
{
    private readonly IWorkspaceProvider _provider;
    public GetWorkspacesHandler(IWorkspaceProvider provider) => _provider = provider;

    public Task<IReadOnlyList<WorkspaceInfo>> Handle(GetWorkspacesQuery request, CancellationToken ct)
        => Task.FromResult(_provider.GetAllWorkspaces());
}

public record GetPipelinesQuery : IRequest<IReadOnlyList<PipelineInfo>>;

public class GetPipelinesHandler : IRequestHandler<GetPipelinesQuery, IReadOnlyList<PipelineInfo>>
{
    private readonly IPipelineProvider _provider;
    public GetPipelinesHandler(IPipelineProvider provider) => _provider = provider;

    public Task<IReadOnlyList<PipelineInfo>> Handle(GetPipelinesQuery request, CancellationToken ct)
        => Task.FromResult(_provider.GetAllPipelines());
}
