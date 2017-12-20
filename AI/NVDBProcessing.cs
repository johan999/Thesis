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
            public IEnumerable<string> refNodeParts;
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
                                lat = wgsPos.Latitude,
                                lon = wgsPos.Longitude
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
                        refNodeParts = refNodes,
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
                                lat = wgsPos.Latitude,
                                lon = wgsPos.Longitude
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
                });

            IEnumerable<Node_DTO> nodes = null;
            nodes = nodeInfos.Select(NI =>
            {
                return new Node_DTO()
                {
                    id = NI.ID,
                    location = NI.location
                };
            });

            Network_DTO network = new Network_DTO();
            network.id = "XML";
            network.arcs = speedArcs;
            network.nodes = nodes;
            network.restrictions = null;

            return network;
        }

        private IEnumerable<Arc_DTO> GetFromAndToNodes(IEnumerable<NvdbNodeInfo> nodeInfos, Arc_DTO arc)
        {
            foreach (var n in nodeInfos)
            {
                if (n.refNodeParts.Contains(arc.id))
                {
                    if (n.IsClose(arc.locations.First()))
                        arc.fromNodeId = n.ID;
                    else if (n.IsClose(arc.locations.Last()))
                        arc.toNodeId = n.ID;
                    else
                        return null;
                }
            }
            return null;
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

        //public List<Arc_DTO> FindArc(IEnumerable<Arc_DTO> a, IEnumerable<Arc_DTO> b)
        //{
        //    List<Arc_DTO> matchList = new List<Arc_DTO>();
        //    foreach (var aa in a)
        //    {
        //        Arc_DTO closest = null;
        //        foreach (var bb in b)
        //        {
        //            closest = FindArc(aa, bb, closest);
        //        }
        //        if (closest != null)
        //        {
        //            matchList.Add(closest);
        //        }
        //    }
        //    return matchList;
        //}

        //public Arc_DTO FindArc(Arc_DTO a, Arc_DTO b, Arc_DTO closest)
        //{
        //    var startDist = Processing.CalculateDistanceInKilometers(a.start, b.start);
        //    var stopDist = Processing.CalculateDistanceInKilometers(a.stop, b.stop);
        //    var flipStartDist = Processing.CalculateDistanceInKilometers(a.start, b.stop);
        //    var flipStopDist = Processing.CalculateDistanceInKilometers(a.stop, b.start);

        //    double start, stop;

        //    if (startDist < flipStartDist)
        //    {
        //        start = startDist;
        //        stop = stopDist;
        //    }
        //    else
        //    {
        //        start = flipStartDist;
        //        stop = flipStopDist;
        //    }

        //    if (start < THRESHOLD && stop < THRESHOLD && closest == null)
        //    {
        //        closest = b;
        //    }
        //    else if (start < THRESHOLD && stop < THRESHOLD && closest != null)
        //    {
        //        var closestStartDist = Processing.CalculateDistanceInKilometers(a.start, closest.start);
        //        var closestStopDist = Processing.CalculateDistanceInKilometers(a.stop, closest.stop);
        //        var flipClosestStartDist = Processing.CalculateDistanceInKilometers(a.start, closest.stop);
        //        var flipClosestStopDist = Processing.CalculateDistanceInKilometers(a.stop, closest.start);

        //        double closestStart, closestStop;

        //        if (closestStartDist < flipClosestStartDist)
        //        {
        //            closestStart = closestStartDist;
        //            closestStop = closestStopDist;
        //        }
        //        else
        //        {
        //            closestStart = flipClosestStartDist;
        //            closestStop = flipClosestStopDist;
        //        }

        //        if (start < closestStart && stop < closestStop)
        //            closest = b;
        //    }
        //    return closest;
        //}


        ////Calculates the distance between two locations inside a threshold
        //public double? IsClose(Location_DTO a, Location_DTO b)
        //{
        //    double? dist = Processing.CalculateDistanceInKilometers(a, b);
        //    return dist < THRESHOLD ? dist : null;
        //}

        ////Find the closest node in an arc to the reference node inside a threshold
        //public Location_DTO FindClosestNodeInArc(IEnumerable<Location_DTO> locationList, Location_DTO refLocation)
        //{
        //    Location_DTO closest = locationList.First();
        //    double? closestDist = IsClose(closest, refLocation);

        //    foreach (var node in locationList.Skip(1))
        //    {
        //        double? dist = IsClose(node, refLocation);
        //        if (dist != null && dist < closestDist)
        //        {
        //            closest = node;
        //            closestDist = dist;
        //        }
        //    }
        //    return closestDist != null ? closest : null;
        //}

        //public Arc_DTO FindConnectingArc(IEnumerable<Arc_DTO> a, Arc_DTO b)
        //{
        //    Arc_DTO foundArc = null;
        //    foreach (var foo in a)
        //    {
        //        var closestStart = FindClosestNodeInArc(foo.locations, b.start);
        //        var closestStop = FindClosestNodeInArc(foo.locations, b.stop);
        //        if (closestStart != null && closestStop != null)
        //        {
        //            if (foundArc == null)
        //            {
        //                foundArc.id = foo.id;
        //                foundArc.start = closestStart;
        //                foundArc.stop = closestStop;
        //            }
        //            else if (IsClose(closestStart, b.start) < IsClose(foundArc.start, b.start) &&
        //                    IsClose(closestStop, b.stop) < IsClose(foundArc.stop, b.stop))
        //            {
        //                foundArc.id = foo.id;
        //                foundArc.start = closestStart;
        //                foundArc.stop = closestStop;
        //            }
        //        }
        //    }
        //    return foundArc;
        //}
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
