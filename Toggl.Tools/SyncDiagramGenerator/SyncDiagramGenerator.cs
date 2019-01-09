using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Runtime.CompilerServices;
using NSubstitute;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;

namespace SyncDiagramGenerator
{
    internal sealed class SyncDiagramGenerator
    {
        private readonly static string[] commonInterfacePrefixes =
        {
            "IDatabase", "IThreadSafe"
        };

        public (List<Node> Nodes, List<Edge> Edges) GenerateGraph()
        {
            Console.WriteLine("Configuring state machine");

            var entryPoints = new StateMachineEntryPoints();
            var configurator = new Configurator();
            configureTransitions(configurator, entryPoints);

            Console.WriteLine("Extracting states and state results");

            var allStateResults = getAllStateResultsByState(configurator.AllDistinctStatesInOrder);
            var stateNodes = makeNodesForStates(configurator.AllDistinctStatesInOrder);

            var stateResultCount = allStateResults.Sum(s => s.StateResults.Count);
            Console.WriteLine($"Found {stateNodes.Count} states and {stateResultCount} state results");

            var edges = getEdgesBetweenStates(allStateResults, configurator, stateNodes);
            var nodes = stateNodes.Values.ToList();

            Console.WriteLine("Created graph nodes and edges");

            var entryPointCount = addEntryPoints(edges, nodes, entryPoints, configurator, stateNodes);
            var looseEndCount = addLooseEnds(edges, nodes, allStateResults, configurator, stateNodes);

            Console.WriteLine($"Found and added {entryPointCount} entry points and {looseEndCount} loose ends");

            return (nodes, edges);
        }

        private int addLooseEnds(List<Edge> edges, List<Node> nodes,
            List<(object State, List<(IStateResult Result, string Name)> StateResults)> allStateResults,
            Configurator configurator,
            Dictionary<object, Node> stateNodes)
        {
            var count = 0;
            foreach (var (state, result) in allStateResults
                .SelectMany(results => results.StateResults
                    .Where(r => !configurator.Transitions.ContainsKey(r.Result))
                    .Select(r => (results.State, r))))
            {
                var node = new Node
                {
                    Label = "Loose End",
                    Type = Node.NodeType.LooseEnd
                };
                nodes.Add(node);

                var edge = new Edge
                {
                    From = stateNodes[state],
                    To = node,
                    Label = result.Name
                };
                edges.Add(edge);

                count++;
            }

            return count;
        }

        private int addEntryPoints(List<Edge> edges, List<Node> nodes, StateMachineEntryPoints entryPoints,
            Configurator configurator, Dictionary<object, Node> stateNodes)
        {
            var count = 0;
            foreach (var (property, stateResult) in entryPoints.GetType()
                .GetProperties()
                .Where(isStateResultProperty)
                .Select(p => (p, (IStateResult)p.GetValue(entryPoints))))
            {
                var node = new Node
                {
                    Label = property.Name,
                    Type = Node.NodeType.EntryPoint
                };
                nodes.Add(node);

                if (configurator.Transitions.TryGetValue(stateResult, out var state))
                {
                    var edge = new Edge
                    {
                        From = node,
                        To = stateNodes[state.State],
                        Label = ""
                    };
                    edges.Add(edge);
                }

                count++;
            }

            return count;
        }

        private List<Edge> getEdgesBetweenStates(
            List<(object State, List<(IStateResult Result, string Name)> StateResults)> allStateResults,
            Configurator configurator, Dictionary<object, Node> stateNodes)
        {
            return allStateResults
                .SelectMany(results =>
                    results.StateResults
                        .Where(sr => configurator.Transitions.ContainsKey(sr.Result))
                        .Select(sr => edge(results.State, configurator.Transitions[sr.Result], stateNodes, sr.Name))
                )
                .ToList();
        }

        private Edge edge(object fromState, (object State, Type ParameterType) transition,
            Dictionary<object, Node> stateNodes, string name)
        {
            return new Edge
            {
                From = stateNodes[fromState],
                To = stateNodes[transition.State],
                Label = transition.ParameterType == null
                    ? name
                    : $"{name}<{fullGenericTypeName(transition.ParameterType)}>"
            };
        }

        private Dictionary<object, Node> makeNodesForStates(List<object> allStates)
        {
            return allStates.ToDictionary(s => s,
                s => new Node
                {
                    Label = fullGenericTypeName(s.GetType()),
                    Type = nodeType(s)
                });
        }

        private Node.NodeType nodeType(object state)
        {
            switch (state)
            {
                case InvalidTransitionState _:
                    return Node.NodeType.InvalidTransitionState;
                case WaitForAWhileState _:
                    return Node.NodeType.RetryLoop;
                case DeadEndState _:
                case FailureState _:
                    return Node.NodeType.DeadEnd;
                default:
                    return Node.NodeType.Regular;
            }
        }

        private string fullGenericTypeName(Type type)
        {
            if (!type.IsGenericType)
                return cleanCommonInterfaces(type.Name);

            var genericArgumentNames = type.GetGenericArguments()
                .Select(fullGenericTypeName)
                .Distinct();

            if (typeof(ITuple).IsAssignableFrom(type))
            {
                return $"({string.Join(", ", genericArgumentNames)})";
            }

            var cleanedName = type.Name;
            var backTickIndex = cleanedName.IndexOf('`');
            if (backTickIndex >= 0)
                cleanedName = cleanedName.Substring(0, backTickIndex);

            return $"{cleanedName}<{string.Join(", ", genericArgumentNames)}>";
        }

        private string cleanCommonInterfaces(string typeName)
        {
            foreach (var prefix in commonInterfacePrefixes)
            {
                if (typeName.StartsWith(prefix))
                    return $"I{typeName.Substring(prefix.Length)}";
            }

            return typeName;
        }

        private static List<(object State, List<(IStateResult Result, string Name)> StateResults)>
            getAllStateResultsByState(List<object> allStates)
        {
            return allStates
                .Select(state => (state, state.GetType()
                    .GetProperties()
                    .Where(isStateResultProperty)
                    .Select(p => ((IStateResult)p.GetValue(state), p.Name))
                    .ToList())
                ).ToList();
        }

        private static bool isStateResultProperty(PropertyInfo p)
        {
            return typeof(IStateResult).IsAssignableFrom(p.PropertyType);
        }

        private static void configureTransitions(Configurator configurator, StateMachineEntryPoints entryPoints)
        {
            TogglSyncManager.ConfigureTransitions(
                configurator,
                Substitute.For<ITogglDatabase>(),
                Substitute.For<ITogglApi>(),
                Substitute.For<ITogglDataSource>(),
                Substitute.For<IScheduler>(),
                Substitute.For<ITimeService>(),
                Substitute.For<IAnalyticsService>(),
                entryPoints,
                Substitute.For<ISyncStateQueue>()
            );
        }
    }
}
