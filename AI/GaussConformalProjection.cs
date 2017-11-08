using System;

namespace AI
{

    public class Point
    {
        public Point()
        {
            X = 0;
            Y = 0;
        }


        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }
        public double Y { get; set; }
    }

    public class GaussConformalProjection
    {
        public const double SemiMajorAxis = 6378137;
        /// <summary>
        /// Flattening of the ellipsoid
        /// </summary>
        public const double Flattening = 298.257222101;

        /// <summary>
        /// Longitude of the central meridian
        /// </summary>
        public const double CentralMeridian = 16.5;

        /// <summary>
        /// Scale factor along the central meridian
        /// </summary>
        #region Scale
        public double Scale
        {
            get
            {
                return _Scale;
            }
            set
            {
                _Scale = value;
            }
        }
        private double _Scale = 1d;
        #endregion


        public const double FalseNorthing = 0;
        public const double FalseEasting = 150000;

        // Converts a point in local projection koordinates to a longitude/lattitude point 
        public Point InverseProject(Point p)
        {
            var x = p.Y;
            var y = p.X;

            var a = SemiMajorAxis;
            var f = Flattening;
            // e2: First Eccentricity, squared
            var e2 = f * (2d - f);
            var e4 = e2 * e2;
            var n = f / (2d - f);
            var n2 = n * n;
            var n3 = n * n2;
            var ah = (a / (1d + n)) * (1 + 0.25 * n2 + (1d / 64d) * n2 * n2); // + ...

            var E = (x - FalseNorthing) / (Scale * ah);
            var nj = (y - FalseEasting) / (Scale * ah);

            var d1 = 0.5 * n - (2d / 3d) * n2 + (37d / 96d) * n3 - (1d / 360d) * n2 * n2; // + ...
            var d2 = (1d / 48d) * n2 + (1d / 15d) * n3 - (437d / 1440d) * n2 * n2; // + ...
            var d3 = (17d / 480d) * n3 - (37d / 840d) * n2 * n2;
            var d4 = (4397d / 161280d) * n2 * n2;

            var Ep = E 
                - d1 * Math.Sin(2 * E) * Math.Cosh(2 * nj) 
                - d2 * Math.Sin(4 * E) * Math.Cosh(4 * nj) 
                - d3 * Math.Sin(6 * E) * Math.Cosh(6 * nj) 
                - d4 * Math.Sin(8 * E) * Math.Cosh(8 * nj);

            var njp = nj
                - d1 * Math.Cos(2 * E) * Math.Sinh(2 * nj)
                - d2 * Math.Cos(4 * E) * Math.Sinh(4 * nj)
                - d3 * Math.Cos(6 * E) * Math.Sinh(6 * nj)
                - d4 * Math.Cos(8 * E) * Math.Sinh(8 * nj);

            var latC = Math.Asin(Math.Sin(Ep) / Math.Cosh(njp));
            var lonDiff = Math.Atan(Math.Sinh(njp) / Math.Cos(Ep));

            var A = e2 + e4 + e2 * e4 + e4 * e4; // + ...
            var B = -(1d / 6d) * (7d * e4 + 17d * e4 * e2 + 30d * e4 * e4); // + ...
            var C = (1d / 120d) * (224d * e2 * e4 + 889d * e4 * e4); // + ...
            var D = -(1d / 1260d) * (4279d * e4 * e4); // + ...

            var lon = CentralMeridian + lonDiff;
            var sin_latC = Math.Sin(latC);
            var sin_latC2 = sin_latC*sin_latC;
            var lat = latC + sin_latC * Math.Cos(latC) * (A + B * sin_latC2 + C * sin_latC2 * sin_latC2 + D * sin_latC2 * sin_latC2 * sin_latC2);

            var xkoord = Math.Round(180d * lon / Math.PI, 7);
            var ykoord = Math.Round(180d * lat / Math.PI, 7);

            // check if inital coordinate is 0
            if (p.X == 0 )
            {
                xkoord = 0;
            }
            // check if inital coordinate is 0
            if (p.Y == 0)
            {
                ykoord = 0;
            }

            return new Point(xkoord, ykoord);
        }

        // Converts a longitude/lattitude point to a point in local projection koordinates
        public Point Project(Point p)
        {
            // check if 0
            if ( p.X == 0 || p.Y == 0 )
            {
                return p;
            }

            var lon = p.X * Math.PI / 180d;
            var lat = p.Y * Math.PI / 180d;
            var sinLat = Math.Sin(lat);
            var sinLat2 = sinLat * sinLat;

            var a = SemiMajorAxis;
            var f = Flattening;
            // e2: First Eccentricity, squared
            var e2 = f * (2d - f);
            var e4 = e2 * e2;
            var n = f / (2d - f);
            var n2 = n * n;
            var ah = (a / (1d + n)) * (1 + 0.25 * n2 + (1d / 64d) * n2 * n2); // + ...

            var A = e2;
            var B = (1d / 6d) * (5d * e4 - e4 * e2);
            var C = (1d / 120d) * (104d * e4 * e2 - 45d * e4 * e4); // + ...
            var D = (1d / 1260d) * (1237d * e4 * e4); // + ...
            // latC: Conformal Latitude
            var latC = lat - sinLat * Math.Cos(lat) * (A + sinLat2 * (B + sinLat2 * (C + sinLat2 * D))); // + ...

            var dLon = lon - CentralMeridian;
            var E = Math.Atan2(Math.Tan(latC), Math.Cos(dLon));
            // Calculate nj = arctanh(cos(latC)*sin(dLon)):
            var njx = Math.Cos(latC) * Math.Sin(dLon);
            var nj = 0.5 * Math.Log((1d + njx) / (1d - njx));

            var B1 = 0.5 * n - (2d / 3d) * n2 + (5d / 16d) * n2 * n + (41d / 180d) * n2 * n2; // + ...
            var B2 = (13d / 48d) * n2 - (3d / 5d) * n2 * n + (557d / 1440d) * n2 * n2; // + ...
            var B3 = (61d / 240d) * n2 * n - (103d / 140d) * n2 * n2; // + ...
            var B4 = (49561d / 161280d) * n2 * n2; // + ...

            var k0 = Scale;
            var x = k0 * ah * (nj +
                B1 * Math.Cos(2d * E) * Math.Sinh(2d * nj) +
                B2 * Math.Cos(4d * E) * Math.Sinh(4d * nj) +
                B3 * Math.Cos(6d * E) * Math.Sinh(6d * nj) +
                B4 * Math.Cos(8d * E) * Math.Sinh(8d * nj)) + FalseEasting;
            var y = k0 * ah * (E +
                B1 * Math.Sin(2d * E) * Math.Cosh(2d * nj) +
                B2 * Math.Sin(4d * E) * Math.Cosh(4d * nj) +
                B3 * Math.Sin(6d * E) * Math.Cosh(6d * nj) +
                B4 * Math.Sin(8d * E) * Math.Cosh(8d * nj)) + FalseNorthing;

            var xkoord = Math.Round(x, 2);
            var ykoord = Math.Round(y, 2);

            // check if inital coordinate is 0
            if (p.X == 0)
            {
                xkoord = 0;
            }
            // check if inital coordinate is 0
            if (p.Y == 0)
            {
                ykoord = 0;
            }

            return new Point(xkoord, ykoord);
        }
    }
}
