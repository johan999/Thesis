using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AI
{
    class Processing
    {
        //Calculate the distance of an arc in lat, lon to km
        public static double CalculateDistanceInKilometers(Location_DTO from, Location_DTO to)
        {
            // Havesine formula.
            // http://en.wikipedia.org/wiki/Haversine_formula
            // http://www.movable-type.co.uk/scripts/latlong.html

            var R = 6371; // km

            var slat = from.lat;
            var slon = from.lon;

            var tlat = to.lat;
            var tlon = to.lon;

            var dLat = (tlat - slat) / 180 * Math.PI;// .toRad();
            var dLon = (tlon - slon) / 180 * Math.PI;
            var lat1 = slat / 180 * Math.PI;  // lat1.toRad();
            var lat2 = tlat / 180 * Math.PI;  // lat2.toRad();

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;

            return Math.Round(d, 4, MidpointRounding.AwayFromZero);
        }

        public static double CalculateDistanceInKilometers(IEnumerable<Location_DTO> locations)
        {
            return
                Enumerable.Zip(
                    locations,
                    locations.Skip(1),
                    (from, to) => CalculateDistanceInKilometers(from, to))
                .Sum();
        }

        public static double EvaluateCurves(IEnumerable<Location_DTO> locations)
        {
            //Create a list of triangles from arc locations
            var triangles =
                locations
                .Zip(locations.Skip(1), (p0, p1) => new { p0, p1 })
                .Zip(locations.Skip(2), (i, p2) => new { i.p0, i.p1, p2 });

            List<double[]> arcCurves = new List<double[]>();
            bool curve = false;
            double curveAngle = 0.0;   //Angle of a whole curve
            double curveLength = 0.0;  //Lenght of a whole curve

            int tracker = 1;
            foreach (var t in triangles)
            {
                double hypotenuse = CalculateDistanceInKilometers(t.p2, t.p0);
                double frontDist = CalculateDistanceInKilometers(t.p1, t.p2);
                double backDist = CalculateDistanceInKilometers(t.p1, t.p0);

                //Calculates central angle for a triangle
                double angle = Math.PI;
                if (frontDist + backDist - hypotenuse > 0.001)
                {
                    angle =
                        Math.Acos((Math.Pow(frontDist, 2) + Math.Pow(backDist, 2) - Math.Pow(hypotenuse, 2))
                        / (2.0 * frontDist * backDist));
                    if (!HasValue(angle))
                    {
                        Console.WriteLine("NaN! H: " + hypotenuse + " f: " + frontDist + " b: " + backDist +
                            "\n P0: " + t.p0.lat + " " + t.p0.lon +
                            " P1: " + t.p1.lat + " " + t.p1.lon +
                            " P2: " + t.p2.lat + " " + t.p2.lon);
                        angle = 0.0;
                    }
                    if (angle < 0)
                    {
                        Console.WriteLine("Negative angle! H: " + hypotenuse + " f: " + frontDist
                            + " b: " + backDist + " a: " + angle);
                    }
                }

                //PI - angle, to get the vehicle displacement angle instead of curve angle
                angle = Math.PI - angle;

                bool hasChanged = false;
                //Start of a curve
                if (angle > 0 && curve == false)
                {
                    curve = true;
                }
                //End of a curve
                if (angle == 0 && curve == true)
                {
                    curve = false;
                    hasChanged = true;
                }

                //Summarise the angle and length of all triangles in a curve                
                if (curve)
                {
                    curveAngle += angle;
                    curveLength += backDist;
                }
                if (!curve && hasChanged && tracker != triangles.Count())
                {
                    double[] item = { curveAngle, curveLength };
                    arcCurves.Add(item);
                    curveAngle = 0.0;
                    curveLength = 0.0;
                }
                if (tracker == triangles.Count() && curveAngle != 0.0)
                {
                    double[] item = { curveAngle, curveLength };
                    arcCurves.Add(item);
                }
                tracker++;
            }
            double final = 0.0;
            foreach (double[] c in arcCurves)
            {
                final += c[0] / c[1];
            }
            return final;
        }

        public static bool HasValue(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }

        //Input should return:        
        //Sinuosity
        public static double[][] CreateInputList(Network_DTO network)
        {
            int arcsWithCurves = 0;
            double max = 0;
            double total = 0;


            double[][] inputList = new double[network.arcs.Count()][];
            int listNr = 0;
            foreach (Arc_DTO arc in network.arcs)
            {
                double birdDist =
                    CalculateDistanceInKilometers(
                        arc.locations.First(), arc.locations.Last());
                double totalDist =
                    CalculateDistanceInKilometers(
                        arc.locations);
                double curves = 0.0;
                //If birdDist != totalDist => atleast one curve in the arc
                if (birdDist != totalDist)
                {
                    arcsWithCurves++;
                    curves = EvaluateCurves(arc.locations);
                    if (curves > max)
                        max = curves;
                    total += curves;
                }



                inputList[listNr] = new double[] { curves, 50 };
                listNr++;
            }
            Console.WriteLine("Arcs with curves: " + arcsWithCurves + " Max: " + max +
                " Mean: " + total / network.arcs.Count());
            return inputList;
        }

        //Creates an output list. 
        //For each arc a double[] is created and placed in the list.
        //The array length is the number of possible restrictions
        //If an arc has a restriction, the position in the array corresponding to the same 
        //type number of a restriction - 1, gets the value 1, the rest 0. e.g {0, 1, 0, 0} type 2
        public static double[][] CreateOutputList(Network_DTO network)
        {
            int nrOfRestrictions = network.restrictions.Count();
            double[][] outputList = new double[network.arcs.Count()][];
            int listNr = 0;

            foreach (Arc_DTO arc in network.arcs)
            {
                double[] restrictionOutput = new double[nrOfRestrictions];
                if (nrOfRestrictions != 0)
                {
                    foreach (string restrictionID in arc.arcRestrictionIds)
                    {
                        int restrictionNr = network.restrictions.Where(a => a.id == restrictionID).First().type - 1;
                        restrictionOutput[restrictionNr] = 1.0;
                    }
                    outputList[listNr] = restrictionOutput;
                }
                else
                {
                    outputList[listNr] = restrictionOutput;
                }
                listNr++;
            }
            return outputList;
        }

        public static void LoadXML(string name)
        {
            XDocument doc = XDocument.Load(name);

            //Get node locations
            var arc = doc
                .Elements("GI")
                .Elements("dataset")
                .Elements("NW_RefLink")
                .ToDictionary(a => a.Attribute("uuid").Value.ToString());

            //Get speeds for arcs
            var speeds = doc
                .Elements("GI")
                .Elements("dataset")
                .Elements("FI_ChangedFeatureWithHistory");


            List<Speed_DTO> list = new List<Speed_DTO>();

            foreach (var a in speeds)
            {
                Speed_DTO speedOfLocation = new Speed_DTO();

                //Reference to road geometry
                var locationRef = a.Descendants()
                    .Where(n => n.Name == "locationInstance")
                    .FirstOrDefault().Attribute("uuidref").Value;

                //Speed of the geometry
                string speed = a.Descendants()
                    .Where(n => n.Name == "number")
                    .FirstOrDefault().Value;

                if (locationRef != null && speed != null)
                {
                    speedOfLocation.refLocation = locationRef;
                    speedOfLocation.speed = Convert.ToInt32(speed);
                }

                list.Add(speedOfLocation);
            }

            int match = 0;
            for (int i = 0; i < list.Count(); i++)
            {
                XElement value;
                if (arc.TryGetValue(list[i].refLocation, out value))
                {
                    IEnumerable<XElement> nodes = value.Descendants("controlPoint");
                    Console.WriteLine("\n");
                    foreach (var c in nodes)
                    {
                        Console.WriteLine(c);
                        //I geometrin leta reda på en nod, lägg till och omvandla
                        //koordinaterna, fortsätt för varje nod. 
                        //Eller räcker det med första och sista?
                        //.geometry
                        //.GM_Curve
                        //.segment
                        //.GM_LineString
                        //.controlPoint
                        //.column
                        //An element includes
                        //.direct
                        //.coordinate
                        //.number x 3

                    }

                }
                else
                {
                    Console.WriteLine("No such key: " + list[i].refLocation);
                }
            }
            Console.WriteLine("Match: " + match + " of " + list.Count());
        }
    }
}