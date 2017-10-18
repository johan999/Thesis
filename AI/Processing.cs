using System;
using System.Collections.Generic;
using System.Linq;

namespace AI
{
    class Processing
    {
        const double BIGCURVE = 0.7854;     //45 degrees 
        const double SMALLCURVE = 0.0873;   //5 degrees
        const double REACTIONTIME = 1.0;    //1 second
        const double FRICTION = 0.8;        //Dry asphalt 0.8, smooth ice 0.1

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

        //Place before calling the method
        //if(totalDistance == birdDistance) => no curve
        public static double[] EvaluateCurves(IEnumerable<Location_DTO> locations)
        {
            //curveSum[0] sums large curves, curveSum [1] sums smaller curves
            double[] curveSum = new double[2];
            
            //Create a list of triangles from arc locations
            var triangles =
                locations
                .Zip(locations.Skip(1), (p0, p1) => new { p0, p1 })
                .Zip(locations.Skip(2), (i, p2) => new { i.p0, i.p1, p2 });

            double A = 0;
            foreach (var t in triangles)
            {
                double a = CalculateDistanceInKilometers(t.p2, t.p0);
                double b = CalculateDistanceInKilometers(t.p1, t.p2);
                double c = CalculateDistanceInKilometers(t.p1, t.p0);

                //Calculates central angle for a triangle
                A = Math.Acos((Math.Pow(b, 2) + Math.Pow(c, 2) - Math.Pow(a, 2)) / (2.0 * b * c));
                
                //PI - angle to the the vehicle displacement angle instead of curve angle
                A = Math.PI - A;

                //The curve should be scaled against the length between the 3 nodes for each 
                //triangle and also for the speed of the arc
                //double evalA = A* StoppingDistance(speed) / totDist;

                if (A > BIGCURVE)
                    curveSum[0] += A;
                else if (BIGCURVE > A && A > SMALLCURVE)
                    curveSum[1] += A;
            }        
            return curveSum;
        }

        public static double RadToGrad(double rad)
        {
            return rad / Math.PI * 180.0;
        }

        public static double StoppingDistance(int speed)
        {
            double rDist = (speed * REACTIONTIME) / 3.6;            //Reaction distance
            double bDist = Math.Pow(speed, 2) / (250 * FRICTION);   //Brake distance
            return rDist + bDist;
        }
    }
}