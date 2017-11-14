using Accord.Neuro;
using Accord.Neuro.Learning;
using System;

namespace AI
{
    class NeuralNetwork
    {
        public static double[][] ComputeNetwork(double[][] input, ActivationNetwork ANN)
        {
            double[][] output = new double[input.Length][];
            int i = 0;
            foreach (double[] d in input)
            {
                output[i] = ANN.Compute(d);
                i++;
            }
            return output;
        }

        public static ActivationNetwork CreateANN(double[][] input, double[][] output, double learningRate)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int nrOfOutputs = output[0].Length;
            int nrOfInputs = input[0].Length;

            int[] layers = { 4, 4, nrOfOutputs }; //Neurons per layer

            ActivationNetwork network = new ActivationNetwork(
                new SigmoidFunction(),  //Activation function
                2,                      //Input
                layers);                //Hidden layers + output

            BackPropagationLearning teacher = new BackPropagationLearning(network);
            teacher.LearningRate = learningRate;
            bool needToStop = false;

            int epochs = 0;
            double prevError = 999999.9;
            while (!needToStop)
            {
                double error = teacher.RunEpoch(input, output);

                if (Math.Abs(prevError - error) < 0.0000001 || epochs == 10000)
                {
                    needToStop = true;
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    Console.WriteLine("\n" + 2 + ", " + nrOfOutputs + " Stop at: " + epochs +
                        " Error: " + error + " Time: " + elapsedMs / 1000 + "s");
                }
                if (epochs % 1000 == 0 && epochs != 0)
                {
                    Console.Write(".");
                }
                prevError = error;
                epochs++;
            }
            return network;
        }

        public static void NetworkComparison(double[][] calculatedOutput, double[][] originalOutput)
        {
            int restrictionsLength = originalOutput[0].Length;
            int nrOfOutputs = originalOutput.Length;
            if (nrOfOutputs != calculatedOutput.Length)
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
                    for (int j = 0; j < restrictionsLength; j++)
                    {
                        double resError = Math.Abs(calculatedOutput[i][j] - originalOutput[i][j]);
                        if (resError < 0.3)
                        {
                            correctRes++;
                        }

                        error += resError;
                    }
                    if (correctRes == restrictionsLength)
                    {

                        correct++;
                    }

                }
                error = error / nrOfOutputs;
                Console.WriteLine("Nr of correct arcs: " + correct + " of " + nrOfOutputs +
                    " Error of network: " + error);
            }
        }
    }
}