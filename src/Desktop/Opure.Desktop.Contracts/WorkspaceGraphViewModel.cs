using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opure.Workspace.Contracts;

namespace Opure.Desktop.Contracts;

public sealed class WorkspaceGraphViewModel
{
    private readonly IWorkspaceGraphStore _store;
    private readonly SynchronizationContext? _syncContext;

    public ObservableCollection<GraphNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<GraphEdgeViewModel> Edges { get; } = new();

    public WorkspaceGraphViewModel(IWorkspaceGraphStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _syncContext = SynchronizationContext.Current;
    }

    public async Task LoadAndLayoutGraphAsync(CancellationToken cancellationToken)
    {
        var graph = await _store.LoadGraphAsync(cancellationToken);
        
        var layoutNodes = await Task.Run(() => ForceDirectedLayoutEngine.Calculate(graph), cancellationToken);

        Action updateAction = () =>
        {
            Nodes.Clear();
            Edges.Clear();

            var nodeLookup = layoutNodes.ToDictionary(n => n.Id);

            foreach (var node in layoutNodes)
            {
                Nodes.Add(node);
            }

            foreach (var edge in graph.Edges)
            {
                if (nodeLookup.TryGetValue(edge.SourceId, out var sourceVm) &&
                    nodeLookup.TryGetValue(edge.TargetId, out var targetVm))
                {
                    Edges.Add(new GraphEdgeViewModel(sourceVm, targetVm));
                }
            }
        };

        if (_syncContext != null)
        {
            _syncContext.Post(_ => updateAction(), null);
        }
        else
        {
            updateAction();
        }
    }

    public void HighlightNode(string nodeId)
    {
        Action highlightAction = () =>
        {
            foreach (var node in Nodes)
            {
                if (node.Id == nodeId)
                {
                    node.IsActive = true;
                }
            }
        };

        if (_syncContext != null)
        {
            _syncContext.Post(_ => highlightAction(), null);
        }
        else
        {
            highlightAction();
        }
    }

    public void ClearHighlights()
    {
        Action clearAction = () =>
        {
            foreach (var node in Nodes)
            {
                node.IsActive = false;
            }
        };

        if (_syncContext != null)
        {
            _syncContext.Post(_ => clearAction(), null);
        }
        else
        {
            clearAction();
        }
    }
}
