using MightyLittleGeodesy.Positions;
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
            public IEnumerable<Location_DTO> locations;
        }

        public IEnumerable<Arc_DTO> LoadXML(string name)
        {
            XDocument doc = XDocument.Load(name);

            var arcInfos = doc
                .Descendants("FI_ChangedFeatureWithHistory")
                .Select(FI =>
                {
                    var id =
                        FI
                        .Descendants("NW_LineExtent")
                        .First()
                        .Descendants("locationInstance")
                        .First()
                        .Attribute("uuidref")
                        .Value;

                    var speed =
                        int.Parse(
                            FI
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
                .Select(NW =>
                {
                    var id =
                        NW
                        .Attribute("uuid")
                        .Value;

                    var length =
                        NW
                        .Descendants("length")
                        .First()
                        .Value;

                    var locations =
                         NW
                        .Descendants("coordinate")
                        .Select(nr =>
                        {
                            var numbers = nr.Elements("Number").ToList();

                            double X = double.Parse(numbers[0].Value, CultureInfo.InvariantCulture);
                            double Y = double.Parse(numbers[1].Value, CultureInfo.InvariantCulture);

                            //GaussConformalProjection.

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
                        start = m.locations.First(),
                        stop = m.locations.Last(),
                        locations = m.locations,
                        speed = arcInfoById[m.id].First().speed
                    };
                });

            return speedArcs;
        }

        public List<Arc_DTO> FindArc(IEnumerable<Arc_DTO> a, IEnumerable<Arc_DTO> b)
        {
            List<Arc_DTO> matchList = new List<Arc_DTO>();
            foreach (var aa in a)
            {
                Arc_DTO closest = null;
                foreach (var bb in b)
                {
                    closest = FindArc(aa, bb, closest);
                }
                if (closest != null)
                {
                    matchList.Add(closest);
                }
            }
            return matchList;
        }

        public Arc_DTO FindArc(Arc_DTO a, Arc_DTO b, Arc_DTO closest)
        {
            var startDist = Processing.CalculateDistanceInKilometers(a.start, b.start);
            var stopDist = Processing.CalculateDistanceInKilometers(a.stop, b.stop);
            var flipStartDist = Processing.CalculateDistanceInKilometers(a.start, b.stop);
            var flipStopDist = Processing.CalculateDistanceInKilometers(a.stop, b.start);

            double start, stop;

            if (startDist < flipStartDist)
            {
                start = startDist;
                stop = stopDist;
            }
            else
            {
                start = flipStartDist;
                stop = flipStopDist;
            }

            if (start < THRESHOLD && stop < THRESHOLD && closest == null)
            {
                closest = b;
            }
            else if (start < THRESHOLD && stop < THRESHOLD && closest != null)
            {
                var closestStartDist = Processing.CalculateDistanceInKilometers(a.start, closest.start);
                var closestStopDist = Processing.CalculateDistanceInKilometers(a.stop, closest.stop);
                var flipClosestStartDist = Processing.CalculateDistanceInKilometers(a.start, closest.stop);
                var flipClosestStopDist = Processing.CalculateDistanceInKilometers(a.stop, closest.start);

                double closestStart, closestStop;

                if (closestStartDist < flipClosestStartDist)
                {
                    closestStart = closestStartDist;
                    closestStop = closestStopDist;
                }
                else
                {
                    closestStart = flipClosestStartDist;
                    closestStop = flipClosestStopDist;
                }

                if (start < closestStart && stop < closestStop)
                    closest = b;
            }
            return closest;
        }


        //public IEnumerable<Arc_DTO> FindSmallArcs(IEnumerable<Arc_DTO> a, IEnumerable<Arc_DTO> b)
        //{
        //    List<Arc_DTO> matchList = new List<Arc_DTO>();
        //    foreach (var aa in a)
        //    {
        //        Arc_DTO closest = null;
        //        foreach (var bb in b)
        //        {
        //            closest = FindSmallArc(aa, bb, closest);
        //        }
        //        if (closest != null)
        //        {
        //            matchList.Add(closest);
        //        }
        //    }
        //    return matchList;
        //}

        //public Arc_DTO FindSmallArc(Arc_DTO a, Arc_DTO b, Arc_DTO closest)
        //{
        //    var fluff = b.locations.ToArray();
        //    Arc_DTO temp = null;
        //    foreach( var t in foo)
        //    {
        //        if()

        //    }

        //    return closest;
        //}

        //Return the distance between two locations within a specified threshold, else return -1


        public double IsClose(Location_DTO a, Location_DTO b)
        {
            var dist = Processing.CalculateDistanceInKilometers(a, b);
            return dist < THRESHOLD ? dist : -1;
        }

        public Location_DTO FindClosestNodeInArc(IEnumerable<Location_DTO> locationList, Location_DTO refLocation)
        {
            Location_DTO closest = locationList.First();
            double closestDist = IsClose(closest, refLocation);
            foreach (var node in locationList.Skip(1))
            {
                double dist = IsClose(node, refLocation);
                if (dist != -1 && dist < closestDist)
                {
                    closest = node;
                    closestDist = dist;
                }
            }
            return closestDist != -1 ? closest : null;
        }

        public Arc_DTO FindConnectingArc(IEnumerable<Arc_DTO> a, Arc_DTO b)
        {
            Arc_DTO foundArc = null;
            foreach (var foo in a)
            {
                var closestStart = FindClosestNodeInArc(foo.locations, b.start);
                var closestStop = FindClosestNodeInArc(foo.locations, b.stop);
                if (closestStart != null && closestStop != null)
                {
                    if (foundArc == null)
                    {
                        foundArc.id = foo.id;
                        foundArc.start = closestStart;
                        foundArc.stop = closestStop;
                    }
                    else if (IsClose(closestStart, b.start) < IsClose(foundArc.start, b.start) &&
                            IsClose(closestStop, b.stop) < IsClose(foundArc.stop, b.stop))
                    {
                        foundArc.id = foo.id;
                        foundArc.start = closestStart;
                        foundArc.stop = closestStop;
                    }
                }
            }
            return foundArc;
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
