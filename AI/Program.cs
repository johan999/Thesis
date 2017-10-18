using AForge.Neuro;
using AForge.Neuro.Learning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AI
{
    class Program
    {

        //ta reda på hur många!!!!
        const int RESTRICTIONS = 8;

        //Input for the ANN
        public class Input
        {
            public int speed { get; set; } //km/t
            public int walk { get; set; }  //1: Unknown, Walking, Driving 
            public int trafficFlow { get; set; } // vehicles/day
            public int light { get; set; } //1: Good, 2: Bad, 3: None
            public double visibility { get; set; } //Straight road - Sharp curves, 0.0 - 1.0

            public Input(int speed, int walk, int trafficFlow, int light, double visibility)
            {
                this.speed = speed;
                this.walk = walk;
                this.trafficFlow = trafficFlow;
                this.light = light;
                this.visibility = visibility;
            }
        }

        //Output for the ANN
        public class Output
        {
            public int[] restrictions { get; set; } //All restrictions numbers for a network

            public Output(int[] restrictions)
            {
                this.restrictions = restrictions;
            }
        }


        public static int nrOfArcRestrictions = 0;
        static void Main(string[] args)
        {
            //Just for later generalization
            DisplayData();
            //string municipality = Console.ReadLine();
             
            string jsonName = "norrkoping";
            Network_DTO roadNetwork = Network_DTO.LoadJson(jsonName + ".json");
            //CreateInputList(roadNetwork);
            double[][] outputList = CreateOutputList(roadNetwork);
            //CreateANN();
            
            Console.ReadLine();
        }

        //Input should return:        
        //Speed, Traffic flow, Visibility, Light, Pavement
        public static double[][] CreateInputList(Network_DTO net)
        {
            double[][] tempList = null;
            int nr = 0;
            foreach (Arc_DTO arc in net.arcs)
            {         
                
                double birdDist = 
                    Processing.CalculateDistanceInKilometers(
                        arc.locations.First(), arc.locations.Last());
                double totalDist = 
                    Processing.CalculateDistanceInKilometers(
                        arc.locations);

                if (birdDist != totalDist)
                {
                    double[] curves = Processing.EvaluateCurves(arc.locations);
                }
                double[] tempInput = { 1.0, 1.0, 1.0, 1.0, 0.5 };
                tempList[nr] = tempInput;
            }
            return tempList;
        }

        //Creates an output list. 
        //For each arc a double[] is created and placed in the list.
        //The array length is the number of possible restrictions
        //If an arc has a restriction the position in the array corresponding to the same 
        //type number of a restriction - 1, get the value 1, the rest 0. e.g {0, 1, 0, 0} type 2
        public static double[][] CreateOutputList(Network_DTO net)
        {

            double[][] outputList = new double[net.arcs.Count()][];
            int listNr = 0;            

            foreach (Arc_DTO arc in net.arcs)
            {
                double[] listElement = new double[RESTRICTIONS];
                if (arc.arcRestrictionIds.Any())
                {
                    foreach (string restriction in arc.arcRestrictionIds)
                    {
                        int resNr = net.restrictions.Where(a => a.id == restriction).First().type - 1;
                        listElement[resNr] = 1.0;
                    }
                    outputList[listNr] = listElement;
                }
                else
                {
                    outputList[listNr] = listElement;
                }
                listNr++;
            }
            return outputList;
        }

        public static void CreateANN(Input i, Output o)
        {
            // initialize input and output values
            double[][] input = new double[4][] {
            new double[] {0, 0}, new double[] {0, 1},
            new double[] {1, 0}, new double[] {1, 1}
            };
            double[][] output = new double[4][] {
            new double[] {0}, new double[] {1},
            new double[] {1}, new double[] {0}
            };

            int[] layers = {2, 1}; //two neurons in the first layer, one in the second

            // create neural network
            ActivationNetwork network = new ActivationNetwork(
                new SigmoidFunction(),
                2, // two inputs in the network
                layers);
            
            // create teacher
            BackPropagationLearning teacher = new BackPropagationLearning(network);

            bool needToStop = false;

            // loop
            while (!needToStop)
            {
                // run epoch of learning procedure
                double error = teacher.RunEpoch(input, output);
                // check error value to see if we need to stop
                if (error < 0.0001)
                    needToStop = true;
            }
        }

        //Interface stuff
        public static string[] dataSets = { "Norrkoping", "Linkoping", "Nykoping" };
        public static void DisplayData()
        {
            Console.WriteLine("Choose data set to load:");
            for(int i = 0; i < dataSets.Length; i++)
            {
                Console.WriteLine(i + 1 + ". " + dataSets[i]);
            }
            Console.WriteLine("Norrkoping was selected");
        }
    }
}