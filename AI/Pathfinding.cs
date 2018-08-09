using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AI
{
    public class Pathfinding
    {
        List<NodePath> open = new List<NodePath>();
        List<NodePath> closed = new List<NodePath>();
        Matching matcher = new Matching();

        public IEnumerable<MatchingArc> MatchPaths(Network_DTO optiplan, Network_DTO nvdb)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //Filters out walking roads
            var optiplanLookup = matcher.GenerateTable(optiplan.nodes,
                optiplan.arcs.Where(i => i.roadType == ArcRoadType.Driving));
            var nvdbNodeLookup = nvdb.nodes.ToDictionary(a => a.id);

            List<MatchingArc> matches = new List<MatchingArc>();
            int counter = 1;
            int numOfMatches = 0;
            foreach (var nvdbArc in nvdb.arcs)
            {
                open.Clear();
                closed.Clear();
                //Noder
                if (nvdbArc.fromNodeId == null || nvdbArc.toNodeId == null)
                {
                    //Console.WriteLine("Finns ej");
                    continue;
                }

                var startNode = matcher.GetNodeInOtherNetwork(nvdbNodeLookup[nvdbArc.fromNodeId], optiplan.nodes);
                var endNode = matcher.GetNodeInOtherNetwork(nvdbNodeLookup[nvdbArc.toNodeId], optiplan.nodes);

                if (startNode != null && endNode != null && optiplanLookup[endNode.id].Any())
                {
                    var path = MatchPath(nvdbArc.id, startNode, endNode, nvdbArc.length, optiplanLookup);

                    if (path != null)
                    {
                        matches.Add(path);
                        numOfMatches++;
                    }
                }
                else
                {
                    //Console.WriteLine("null");
                }
                //Console.WriteLine(counter + " of  " + nvdb.arcs.Count());
                counter++;
            }
            sw.Stop();
            Console.WriteLine("Matched " + numOfMatches + " of " + nvdb.arcs.Count() + " in " + sw.Elapsed);
            return matches;
        }

        public MatchingArc MatchPath(string name, Node_DTO start, Node_DTO end, double distance, ILookup<string, NodeConnection> table)
        {
            //Node paths
            open.Add(new NodePath(start, null, 0, null));
            bool found = false;
            NodePath np;
            while (!found)
            {
                //if (closed.Count % 100 == 0 && closed.Count > 0)
                //{
                //    var a = open.Where(b => !closed.Contains(b));
                //    Console.WriteLine("Loops: " + closed.Count + " open nodes: " + a.Count());
                //}

                //Get the node with the lowest distance that is not in the closed list
                NodePath current = open
                        .Where(t => !closed
                            .Where(p => p.arcId == t.arcId).Any())
                        .OrderBy(t => t.distance)
                        .FirstOrDefault();

                // This should be removed for final calculations? Or else there is chance that the algorithm stops before the real answer
                if (closed.Count > 300)
                {
                    //Console.WriteLine("Start: " + start.location.lat + " " + start.location.lon +
                    //    " Stop: " + end.location.lat + " " + end.location.lon + "ID: " + name);
                    return null;
                }
                if (current == null)
                {
                    return null;
                }

                //Add the current node to the closed list
                closed.Add(current);

                //Lookup table for the connections of each node
                var nodeConnections = table[current.currentNode.id];

                //Go through every connection for a node
                foreach (var connection in nodeConnections)
                {
                    NodeConnection ILConnection = null;
                    if (connection.from.id == current.currentNode.id)
                    {
                        ILConnection =
                            new NodeConnection(
                                connection.from,
                                connection.to,
                                connection.weight + current.distance,
                                connection.arcId);
                    }
                    else if (connection.to.id == current.currentNode.id)
                    {
                        ILConnection =
                            new NodeConnection(
                                connection.to,
                                connection.from,
                                connection.weight + current.distance,
                                connection.arcId);
                    }
                    else
                    {
                        Console.WriteLine("Något konstigt hände!");
                    }

                    //If the node has a connection to the goal                    

                    if (ILConnection.to.id == end.id)
                    {
                        var diff1 = Math.Abs(distance - ILConnection.weight);
                        var distPercent = distance * 0.025;
                        if (Math.Abs(distance - ILConnection.weight) < distance * 0.025)
                        {
                            found = true;
                            //Console.WriteLine(ILConnection.arcId);
                            np = new NodePath(
                                ILConnection.to,
                                ILConnection.from,
                                ILConnection.weight,
                                ILConnection.arcId);
                            closed.Add(np);
                            break;
                        }
                    }
                    else if (!closed.Where(p => p.arcId == ILConnection.arcId).Any())
                    {
                        NodePath temp = new NodePath(
                            ILConnection.to,
                            ILConnection.from,
                            ILConnection.weight,
                            ILConnection.arcId);
                        if (!closed.Contains(temp))
                        {
                            open.Add(temp);
                        }
                    }
                }
            }
            if (found)
            {
                MatchingArc match = ConnectPath(name);
                return match;
            }
            else
            {
                return null;
            }
        }

        MatchingArc ConnectPath(string original)
        {
            NodePath current = closed.Last();

            var lookup = closed.ToLookup(p => p.currentNode.id);

            foreach (var c in closed)
            {
                if (c.previousNode != null && false == lookup[c.previousNode.id].Any())
                    throw new Exception();
            }

            MatchingArc matchingArcs = new MatchingArc();
            List<string> temp = new List<string>();
            bool end = false;

            while (!end)
            {
                temp.Add(current.arcId);

                if (current.previousNode != null)
                {
                    var tempCurrent =
                        closed
                        .Where(a => a.currentNode.id == current.previousNode.id)
                        .FirstOrDefault();

                    if (tempCurrent == null)
                        Console.WriteLine("null");

                    current = tempCurrent;
                }
                else
                {
                    end = true;
                }
            }

            matchingArcs.nvdbArcId = original;
            matchingArcs.optiplanArcIds = temp;
            return matchingArcs;
        }
    }
}