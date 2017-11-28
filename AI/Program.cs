using Accord.Neuro;
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
            Network_DTO allNetworks = new Network_DTO();
            double[][] inputList = null;
            double[][] outputList = null;
            ActivationNetwork ANN = null;
            IEnumerable<Arc_DTO> roadNetwork = null;
            IEnumerable<Arc_DTO> listOfArcSpeed = null;
            List<Arc_DTO> matchingNetwork = null;


            while (choice != 7)
            {
                Display menu = new Display();
                choice = menu.Menu();
                switch (choice)
                {
                    case 1:
                        string[] files = Directory.GetFiles("Data/");
                        int count = 1;
                        Console.WriteLine("Choose file: ");
                        foreach (string s in files)
                        {
                            Console.WriteLine(count + ". " + s);
                            count++;
                        }

                        allNetworks = allNetworks.LoadJson(files[
                            Convert.ToInt32(Console.ReadLine()) - 1]);

                        roadNetwork = allNetworks.GetDrivingRoads(allNetworks);

                        //inputList = Processing.CreateInputList(allNetworks);
                        //1outputList = Processing.CreateOutputList(allNetworks);

                        Console.WriteLine(roadNetwork.Where(n => n.arcRestrictionIds.Any()).Count());
                        break;
                    case 2:
                        if (allNetworks == null)
                        {
                            Console.WriteLine("Did not load or empty network");
                            break;
                        }
                        Console.WriteLine("Learning rate?[0,0 - 1,0]");
                        double learningRate = Convert.ToDouble(Console.ReadLine());

                        ANN = NeuralNetwork.CreateANN(inputList, outputList, learningRate);
                        break;
                    case 3:
                        if (ANN == null)
                        {
                            Console.WriteLine("Could not save: Neural network is null");
                            break;
                        }
                        Console.WriteLine("Give name to the neural network:");
                        string name = Console.ReadLine();
                        ANN.Save("ANN/" + name + ".json");
                        Console.WriteLine("Saved neural network");
                        break;
                    case 4:
                        string[] AIs = Directory.GetFiles("ANN/");
                        count = 1;
                        Console.WriteLine("Choose file: ");
                        foreach (string s in AIs)
                        {
                            Console.WriteLine(count + ". " + s);
                            count++;
                        }
                        ANN = (ActivationNetwork)Network.Load(AIs[Convert.ToInt32(Console.ReadLine()) - 1]);
                        if (ANN == null)
                        {
                            Console.WriteLine("Could not load network");
                            break;
                        }
                        Console.WriteLine("Loaded neural network");
                        break;
                    case 5:
                        if (inputList != null && ANN != null && outputList != null)
                        {
                            double[][] output = NeuralNetwork.ComputeNetwork(inputList, ANN);
                            NeuralNetwork.NetworkComparison(output, outputList);
                        }
                        if (inputList == null)
                        {
                            Console.WriteLine("Missing input");
                            break;
                        }
                        if (ANN == null)
                        {
                            Console.WriteLine("Missing ANN");
                            break;
                        }
                        if (outputList == null)
                        {
                            Console.WriteLine("Missing output");
                            break;
                        }
                        break;
                    case 6:
                        Console.WriteLine("Add data from XML");
                        NVDBProcessing xmlProcessor = new NVDBProcessing();

                        listOfArcSpeed = xmlProcessor.LoadXML("Data/Norrkoping_hastighet.xml").ToList();

                        matchingNetwork = xmlProcessor.FindArc(listOfArcSpeed, roadNetwork).ToList();

                        //var outfilteredNetwork = roadNetwork.Except(matchingNetwork);
                        //var matchingNetwork2 = xmlProcessor.FindSmallArcs(listOfArcSpeed, outfilteredNetwork);

                        Console.WriteLine("Geometry: " + roadNetwork.Count() +
                            " Speeds: " + listOfArcSpeed.Count() +
                            " Matches: " + matchingNetwork.Count());
                        //Console.WriteLine(distinctNetwork.Where(n => n.arcRestrictionIds.Any()).Count());
                        break;
                    case 7:
                        Console.WriteLine("Bye...");
                        break;
                    case 8: //Secret

                        var filteredRoad = roadNetwork.Except(matchingNetwork.Distinct()).ToList();
                        var roadList = JsonConvert.SerializeObject(filteredRoad);
                        File.WriteAllText(@"C:\Arc_import\road.json", roadList);

                        var filteredSpeed = listOfArcSpeed.ToList();
                        var speedList = JsonConvert.SerializeObject(filteredSpeed);
                        File.WriteAllText(@"C:\Arc_import\speed.json", speedList);

                        var matchList = JsonConvert.SerializeObject(matchingNetwork);
                        File.WriteAllText(@"C:\Arc_import\match.json", matchList);

                        Console.WriteLine("FR: " + filteredRoad.Count +
                            " FS: " + filteredSpeed.Count +
                            " M: " + matchingNetwork.Count);
                        break;
                }
            }
        }

        /*public static void TestCurve()
        {
            Console.WriteLine("Test: ");
            string jsonLocation = System.IO.File.ReadAllText(Path.Combine("Data/", "test.json"));

            Test_Location_DTO test = 
                JsonConvert.DeserializeObject<Test_Location_DTO>(jsonLocation);
            var t = Processing.EvaluateCurves(test.locations);
            Console.WriteLine("Locations.count: " + test.locations.Count());
            foreach (double[] d in t)
            {
                Console.WriteLine("Angle: " + d[0] + ", Length: " + d[1]);
            }
            
        } */
    }
    class Display
    {
        //Interface stuff
        public int Menu()
        {
            Console.WriteLine("Choose option: \n" +
                "1. Load road data\n" +
                "2. Train ANN\n" +
                "3. Save ANN\n" +
                "4. Load ANN\n" +
                "5. Evaluate network\n" +
                "6. Load XML\n" +
                "7. Exit");
            return Convert.ToInt32(Console.ReadLine());
        }
    }
}