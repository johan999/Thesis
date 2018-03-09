using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AI
{
    //A road network
    public class Network_DTO
    {
        public IEnumerable<ArcRestriction_DTO> restrictions { get; set; }   //The restrictions for a network
        public string id { get; set; }                         //Id of network
        public IEnumerable<Arc_DTO> arcs { get; set; }         //Arc of the network
        public IEnumerable<Node_DTO> nodes { get; set; }       //Nodes of the network        

        //Load a Json file into one network (Network_DTO)
        public Network_DTO LoadJson(string name)
        {
            //File path
            string jsonText = System.IO.File.ReadAllText(name);

            //Deserialize json and return
            return JsonConvert.DeserializeObject<Network_DTO>(jsonText);
        }

        public IEnumerable<Arc_DTO> GetDrivingRoads(Network_DTO network)
        {
            //Get roads for driving
            IEnumerable<Arc_DTO> roadArcs = network
                .arcs
                .Where(r => r.roadType == ArcRoadType.Driving).ToList();

            //Add start and stop nodes as variables for easier comparison
            foreach (Arc_DTO arc in roadArcs)
            {
                arc.length = Processing.CalculateDistanceInKilometers(arc.locations);
                arc.start = arc.locations.First();
                arc.stop = arc.locations.Last();
            }
            return roadArcs;
        }
    }
    //Restriction for an arc
    public class ArcRestriction_DTO
    {
        public string id { get; set; }  //Restriction id
        public IEnumerable<int> grades { get; set; }    //Grades which the restriction affect
        public int type { get; set; }           //Restriction number
        public string typeText { get; set; }    //Restriction name       
    }
    //A node in the network
    public class Node_DTO
    {
        public string id { get; set; }              //Node id
        public Location_DTO location { get; set; }  //Node location lat, lon
    }
    public enum ArcRoadType
    {
        Unknown,
        Walking,
        Driving
    }
    //An arc is a connection between two nodes
    public class Arc_DTO
    {
        public int roadClass { get; set; }          //Which vehicles can be driven on the road
        public ArcRoadType roadType { get; set; }   //Road or pavement
        public string id { get; set; }              //Arc id
        public string fromNodeId { get; set; }      //Start node
        public string toNodeId { get; set; }        //End node
        public double speed { get; set; }
        public IEnumerable<string> arcRestrictionIds { get; set; }  //List of arc restrictions
        public IEnumerable<Location_DTO> locations { get; set; }    //Node locations, lat, lon

        //After processing
        public Location_DTO start;
        public Location_DTO stop;
        public double length;
    }
    //Node location
    public class Location_DTO
    {
        public double lat { get; set; }
        public double lon { get; set; }
    }

    public class NodeConnection
    {
        public Node_DTO from;
        public Node_DTO to;
        public double weight;
        public string arcId;

        public NodeConnection(Node_DTO from, Node_DTO to, double weight, string id)
        {
            this.from = from;
            this.to = to;
            this.weight = weight;
            this.arcId = id;
        }

        public NodeConnection CreateNodeConnection(Node_DTO f, Node_DTO t, Arc_DTO a)
        {
            NodeConnection connection = new NodeConnection(f, t, a.length, a.id);
            return connection;
        }
    }

    public class MatchingArc
    {
        public string nvdbArcId;
        public IEnumerable<string> optiplanArcIds;
    }

    public class NodePath : IEquatable<NodePath>
    {
        public Node_DTO currentNode;
        public Node_DTO previousNode;
        public double distance;
        public string arcId;

        public NodePath(Node_DTO currentNode, Node_DTO previousNode, double distance, string arcId)
        {
            this.currentNode = currentNode;
            this.previousNode = previousNode;
            this.distance = distance;
            this.arcId = arcId;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NodePath);
        }

        private bool SameNodes(NodePath leg) =>
            (leg.currentNode?.id == currentNode?.id && leg.previousNode?.id == previousNode?.id) ||
            (leg.currentNode?.id == previousNode?.id && leg.previousNode?.id == currentNode?.id);

        public bool Equals(NodePath leg)
        {
            return leg != null && SameNodes(leg);
        }

        public override int GetHashCode()
        {
            return currentNode.id.GetHashCode() ^ previousNode.GetHashCode();
        }
    }
}