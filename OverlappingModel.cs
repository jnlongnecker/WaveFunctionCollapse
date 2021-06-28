/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

class OverlappingModel : Model
{
    int N;
    byte[][] patterns;
    List<Color> colors;
    int ground;

    public OverlappingModel(string name, int N, int width, int height, bool periodicInput, bool periodicOutput, int symmetry, int ground)
        : base(width, height)
    {
        // N: The sample is N x N pixels
        this.N = N;
        periodic = periodicOutput;

        // bitmap: The input texture
        var bitmap = new Bitmap($"samples/{name}.png");
        int SMX = bitmap.Width, SMY = bitmap.Height;
        byte[,] sample = new byte[SMX, SMY];

        // List of all colors in the sample
        colors = new List<Color>();

        // Loop through the input texture and retrieve a set of all the colors in the texture
        for (int y = 0; y < SMY; y++) for (int x = 0; x < SMX; x++)
            {
                Color color = bitmap.GetPixel(x, y);

                int i = 0;
                foreach (var c in colors)
                {
                    if (c == color) break;
                    i++;
                }

                if (i == colors.Count) colors.Add(color);
                sample[x, y] = (byte)i;
            }

        // C: Number of colors
        int C = colors.Count;

        // W: Number of colors ^ N * N
        long W = C.ToPower(N * N);

        // Begin setting up helper methods

        // From a byte matrix, travel left to right N cols and bottom to top N rows and map that to a 1 dimensional array
        byte[] pattern(Func<int, int, byte> f)
        {
            byte[] result = new byte[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) result[x + y * N] = f(x, y);
            return result;
        };

        // Take an x and y value from the sample texture and create an N x N sample from the texture with (x, y) being the bottom left
        byte[] patternFromSample(int x, int y) => pattern((dx, dy) => sample[(x + dx) % SMX, (y + dy) % SMY]);
        
        // Rotate the input byte array clockwise
        byte[] rotate(byte[] p) => pattern((x, y) => p[N - 1 - y + x * N]);

        // Reflect the input byte array across the x axis
        byte[] reflect(byte[] p) => pattern((x, y) => p[N - 1 - x + y * N]);

        // Convert a byte array into a single long that uniquely describes the array
        long index(byte[] p)
        {
            long result = 0, power = 1;
            for (int i = 0; i < p.Length; i++)
            {
                result += p[p.Length - 1 - i] * power;
                power *= C;
            }
            return result;
        };

        // Deconstruct an index to get the byte array it describes
        byte[] patternFromIndex(long ind)
        {
            long residue = ind, power = W;
            byte[] result = new byte[N * N];

            for (int i = 0; i < result.Length; i++)
            {
                power /= C;
                int count = 0;

                while (residue >= power)
                {
                    residue -= power;
                    count++;
                }

                result[i] = (byte)count;
            }

            return result;
        };

        // A map of patterns to the frequency they occur
        Dictionary<long, int> weights = new Dictionary<long, int>();

        // A list of all the possible patterns, with each pattern only occuring once in the list
        List<long> ordering = new List<long>();

        // Creates unique patterns from the sample texture. If the sample texture should wrap, periodicInput = true
        for (int y = 0; y < (periodicInput ? SMY : SMY - N + 1); y++) for (int x = 0; x < (periodicInput ? SMX : SMX - N + 1); x++)
            {
                /* A matrix containing 8 symmetries of the sampled pattern
                    0: The original pattern
                    1: Pattern refleced across the x axis
                    2: Pattern rotated 90 degrees clockwise
                    3: Pattern rotated 90 degrees clockwise then reflected across the x axis
                    4: Pattern rotated 180 degrees clockwise
                    5: Pattern rotated 180 degrees clockwise then reflected across the x axis
                    6: Pattern rotated 270 degrees clockwise
                    7: Pattern rotated 270 degrees clockwise then reflected across the x axis
                */
                byte[][] ps = new byte[8][];

                ps[0] = patternFromSample(x, y);
                ps[1] = reflect(ps[0]);
                ps[2] = rotate(ps[0]);
                ps[3] = reflect(ps[2]);
                ps[4] = rotate(ps[2]);
                ps[5] = reflect(ps[4]);
                ps[6] = rotate(ps[4]);
                ps[7] = reflect(ps[6]);

                // Symmetry controls how many forms of symmetry to use
                for (int k = 0; k < symmetry; k++)
                {
                    // Convert the pattern to a number
                    long ind = index(ps[k]);

                    // If the weights dictionary already contains the pattern, increase the weight for that pattern
                    // This allows repeated patterns to be weighted heavier for selection
                    if (weights.ContainsKey(ind)) weights[ind]++;

                    // If the dictionary does not contain it, add it to the dictionary and the ordering list
                    else
                    {
                        weights.Add(ind, 1);
                        ordering.Add(ind);
                    }
                }
            }

        // T: The number of unique patterns
        T = weights.Count;
        this.ground = (ground + T) % T;
        patterns = new byte[T][];
        base.weights = new double[T];

        // Convert the dictionary and list to arrays
        int counter = 0;
        foreach (long w in ordering)
        {
            patterns[counter] = patternFromIndex(w);
            base.weights[counter] = weights[w];
            counter++;
        }

        // Check to see if p1 matches up with p2 on a given side
        bool agrees(byte[] p1, byte[] p2, int dx, int dy)
        {
            int xmin = dx < 0 ? 0 : dx, 
                xmax = dx < 0 ? dx + N : N, 
                ymin = dy < 0 ? 0 : dy, 
                ymax = dy < 0 ? dy + N : N;
            
            for (int y = ymin; y < ymax; y++) 
                for (int x = xmin; x < xmax; x++) 
                    if (p1[x + N * y] != p2[x - dx + N * (y - dy)]) 
                        return false;
            return true;
        };

        // Loop 4 times
        // propagator: For each direction, store the list of patterns that match each pattern
        propagator = new int[4][][];
        for (int d = 0; d < 4; d++)
        {
            // Loop over each unique pattern
            propagator[d] = new int[T][];
            for (int t = 0; t < T; t++)
            {
                // The list of patterns that match up to the pattern we're looking at
                List<int> list = new List<int>();
                for (int t2 = 0; t2 < T; t2++) if (agrees(patterns[t], patterns[t2], DX[d], DY[d])) list.Add(t2);

                // Copy the list of the indexes of matching patterns 
                propagator[d][t] = new int[list.Count];
                for (int c = 0; c < list.Count; c++) propagator[d][t][c] = list[c];
            }
        }
    }

    // Periodic controls whether to wrap the output or not. If we wrap, periodic = true
    // Periodic must be false to return true. If so, then return if our sample would be off the map
    protected override bool OnBoundary(int x, int y) => !periodic && (x + N > FMX || y + N > FMY || x < 0 || y < 0);

    // Creates a graphic from the observed patterns
    public override Bitmap Graphics()
    {
        Bitmap result = new Bitmap(FMX, FMY);
        int[] bitmapData = new int[result.Height * result.Width];

        if (observed != null)
        {
            for (int y = 0; y < FMY; y++)
            {
                int dy = y < FMY - N + 1 ? 0 : N - 1;
                for (int x = 0; x < FMX; x++)
                {
                    int dx = x < FMX - N + 1 ? 0 : N - 1;
                    Color c = colors[patterns[observed[x - dx + (y - dy) * FMX]][dx + dy * N]];
                    bitmapData[x + y * FMX] = unchecked((int)0xff000000 | (c.R << 16) | (c.G << 8) | c.B);
                }
            }
        }
        else
        {
            for (int i = 0; i < wave.Length; i++)
            {
                int contributors = 0, r = 0, g = 0, b = 0;
                int x = i % FMX, y = i / FMX;

                for (int dy = 0; dy < N; dy++) for (int dx = 0; dx < N; dx++)
                    {
                        int sx = x - dx;
                        if (sx < 0) sx += FMX;

                        int sy = y - dy;
                        if (sy < 0) sy += FMY;

                        int s = sx + sy * FMX;
                        if (OnBoundary(sx, sy)) continue;
                        for (int t = 0; t < T; t++) if (wave[s][t])
                            {
                                contributors++;
                                Color color = colors[patterns[t][dx + dy * N]];
                                r += color.R;
                                g += color.G;
                                b += color.B;
                            }
                    }

                bitmapData[i] = unchecked((int)0xff000000 | ((r / contributors) << 16) | ((g / contributors) << 8) | b / contributors);
            }
        }

        var bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(bitmapData, 0, bits.Scan0, bitmapData.Length);
        result.UnlockBits(bits);

        return result;
    }

    // Delete patterns that aren't at index ground in the last row
    // Delete patterns that are at index ground in every pixel
    protected override void Clear()
    {
        base.Clear();

        if (ground != 0)
        {
            for (int x = 0; x < FMX; x++)
            {
                for (int t = 0; t < T; t++) if (t != ground) Ban(x + (FMY - 1) * FMX, t);
                for (int y = 0; y < FMY - 1; y++) Ban(x + y * FMX, ground);
            }

            Propagate();
        }
    }
}
