using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SyncDiagramGenerator
{
    internal sealed class DotFileWriter
    {
        private static readonly string[] subGraphStartNodes =
        {
            "LookForChangeToPushState", "LookForSingletonChangeToPushState",
            "EnsureFetchListSucceededState", "EnsureFetchSingletonSucceededState",
            "TryFetchInaccessibleProjectsState",
        };

        private static readonly string[] subGraphTerminationNodes =
        {
            "FetchAllSinceState", "DeadEndState", "Loose End",
            "WaitForAWhileState",
        };

        public void WriteToFile(string outPath, List<Node> nodes, List<Edge> edges)
        {
            Console.WriteLine($"Assigning unique ids to {nodes.Count} nodes");

            assignUniqueIdsTo(nodes);

            Console.WriteLine($"Cleaning up labels of {nodes.Count} nodes and {edges.Count} edges");

            cleanUpLabels(nodes);
            cleanUpLabels(edges);

            Console.WriteLine("Creating sub graphs");

            markSubGraphStartNodes(nodes);
            floodFillSubGraphs(edges);

            Console.WriteLine($"Serialising {nodes.Count} nodes and {edges.Count} edges to DOT format");

            var fileContent = serialise(nodes, edges);

            Console.WriteLine($"Writing DOT file to {outPath}");

            File.WriteAllText(outPath, fileContent);
        }

        private void markSubGraphStartNodes(List<Node> nodes)
        {
            foreach (var node in nodes.Where(node => subGraphStartNodes.Any(name => node.Id.Contains(name))))
            {
                node.SubGraph = new SubGraph { SourceNode = node };
                node.SubGraph.Nodes.Add(node);
            }
        }

        private void floodFillSubGraphs(List<Edge> edges)
        {
            var propagationCandidates = edges.ToList();

            while (propagationCandidates.RemoveAll(tryPropagateSubGraph) > 0)
            {
            }
        }

        private bool tryPropagateSubGraph(Edge edge)
        {
            var subGraph = edge.From.SubGraph;
            if (subGraph == null || edge.To.SubGraph != null)
                return false;

            if (subGraphTerminationNodes.Any(name => edge.To.Id.Contains(name)))
                return false;

            edge.To.SubGraph = subGraph;
            subGraph.Nodes.Add(edge.To);
            subGraph.Edges.Add(edge);
            return true;
        }

        private void cleanUpLabels(IEnumerable<ILabeled> labeledList)
        {
            foreach (var labeled in labeledList)
            {
                labeled.Label = cleanLabel(labeled.Label);
            }
        }

        private string cleanLabel(string label)
        {
            var typeSeparatorIndex = label.IndexOf('<');

            var (name, types) = typeSeparatorIndex == -1
                ? (label, null)
                : (label.Substring(0, typeSeparatorIndex), label.Substring(typeSeparatorIndex));

            const string suffixToStrip = "state";
            var cleanName = name.EndsWith(suffixToStrip, StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - suffixToStrip.Length)
                : name;

            return types == null
                ? cleanName
                : $@"{cleanName}\n{types}";
        }

        private void assignUniqueIdsTo(List<Node> nodes)
        {
            string previousId = null;
            var i = 0;
            foreach (var node in nodes.OrderBy(n => n.Label))
            {
                var id = node.Label;
                if (id != previousId)
                {
                    node.Id = $"\"{id}\"";
                    i = 0;
                }
                else
                {
                    i++;
                    node.Id = $"\"{id + i}\"";
                }

                previousId = id;
            }
        }

        private string serialise(List<Node> nodes, List<Edge> edges)
        {
            var builder = new StringBuilder();

            writeFileStart(builder);

            foreach (var subGraphGroup in nodes.GroupBy(n => n.SubGraph))
            {
                writeSubGraphGroup(subGraphGroup, builder);
            }

            foreach (var edge in edges)
            {
                writeEdge(edge, builder);
            }

            writeFileEnd(builder);

            return builder.ToString();
        }

        private void writeSubGraphGroup(IGrouping<SubGraph, Node> subGraphGroup, StringBuilder builder)
        {
            var subGraph = subGraphGroup.Key;

            if (subGraph != null)
                writeSubGraphHeader(subGraph, builder);

            foreach (var node in subGraphGroup)
            {
                writeNode(node, builder);
            }

            if (subGraph != null)
                writeSubGraphFooter(builder);
        }

        private void writeSubGraphHeader(SubGraph subGraph, StringBuilder builder)
        {
            // "cluster" is a syntactic prefix, do not change!
            builder.AppendLine($"subgraph \"cluster {subGraph.SourceNode.Id.Trim('"')}\" {{");
        }

        private void writeSubGraphFooter(StringBuilder builder)
        {
            builder.AppendLine("}");
        }

        private static void writeFileStart(StringBuilder builder)
        {
            builder.AppendLine("digraph SyncGraph {");
            builder.AppendLine("newrank=true;");
            builder.AppendLine("node [shape=box,style=rounded];");
        }

        private static void writeFileEnd(StringBuilder builder)
        {
            builder.AppendLine("}");
        }

        private static void writeEdge(Edge edge, StringBuilder builder)
        {
            var edgeAttributes = getAttributes(edge);
            var attributeString = buildAttributeString(edgeAttributes);
            builder.AppendLine($"{edge.From.Id} -> {edge.To.Id} [{attributeString}];");
        }

        private void writeNode(Node node, StringBuilder builder)
        {
            var nodeAttributes = getAttributes(node);
            var attributeString = buildAttributeString(nodeAttributes);
            builder.AppendLine($"{node.Id} [{attributeString}];");
        }

        private static string buildAttributeString(List<(string Key, string Value)> edgeAttributes)
        {
            return string.Join(",", edgeAttributes.Select(a => $"{a.Key}=\"{a.Value}\""));
        }

        private static List<(string Key, string Value)> getAttributes(Edge edge)
        {
            var attributes = new List<(string, string)>
            {
                ("label", edge.Label)
            };

            if (edge.From.Type == Node.NodeType.RetryLoop || edge.To.Type == Node.NodeType.RetryLoop)
            {
                attributes.Add(("color", "lightblue"));
                attributes.Add(("fontcolor", "lightblue"));
            }
            else if (edge.From.Type == Node.NodeType.APIDelayReset || edge.To.Type == Node.NodeType.APIDelayReset)
            {
                attributes.Add(("color", "palegreen"));
                attributes.Add(("fontcolor", "palegreen"));
            }

            return attributes;
        }

        private List<(string Key, string Value)> getAttributes(Node node)
        {
            var attributes = new List<(string, string)>
            {
                ("label", node.Label)
            };

            switch (node.Type)
            {
                case Node.NodeType.EntryPoint:
                    attributes.Add(("color", "green"));
                    break;
                case Node.NodeType.DeadEnd:
                    attributes.Add(("color", "orange"));
                    break;
                case Node.NodeType.InvalidTransitionState:
                    attributes.Add(("color", "red"));
                    break;
                case Node.NodeType.LooseEnd:
                    attributes.Add(("color", "red"));
                    attributes.Add(("style", "rounded,filled"));
                    break;
                case Node.NodeType.RetryLoop:
                    attributes.Add(("color", "lightblue"));
                    attributes.Add(("style", "rounded,filled"));
                    break;
                case Node.NodeType.APIDelayReset:
                    attributes.Add(("color", "palegreen"));
                    attributes.Add(("style", "rounded,filled"));
                    break;
            }

            return attributes;
        }
    }
}
