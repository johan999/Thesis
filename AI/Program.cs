using Accord.Neuro;
using Accord.Neuro.Learning;
using System;
using System.IO;
using System.Linq;

namespace AI
{
    class Program
    {
        const int RESTRICTIONS = 2;
        static int choice = 0;
        static void Main(string[] args)
        {
            //Empty variables
            Network_DTO roadNetwork = null;
            double[][] inputList = null;
            double[][] outputList = null;
            ActivationNetwork ANN = null;

            while (choice != 6) {
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
                            Convert.ToInt32(Console.ReadLine())-1]);

                        inputList = CreateInputList(roadNetwork);
                        outputList = CreateOutputList(roadNetwork);

                        break;
                    case 2:
                        if (roadNetwork != null)
                        {
                            ANN = CreateANN(inputList, outputList);
                        }
                        else
                        {
                            Console.WriteLine("Did not load or empty network!");
                        }
                        break;
                    case 3:
                        Console.WriteLine("Saved neural network");
                        if (ANN != null)
                        {
                            ANN.Save("ANN/neuralnetwork.json");
                        }
                        else
                        {
                            Console.WriteLine("Neural network is null");
                        }
                        break;
                    case 4:
                        Console.WriteLine("Loaded neural network");
                        ANN = (ActivationNetwork)Network.Load("ANN/neuralnetwork.json");
                        if (ANN == null)
                        {
                            Console.WriteLine("Could not load network");
                        }
                        break;
                    case 5:
                        if (inputList != null && ANN != null && outputList != null)
                        {
                            double[][] output = ComputeNetwork(inputList, ANN);
                            NetworkComparison(output, outputList);
                        }
                        if(inputList == null)
                        {
                            Console.WriteLine("Missing input");
                        }
                        if(ANN == null)
                        {
                            Console.WriteLine("Missing ANN");
                        }
                        if (outputList == null)
                        {
                            Console.WriteLine("Missing output");
                        }

                        break;
                    case 6:
                        Console.WriteLine("Bye...");
                        break;
                    case 7:
                        Console.WriteLine(ANN.Compute(new double[]{2.450})[0]);
                        break;
                }
                Console.WriteLine();
            }
        }

        //Input should return:        
        //Sinuosity
        public static double[][] CreateInputList(Network_DTO network)
        {
            double[][] inputList = new double[network.arcs.Count()][];
            int listNr = 0;
            foreach (Arc_DTO arc in network.arcs)
            {
                double birdDist =
                    Processing.CalculateDistanceInKilometers(
                        arc.locations.First(), arc.locations.Last());
                double totalDist =
                    Processing.CalculateDistanceInKilometers(
                        arc.locations);
                double curves = 0.0;
                //If birdDist != totalDist => atleast one curve in the arc
                if (birdDist != totalDist)
                {
                    curves = Processing.EvaluateCurves(arc.locations);
                }

                inputList[listNr] = new double[] { curves, 50 };
                //Console.WriteLine("input: " + inputList[listNr][0]);
                listNr++;
                
            }
            return inputList;
        }

        //Creates an output list. 
        //For each arc a double[] is created and placed in the list.
        //The array length is the number of possible restrictions
        //If an arc has a restriction, the position in the array corresponding to the same 
        //type number of a restriction - 1, gets the value 1, the rest 0. e.g {0, 1, 0, 0} type 2
        public static double[][] CreateOutputList(Network_DTO network)
        {
            double[][] outputList = new double[network.arcs.Count()][];
            int listNr = 0;

            foreach (Arc_DTO arc in network.arcs)
            {
                double[] restrictionOutput = new double[RESTRICTIONS];
                if (arc.arcRestrictionIds.Any())
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

        public static ActivationNetwork CreateANN(double[][] input, double[][] output)
        {   
            var watch = System.Diagnostics.Stopwatch.StartNew();

            int[] layers = { 4, RESTRICTIONS }; //neurons per layer(lenght), last is output

            // create neural network
            ActivationNetwork network = new ActivationNetwork(
                new SigmoidFunction(),
                2,      //Input
                layers);//Hidden layers + output
   
            // create teacher
            BackPropagationLearning teacher = new BackPropagationLearning(network);

            bool needToStop = false;

            // loop
            int epochs = 0;
            int epochsHundreds = 0;
            double prevError = 999999.9;
            while (!needToStop)
            {
                // run epoch of learning procedure
                double error = teacher.RunEpoch(input, output);
                // check error value to see if we need to stop
                if (Math.Abs(prevError - error) < 0.000001 || epochsHundreds == 100)
                {
                    needToStop = true;
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    Console.WriteLine(2 + ", " + RESTRICTIONS + " Stop at: " + epochsHundreds + "" + epochs
                        + " Error: " + error + " Time: " + elapsedMs / 1000 + "s");
                }
                if (epochs == 100)
                {
                    epochsHundreds++;
                    epochs = 0;
                }
                prevError = error;
                epochs++;
            }
            return network;
        }

        public static double[][] ComputeNetwork(double[][] input, ActivationNetwork ANN)
        {
            double[][] output = new double[input.Length][];
            int i = 0;
            foreach(double[] d in input)
            {
                output[i] = ANN.Compute(d);
                i++;
            }
            return output;
        }

        public static void NetworkComparison(double[][] calculatedOutput, double[][] originalOutput)
        {
            int nrOfOutputs = calculatedOutput.Length;
            if (nrOfOutputs != originalOutput.Length)
            {
                Console.WriteLine("Size doesn't match!");
            }
            else
            {
                double error = 0;
                int correct = 0;
                for (int i = 0; i < nrOfOutputs; i++)
                {
                    int correctRes = 0;
                    for (int j = 0; j < RESTRICTIONS - 1; j++)
                    {
                        double resError = Math.Abs(calculatedOutput[i][j] - originalOutput[i][j]);
                        if(resError < 0.9)
                        {
                            correctRes++;
                        }
                        error += resError;
                    }
                    if(correctRes == RESTRICTIONS)
                    {
                        correct++;
                    }
                }
                error = error / nrOfOutputs;
                Console.WriteLine("Nr of correct arcs: " + correct + " of " + nrOfOutputs + 
                    " Error of network: " + error);
            }
        }

        //Interface stuff
        public static string[] dataSets = { "Norrkoping", "Linkoping", "Nykoping" };
        public static int DisplayData()
        {
            Console.WriteLine("Choose option: \n" +
                "1. Load road data\n" +
                "2. Train ANN\n" +
                "3. Save ANN\n" +
                "4. Load ANN\n" +
                "5. Evaluate network\n" +
                "6. Exit");
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