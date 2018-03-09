using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AI
{
    class Matching
    {
        public Node_DTO GetNodeInOtherNetwork(Node_DTO origin, IEnumerable<Node_DTO> nodeList)
        {
            //if (origin.id == "2:995188")
            //    Console.WriteLine("stop");
            Node_DTO closestMatch = null;
            double closestDist = 999999999;
            foreach (var node in nodeList)
            {
                double dist = Processing.CalculateDistanceInKilometers(origin.location, node.location);
                //Distance in km
                if (dist < 0.02)
                {
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestMatch = node;
                    }
                }
            }
            return closestMatch;
        }

        public ILookup<string, NodeConnection> GenerateTable(IEnumerable<Node_DTO> nodes, IEnumerable<Arc_DTO> arcs)
        {
            var nLookup = nodes.ToDictionary(n => n.id);
            var connectionsLookup =
                arcs
                //.Where(b =>
                //    b.fromNodeId != null && b.toNodeId != null &&
                //    nLookup[b.fromNodeId] != null && nLookup[b.toNodeId] != null)
                .Select(a =>
                {
                    Node_DTO fromValue, toValue;
                    if (nLookup.TryGetValue(a.fromNodeId, out fromValue) &&
                    nLookup.TryGetValue(a.toNodeId, out toValue))
                    {
                        return new NodeConnection(fromValue, toValue, a.length, a.id);
                    }
                    else
                        return null;
                })
                .Where(a => a != null)
                .SelectMany(i =>
                    new[] {
                        new { key = i.from.id, NodeConnection = i },
                        new { key = i.to.id, NodeConnection = i }
                    })
                .ToLookup(i => i.key, i => i.NodeConnection);

            return connectionsLookup;
        }

        public ILookup<string, NodeConnection> LoadTable(string name)
        {
            string jsonText = System.IO.File.ReadAllText(name);
            var temp = JsonConvert.DeserializeObject<ILookup<string, NodeConnection>>(jsonText);
            return temp;
        }
    }
}