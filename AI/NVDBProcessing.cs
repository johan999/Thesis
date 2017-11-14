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

        public static void LoadXML(string name)
        {
            XDocument doc = XDocument.Load(name);

            var geometryRefs = doc
                .Descendants("NW_LineExtent")
                .Descendants("locationInstance")
                .Select(n => new { id = n.Attribute("uuidref").Value });

            var distinctRefs = geometryRefs.Distinct().ToDictionary(p => p.id);

            var geometryIds = doc
                .Descendants("NW_RefLink")
                .Select(n => n.Attribute("uuid").Value);

            int match = 0;
            foreach (var i in geometryIds)
            {
                var temp = distinctRefs.GetValueOrDefault(i);
                if (temp != null)
                {
                    match++;
                }
            }

            Console.WriteLine("Geometry references: " + geometryRefs.Count() +
                "\nDistinct references: " + distinctRefs.Count() +
                "\nGeometry Ids: " + geometryIds.Count() +
                "\nmatches: " + match);

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
                            FI.Descendants("number")
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

                    var locations =
                         NW
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
                        locations = locations
                    };
                }).ToList();

            Console.WriteLine("stop");
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
