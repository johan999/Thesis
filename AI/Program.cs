using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AI
{
    class Program
    {
        public const int RESTRICTIONS = 2;
        private static int choice = 0;
        static void Main(string[] args)
        {
            //Empty variables
            Network_DTO optiplanNetwork = new Network_DTO();
            Network_DTO nvdbNetwork = new Network_DTO();
            IEnumerable<Arc_DTO> roadNetwork = null;
            ILookup<string, NodeConnection> table = null;
            Matching matcher = new Matching();
            Pathfinding pathfinder = new Pathfinding();
            while (choice != 4)
            {
                Display menu = new Display();
                choice = menu.Menu();
                switch (choice)
                {
                    case 1:
                        optiplanNetwork = optiplanNetwork.LoadJson(@"D:\Repos\ThesisData\Data\norrkoping.json");
                        roadNetwork = optiplanNetwork.GetDrivingRoads(optiplanNetwork);
                        nvdbNetwork = nvdbNetwork.LoadJson(@"D:\Repos\ThesisData\Data\norrkoping_xml.json");

                        break;
                    case 2:
                        NVDBProcessing xmlProcessor = new NVDBProcessing();
                        var XMLNetwork = xmlProcessor.CreateNetworkFromXML(@"D:\Repos\ThesisData\Data\Norrkoping_hastighet.xml");

                        var serializedNetwork = JsonConvert.SerializeObject(XMLNetwork);
                        File.WriteAllText(@"D:\Repos\Thesis\AI\Data\norrkoping_xml.json", serializedNetwork);

                        //matchingNetwork = xmlProcessor.FindArc(roadNetwork, listOfArcSpeed).ToList();
                        //var outfilteredNetwork = roadNetwork.Except(matchingNetwork);
                        //var matchingNetwork2 = xmlProcessor.FindSmallArcs(listOfArcSpeed, outfilteredNetwork);                        
                        break;
                    case 3:
                        var a = pathfinder.MatchPaths(optiplanNetwork, nvdbNetwork);
                        List<string> nvdbArcs = new List<string>();
                        List<string> optiplanArcs = new List<string>();

                        foreach (var aa in a)
                        {
                            nvdbArcs.Add(aa.nvdbArcId);
                            foreach (var bb in aa.optiplanArcIds)
                            {
                                if (bb != null)
                                    optiplanArcs.Add(bb);
                            }
                        }

                        var matches = JsonConvert.SerializeObject(a);
                        File.WriteAllText(@"D:\Repos\ThesisData\matches.json", matches);

                        //Matching arcs presented as the nvdb arcs
                        var matching = nvdbNetwork.arcs
                            .Where(i => nvdbArcs.Contains(i.id));
                        var matchingRoads = JsonConvert.SerializeObject(matching);
                        File.WriteAllText(@"D:\Repos\ThesisData\nvdbMatchedRoads.json", matchingRoads);

                        //Matching arcs presented as the optiplan arcs
                        var optiplanMatched = roadNetwork
                            .Where(i => optiplanArcs.Contains(i.id));
                        var optiplanMatchedRoads = JsonConvert.SerializeObject(optiplanMatched);
                        File.WriteAllText(@"D:\Repos\ThesisData\optiplanMatchedRoads.json", optiplanMatchedRoads);

                        //Outfiltered nvdb arcs 
                        var nvdb = nvdbNetwork.arcs
                            .Where(i => !nvdbArcs.Contains(i.id));
                        var nvdbRoads = JsonConvert.SerializeObject(nvdb);
                        File.WriteAllText(@"D:\Repos\ThesisData\nvdbRoads.json", nvdbRoads);

                        //Outfilterd optiplan arcs
                        var optiplan = roadNetwork
                            .Where(i => !optiplanArcs.Contains(i.id));
                        var optiplanRoads = JsonConvert.SerializeObject(optiplan);
                        File.WriteAllText(@"D:\Repos\ThesisData\optiplanRoads.json", optiplanRoads);

                        /*var nvdbOriginal = nvdbNetwork.arcs;
                        var nvdbOriginalRoads = JsonConvert.SerializeObject(nvdbOriginal);
                        File.WriteAllText(@"D:\Repos\ThesisData\nvdbOriginalRoads.json", nvdbOriginalRoads);

                        var optiplanOriginal = roadNetwork;
                        var optiplanOriginalRoads = JsonConvert.SerializeObject(optiplanOriginal);
                        File.WriteAllText(@"D:\Repos\ThesisData\optiplanOriginalRoads.json", optiplanOriginalRoads);*/

                        break;
                    case 4:
                        Console.WriteLine("Bye");
                        break;

                        //case 5:
                        //    if (inputList != null && ANN != null && outputList != null){
                        //        double[][] output = NeuralNetwork.ComputeNetwork(inputList, ANN);
                        //        NeuralNetwork.NetworkComparison(output, outputList);
                        //    }
                        //    if (inputList == null){
                        //        Console.WriteLine("Missing input");
                        //        break;
                        //    }
                        //    if (ANN == null){
                        //        Console.WriteLine("Missing ANN");
                        //        break;
                        //    }
                        //    if (outputList == null){
                        //        Console.WriteLine("Missing output");
                        //        break;
                        //    }
                        //    break;

                        //    var filteredRoad = roadNetwork.Except(matchingNetwork.Distinct()).ToList();
                        //    //var roadList = JsonConvert.SerializeObject(filteredRoad);
                        //    var roadList = JsonConvert.SerializeObject(roadNetwork);
                        //    File.WriteAllText(@"C:\Arc_import\road.json", roadList);
                        //    var filteredSpeed = listOfArcSpeed.ToList();
                        //    var speedList = JsonConvert.SerializeObject(filteredSpeed);
                        //    File.WriteAllText(@"C:\Arc_import\speed.json", speedList);
                        //    var matchList = JsonConvert.SerializeObject(matchingNetwork);
                        //    File.WriteAllText(@"C:\Arc_import\match.json", matchList);
                        //    Console.WriteLine("FR: " + filteredRoad.Count +
                        //        " FS: " + filteredSpeed.Count +
                        //        " M: " + matchingNetwork.Count);
                        //    break;
                }
            }
        }
    }

    class Display
    {
        //Interface stuff
        public int Menu()
        {
            Console.WriteLine("Choose option: \n" +
                "1. Load road data\n" +
                "2. Load XML\n" +
                "3. Match networks\n" +
                "4. Exit");
            return Convert.ToInt32(Console.ReadLine());
        }
    }
}