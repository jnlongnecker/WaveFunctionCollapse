/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;

abstract class Model
{
    protected bool[][] wave;

    protected int[][][] propagator;
    int[][][] compatible;
    protected int[] observed;

    (int, int)[] stack;
    int stacksize;

    protected Random random;
    protected int FMX, FMY, T;
    protected bool periodic;

    protected double[] weights;
    double[] weightLogWeights;

    int[] sumsOfOnes;
    double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

    // Called when child class is created
    // FMX and FMY are the width and height of the desired output
    protected Model(int width, int height)
    {
        FMX = width;
        FMY = height;
    }

    // Set up necessary variables
    void Init()
    {
        // For each pixel, store an array for each unique pattern
        wave = new bool[FMX * FMY][];

        // For each pixel, and for each unique pattern, create an int array of size 4
        compatible = new int[wave.Length][][];

        // Set up those two matrices
        for (int i = 0; i < wave.Length; i++)
        {
            wave[i] = new bool[T];
            compatible[i] = new int[T][];
            for (int t = 0; t < T; t++) compatible[i][t] = new int[4];
        }

        // For each weight w, store w * log(w)
        weightLogWeights = new double[T];
        sumOfWeights = 0;
        sumOfWeightLogWeights = 0;

        // Weights is a weight for each unique pattern, set in the child constructor
        // This loop sets up the above variables
        for (int t = 0; t < T; t++)
        {
            weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
            sumOfWeights += weights[t];
            sumOfWeightLogWeights += weightLogWeights[t];
        }

        // Calculate an entropy
        // What is an entropy, you ask? I dunno
        // But we spent a good bit of effort to calculate it
        startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

        sumsOfOnes = new int[FMX * FMY];
        sumsOfWeights = new double[FMX * FMY];
        sumsOfWeightLogWeights = new double[FMX * FMY];
        entropies = new double[FMX * FMY];

        // Stack with a max size of Each Pixel * Number of unique patterns
        stack = new (int, int)[wave.Length * T];
        stacksize = 0;
    }

    // Returns false if there are no weights (contradiction)
    // Returns true if there is 1 weight (solved)
    // Deletes some patterns and returns null otherwise
    bool? Observe()
    {
        // 1000, for those not versed in scientific notation
        double min = 1E+3;
        int argmin = -1;

        // Find the minimum entropy and the argument with that minumum, with some noise offset
        for (int i = 0; i < wave.Length; i++)
        {
            // Skip if we'd go off the map at this location and we don't want to do that
            if (OnBoundary(i % FMX, i / FMX)) continue;

            // If our sumOfOnes is 0, that's a contradiction and we return false
            int amount = sumsOfOnes[i];
            if (amount == 0) return false;

            // Adjust the min and argmin based off the entropy and some noise offset
            double entropy = entropies[i];
            if (amount > 1 && entropy <= min)
            {
                double noise = 1E-6 * random.NextDouble();
                if (entropy + noise < min)
                {
                    min = entropy + noise;
                    argmin = i;
                }
            }
        }

        // If the sumOfOnes == 1, the output has been solved
        // Save all the patterns that have been observed in the observed array
        // At the end, return true
        if (argmin == -1)
        {
            observed = new int[FMX * FMY];
            for (int i = 0; i < wave.Length; i++) for (int t = 0; t < T; t++) if (wave[i][t]) { observed[i] = t; break; }
            return true;
        }

        double[] distribution = new double[T];

        // For the pixel with the lowest entropy, get the weight of each pattern. If the pattern has not been set to true
        // in the wave matrix, set its weight to zero instead
        for (int t = 0; t < T; t++) distribution[t] = wave[argmin][t] ? weights[t] : 0;

        // Get a somewhat random index from the distribution list
        int r = distribution.Random(random.NextDouble());

        // Get the patterns at the pixel with the lowest entropy
        bool[] w = wave[argmin];

        // For all values not selected, ban them if they are not false. For the selected value, ban it if it's not true
        for (int t = 0; t < T; t++) if (w[t] != (t == r)) Ban(argmin, t);

        return null;
    }

    // For each banned pixel / pattern pair, ripple the effect outward so that any patterns that match with the banned
    // patterns will no longer link. Ban those pixel / pattern pairs if they find they have no more matches and 
    // ripple that as well
    protected void Propagate()
    {
        // Go through the stack of banned pixel / pattern pairs
        while (stacksize > 0)
        {
            // Pop a pixel / pattern pair
            var e1 = stack[stacksize - 1];
            stacksize--;

            // i1 = pixel value
            int i1 = e1.Item1;

            // Get the actual x,y coordinates of the pixel value
            int x1 = i1 % FMX, y1 = i1 / FMX;

            // For each cardinal direction
            for (int d = 0; d < 4; d++)
            {
                // Choose which pixel to look at
                // ORDER: Left, Up, Right, Down
                int dx = DX[d], dy = DY[d];

                // Get the second pixel's x,y coordinate
                int x2 = x1 + dx, y2 = y1 + dy;

                // If we're not wrapping and our second pixel is off the mappable space, skip it
                if (OnBoundary(x2, y2)) continue;

                // Ensure the second pixel is on the grid (this bit is for wrapping output)
                if (x2 < 0) x2 += FMX;
                else if (x2 >= FMX) x2 -= FMX;
                if (y2 < 0) y2 += FMY;
                else if (y2 >= FMY) y2 -= FMY;

                // Get the pixel value from the second pixel's coordinate
                int i2 = x2 + y2 * FMX;

                // Get the list of patterns that match the pattern on the stack
                int[] p = propagator[d][e1.Item2];
                int[][] compat = compatible[i2];

                // Loop over each matching pattern on the pixel
                for (int l = 0; l < p.Length; l++)
                {
                    // Reduce the number of compatible patterns for our second pixel
                    int t2 = p[l];
                    int[] comp = compat[t2];

                    comp[d]--;
                    // If this causes the second pixel / pattern pair to have no more matches, ban it as well
                    if (comp[d] == 0) Ban(i2, t2);
                }
            }
        }
    }

    // Run the algorithm
    public bool Run(int seed, int limit)
    {
        // Set up the wave matrix
        if (wave == null) Init();
        
        // Reset variables
        Clear();
        random = new Random(seed);

        // Run observations and propogations until solved or contradition found
        for (int l = 0; l < limit || limit == 0; l++)
        {
            bool? result = Observe();
            if (result != null) return (bool)result;
            Propagate();
        }

        // Return true if we hit the limit of allowed observations
        return true;
    }

    // Glorified delete button for a pattern (t) at a pixel (i)
    protected void Ban(int i, int t)
    {
        // Set the pattern specified at the pixel specified to be false
        wave[i][t] = false;

        // An int array of size 4 from the specified pixel / pattern combination
        int[] comp = compatible[i][t];

        // Set all those to 0
        for (int d = 0; d < 4; d++) comp[d] = 0;

        // Push the pixel / pattern pair to the stack
        stack[stacksize] = (i, t);
        stacksize++;

        // Subtract that pattern from existance at the pixel
        sumsOfOnes[i] -= 1;
        sumsOfWeights[i] -= weights[t];
        sumsOfWeightLogWeights[i] -= weightLogWeights[t];

        double sum = sumsOfWeights[i];
        entropies[i] = Math.Log(sum) - sumsOfWeightLogWeights[i] / sum;
    }

    // Resets everything
    protected virtual void Clear()
    {
        for (int i = 0; i < wave.Length; i++)
        {
            // Set all patterns to true for each pixel
            for (int t = 0; t < T; t++)
            {
                wave[i][t] = true;
                // Store in compatible the number of patterns that the linking cardinal pixel matches to
                for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[opposite[d]][t].Length;
            }

            sumsOfOnes[i] = weights.Length;
            sumsOfWeights[i] = sumOfWeights;
            sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
            entropies[i] = startingEntropy;
        }
    }

    protected abstract bool OnBoundary(int x, int y);
    public abstract System.Drawing.Bitmap Graphics();

    protected static int[] DX = { -1, 0, 1, 0 };
    protected static int[] DY = { 0, 1, 0, -1 };
    static int[] opposite = { 2, 3, 0, 1 };
}
