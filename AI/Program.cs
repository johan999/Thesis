using Accord.Neuro;
using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MightyLittleGeodesy.Positions;

namespace AI
{
    class Program
    {
        public const int RESTRICTIONS = 2;
        private static int choice = 0;
        static void Main(string[] args)
        {
            //Empty variables
            Network_DTO roadNetwork = null;
            double[][] inputList = null;
            double[][] outputList = null;
            ActivationNetwork ANN = null;

            while (choice != 7)
            {
                choice = DisplayData();
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

                        roadNetwork = Network_DTO.LoadJson(files[
                            Convert.ToInt32(Console.ReadLine()) - 1]);

                        inputList = Processing.CreateInputList(roadNetwork);
                        outputList = Processing.CreateOutputList(roadNetwork);

                        break;
                    case 2:
                        if (roadNetwork == null)
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
                        Console.WriteLine("Load XML");
                        //Processing.LoadXML("Data/Nkpg_hastighet_RT90_00.xml");
                        NVDBProcessing.LoadXML("Data/Norrkoping_hastighet.xml");

                        break;
                    case 7:
                        Console.WriteLine("Bye...");
                        break;
                    case 8: //Secret
                        SWEREF99Position swePos = new SWEREF99Position(6496020, 570037);
                        WGS84Position wgsPos = swePos.ToWGS84();

                        Console.WriteLine(wgsPos.Latitude);
                        break;
                }
            }
        }

        //Interface stuff
        public string[] dataSets = { "Norrkoping", "Linkoping", "Nykoping" };
        public static int DisplayData()
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
}