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
            while (choice != 3)
            {
                Display menu = new Display();
                choice = menu.Menu();
                switch (choice)
                {
                    case 1:
                        //string[] files = Directory.GetFiles("Data/");
                        //int count = 1;
                        //Console.WriteLine("Choose file: ");
                        //foreach (string s in files){
                        //    Console.WriteLine(count + ". " + s);
                        //    count++;
                        //}
                        //allNetworks = allNetworks.LoadJson(files[
                        //    Convert.ToInt32(Console.ReadLine()) - 1]);
                        optiplanNetwork = optiplanNetwork.LoadJson("Data/norrkoping.json");
                        roadNetwork = optiplanNetwork.GetDrivingRoads(optiplanNetwork);
                        nvdbNetwork = nvdbNetwork.LoadJson("Data/norrkoping_xml.json");

                        break;
                    case 2:
                        NVDBProcessing xmlProcessor = new NVDBProcessing();
                        var XMLNetwork = xmlProcessor.CreateNetworkFromXML("Data/Norrkoping_hastighet.xml");
                        var serializedNetwork = JsonConvert.SerializeObject(XMLNetwork);
                        File.WriteAllText(@"C:\Users\Johan Eliasson\source\repos\AI\AI\Data\norrkoping_xml.json", serializedNetwork);

                        //matchingNetwork = xmlProcessor.FindArc(roadNetwork, listOfArcSpeed).ToList();
                        //var outfilteredNetwork = roadNetwork.Except(matchingNetwork);
                        //var matchingNetwork2 = xmlProcessor.FindSmallArcs(listOfArcSpeed, outfilteredNetwork);                        
                        break;
                    case 3:

                        var a = pathfinder.MatchPaths(optiplanNetwork, nvdbNetwork);
                        break;
                    case 4:
                        Console.WriteLine("Bye");
                        break;
                        //case 2:
                        //    if (allNetworks == null){
                        //        Console.WriteLine("Did not load or empty network");
                        //        break;
                        //    }
                        //    Console.WriteLine("Learning rate?[0,0 - 1,0]");
                        //    double learningRate = Convert.ToDouble(Console.ReadLine());
                        //    ANN = NeuralNetwork.CreateANN(inputList, outputList, learningRate);
                        //    break;
                        //case 3:
                        //    if (ANN == null){
                        //        Console.WriteLine("Could not save: Neural network is null");
                        //        break;
                        //    }
                        //    Console.WriteLine("Give name to the neural network:");
                        //    string name = Console.ReadLine();
                        //    ANN.Save("ANN/" + name + ".json");
                        //    Console.WriteLine("Saved neural network");
                        //    break;
                        //case 4:
                        //    string[] AIs = Directory.GetFiles("ANN/");
                        //    count = 1;
                        //    Console.WriteLine("Choose file: ");
                        //    foreach (string s in AIs){
                        //        Console.WriteLine(count + ". " + s);
                        //        count++;
                        //    }
                        //    ANN = (ActivationNetwork)Network.Load(AIs[Convert.ToInt32(Console.ReadLine()) - 1]);
                        //    if (ANN == null){
                        //        Console.WriteLine("Could not load network");
                        //        break;
                        //    }
                        //    Console.WriteLine("Loaded neural network");
                        //    break;
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
                "3. Exit");
            return Convert.ToInt32(Console.ReadLine());
        }
    }
}