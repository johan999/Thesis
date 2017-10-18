using AForge.Neuro;
using AForge.Neuro.Learning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AI
{
    class Program
    {
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
            public int[] restrictions; //
        }

        static void Main(string[] args)
        {
            //Just for later generalization
            string jsonName = "norrkoping_v2";
            Network_DTO roadNetwork = Network_DTO.LoadJson(jsonName + ".json");
            CreateInputList(roadNetwork);
            //CreateANN();
            
            Console.ReadLine();
        }

        //Input should return:        
        //Speed, Traffic flow, Visibility, Light, Pavement
        public static List<Input> CreateInputList(Network_DTO net)
        {
            List<Input> tempList = null;
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
                Input tempInput = new Input(1, 1, 1, 1, 0.5);
                tempList.Add(tempInput);
            }
            return null;
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
    }
}