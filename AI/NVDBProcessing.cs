using MightyLittleGeodesy.Positions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AI
{
    class NVDBProcessing
    {
        public const double THRESHOLD = 0.02;
        class NvdbArcInfo
        {
            public string id;
            public int speed;
        }
        class NvdbArcGeometry
        {
            public string id;
            public double length;
            public IEnumerable<Location_DTO> locations;
        }
        public class NvdbNodeInfo
        {
            public IEnumerable<string> connections;
            public string ID;
            public Location_DTO location;
        }

        public Network_DTO CreateNetworkFromXML(string name)
        {
            XDocument doc = XDocument.Load(name);
            //Nodes
            var nodeInfos = doc
                .Descendants("NW_RefNode")
                .Select(RN =>
                {
                    var id = RN
                        .Attribute("uuid")
                        .Value;

                    var locations = RN
                        .Descendants("coordinate")
                        .Select(nr =>
                        {
                            var numbers = nr.Elements("Number").ToList();
                            double X = double.Parse(numbers[0].Value, CultureInfo.InvariantCulture);
                            double Y = double.Parse(numbers[1].Value, CultureInfo.InvariantCulture);

                            SWEREF99Position swePos = new SWEREF99Position(X, Y);
                            WGS84Position wgsPos = swePos.ToWGS84();

                            return new Location_DTO()
                            {
                                lat = Math.Round(wgsPos.Latitude, 7, MidpointRounding.AwayFromZero),
                                lon = Math.Round(wgsPos.Longitude, 7, MidpointRounding.AwayFromZero)
                            };
                        });

                    var refNodes = RN
                        .Descendants("connectedPort")
                        .Select(CP =>
                        {
                            return GetUntilOrEmpty(CP.Attribute("uuidref").Value);
                        });

                    return new NvdbNodeInfo()
                    {
                        connections = refNodes,
                        ID = id,
                        location = locations.First()
                    };
                });
            //Arc
            var arcInfos = doc
                .Descendants("FI_ChangedFeatureWithHistory")
                .Select(FI =>
                {
                    var id = FI
                        .Descendants("NW_LineExtent")
                        .First()
                        .Descendants("locationInstance")
                        .First()
                        .Attribute("uuidref")
                        .Value;

                    var speed = int.Parse(FI
                            .Descendants("number")
                            .First()
                            .Value);

                    return new NvdbArcInfo()
                    {
                        id = id,
                        speed = speed
                    };
                }).ToList();

            var arcInfoById = arcInfos.ToLookup(a => a.id);

            var arcGeometry = doc
                .Descendants("NW_RefLink")
                .Select(RL =>
                {
                    var id = RL
                        .Attribute("uuid")
                        .Value;

                    var length = RL
                        .Descendants("length")
                        .First()
                        .Value;

                    var locations = RL
                        .Descendants("coordinate")
                        .Select(nr =>
                        {
                            var numbers = nr.Elements("Number").ToList();

                            double X = double.Parse(numbers[0].Value, CultureInfo.InvariantCulture);
                            double Y = double.Parse(numbers[1].Value, CultureInfo.InvariantCulture);

                            SWEREF99Position swePos = new SWEREF99Position(X, Y);
                            WGS84Position wgsPos = swePos.ToWGS84();

                            return new Location_DTO()
                            {
                                lat = Math.Round(wgsPos.Latitude, 7, MidpointRounding.AwayFromZero),
                                lon = Math.Round(wgsPos.Longitude, 7, MidpointRounding.AwayFromZero)
                            };
                        });

                    return new NvdbArcGeometry()
                    {
                        id = id,
                        length = double.Parse(length, CultureInfo.InvariantCulture) / 1000,
                        locations = locations
                    };
                }).ToList();
            //Creates an almost empty arc to be compared against the json data 
            IEnumerable<Arc_DTO> speedArcs = arcGeometry
                .Where(n => arcInfoById[n.id].Any())
                .Select(m =>
                {
                    return new Arc_DTO()
                    {
                        id = m.id,
                        start = m.locations.First(),
                        stop = m.locations.Last(),
                        locations = m.locations,
                        length = m.length,
                        speed = arcInfoById[m.id].First().speed
                    };
                }).ToList();

            var completeArcs = GetFromAndToNodes(nodeInfos, speedArcs);

            var b = completeArcs.Where(p => p.id == "3:495470");
            Console.WriteLine("stsop");



            IEnumerable<Node_DTO> nodes = null;



            var c = speedArcs.Where(p => p.id == "3:495479");
            Console.WriteLine("a");
            nodes = nodeInfos.Select(NI =>
            {
                return new Node_DTO()
                {
                    id = NI.ID,
                    location = NI.location
                };
            });

            Network_DTO network = new Network_DTO()
            {
                id = "XML",
                arcs = speedArcs,
                nodes = nodes,
                restrictions = null
            };
            return network;
        }

        private IEnumerable<Arc_DTO> GetFromAndToNodes(IEnumerable<NvdbNodeInfo> nodeInfos, IEnumerable<Arc_DTO> arcs)
        {
            //To check progress
            int counter = 0;
            foreach (var arc in arcs)
            {


                if (counter % 1000 == 0)
                {
                    Console.WriteLine(counter);
                }
                bool from = false;
                bool to = false;
                foreach (var node in nodeInfos)
                {
                    //if (node.location.lat > 58.5799 && node.location.lat < 58.5801
                    //    && node.location.lon > 16.034 && node.location.lon < 16.0365)
                    //{
                    //    //Console.WriteLine("inne");
                    //}
                    if (from && to)
                    {
                        break;
                    }
                    else if (node.connections.Contains(arc.id))
                    {
                        if (arc.id == "3:495470")
                        {
                            Console.WriteLine("stop");
                        }
                        if (IsClose(node.location, arc.locations.First()))
                        {
                            arc.fromNodeId = node.ID;
                            from = true;
                        }
                        else if (IsClose(node.location, arc.locations.Last()))
                        {
                            arc.toNodeId = node.ID;
                            to = true;
                        }
                    }
                }
                counter++;
            }
            return arcs;
        }

        //Get the object id from the port id by removing substring from "/"
        private string GetUntilOrEmpty(string text)
        {
            string stopAt = "/";
            if (!string.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);
                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }
            return String.Empty;
        }

        private bool IsClose(Location_DTO a, Location_DTO b)
        {
            if (Processing.CalculateDistanceInKilometers(a, b) < 0.0005)
                return true;
            else
                return false;
        }
    }
}

/*
MightyLittleGeodyse library

Copyright (C) 2009 Björn Sållarp

Permission is hereby granted, free of charge, to any person obtaining a copy of this
software and associated documentation files (the "Software"), to deal in the Software 
without restriction, including without limitation the rights to use, copy, modify,
merge, publish, distribute, sublicense, and/or sell copies of the Software, and to 
permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
