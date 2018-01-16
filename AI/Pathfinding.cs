using System.Collections.Generic;
using System.Linq;

namespace AI
{
    public class Pathfinding
    {
        List<Node_DTO> open;
        List<NodeConnection> closed;
        Matching matcher;

        public IEnumerable<MatchingArc> MatchPaths(Network_DTO optiplan, Network_DTO nvdb)
        {
            var optiplanLookup = matcher.GenerateTable(optiplan.nodes, optiplan.arcs);
            var nvdbNodeLookup = nvdb.nodes.ToDictionary(a => a.id);

            foreach (var a in nvdb.arcs)
            {
                var startNode = matcher.GetNodeInOtherNetwork(nvdbNodeLookup[a.fromNodeId], optiplan.nodes);
                var endNode = matcher.GetNodeInOtherNetwork(nvdbNodeLookup[a.toNodeId], optiplan.nodes);



            }
            //open.Add(origin);
            //var start = origin.start;
            //var stop = origin.stop;
            //bool found = false;
            //while (!found && open.Count != 0){
            //    foreach (var arc in network){
            //        if (IsClose(start, arc.start) || IsClose(start, arc.stop)){
            //        }
            //    }
            //    //pick a node 
            //    //find connecting arcs
            //    //get the end nodes from the arcs
            //    //the lenght of the arc is the weight
            //}
            return null;
        }
    }

    class Path
    {
        string previousNodeId;
        string arcId;
        double distance;
    }
}

//Denna ska via arcs gå från node till node genom attributen "fromNodeId" och "toNodeId" med 
//"length" som vikt.