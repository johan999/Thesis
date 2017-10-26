using System;
using System.Collections.Generic;
using System.Linq;

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
                        Console.WriteLine("NaN! H: " + hypotenuse + " f: " + frontDist
                            + " b: " + backDist + " a: " + angle);
                        angle = 0.0;
                    }
                    if(angle < 0)
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
            foreach(double[] c in arcCurves)
            {
                final += c[0] / c[1];
            }
            return final;
        }

        public static bool HasValue(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }
    }
}