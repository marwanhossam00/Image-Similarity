﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ImageSimilarity.ImageOperations;
using System.Runtime.InteropServices;
using System.Windows.Forms.DataVisualization.Charting;

namespace ImageSimilarity
{
    public struct ChannelStats
    {
        public int[] Hist;
        public int Min;
        public int Max;
        public int Med;
        public double Mean;
        public double StdDev;
    }
    public struct ImageInfo
    {
        public string Path;
        public int Width;
        public int Height;
        public ChannelStats RedStats;
        public ChannelStats GreenStats;
        public ChannelStats BlueStats;
    }

    public struct MatchInfo : IComparable<MatchInfo>
    {
        public int CompareTo(MatchInfo b)
        {
            return this.MatchScore.CompareTo(b.MatchScore);
        }
        public string MatchedImgPath;
        public double MatchScore;
    }
    public class ImageHistSimilarity
    {
        /// <summary>
        /// Calculate the image stats (Max, Min, Med, Mean, StdDev & Histogram) of each color
        /// </summary>
        /// <param name="imgPath">Image path</param>
        /// <returns>Calculated stats of the given image</returns>
        public static ImageInfo CalculateImageStats(string imgPath)
        {
            //throw new NotImplementedException();
            ImageInfo result = new ImageInfo();
            result.Path = imgPath;
            RGBPixel[,] img2darray = ImageOperations.OpenImage(imgPath);
            result.Height = img2darray.GetLength(0);
            result.Width = img2darray.GetLength(1);

            int size = result.Width * result.Height;

            ChannelStats redStats = new ChannelStats();
            ChannelStats greenStats = new ChannelStats();
            ChannelStats blueStats = new ChannelStats();

            int[] redFreq = new int[256];
            int[] greenFreq = new int[256];
            int[] blueFreq = new int[256];

            redStats.Max = int.MinValue;
            redStats.Min = int.MaxValue;
            greenStats.Max = int.MinValue;
            greenStats.Min = int.MaxValue;
            blueStats.Max = int.MinValue;
            blueStats.Min = int.MaxValue;

            double redSum = 0, greenSum = 0, blueSum = 0;

            for (int i = 0; i < result.Height; i++)
            {
                for (int j = 0; j < result.Width; j++)
                {
                    int red = (int)img2darray[i,j].red;
                    int green = (int)img2darray[i,j].green;
                    int blue = (int)img2darray[i, j].blue;

                    redFreq[red]++;
                    greenFreq[green]++;
                    blueFreq[blue]++;

                    redSum += red;
                    greenSum += green;
                    blueSum += blue;

                    if (red > redStats.Max) redStats.Max = red;
                    if (red < redStats.Min) redStats.Min = red;
                    if (green > greenStats.Max) greenStats.Max = green;
                    if (green < greenStats.Min) greenStats.Min = green;
                    if (blue > blueStats.Max) blueStats.Max = blue;
                    if (blue < blueStats.Min) blueStats.Min = blue;
                }
            }
           
            redStats.Mean = redSum / ((double)size);
            greenStats.Mean = greenSum / ((double)size);
            blueStats.Mean = blueSum / ((double)size);
            
            redStats.Hist = redFreq;
            greenStats.Hist = greenFreq;
            blueStats.Hist = blueFreq;

            bool even = false;
            if ((size) % 2 == 0) even = true;
            int idx1 = size / 2;
            int idx2 = idx1 + 1;

            int num1Red = -1, num2Red = -1, num1Green = -1, num2Green = -1, num1Blue = -1, num2Blue = -1; ;

            int prefRed = 0, prefGreen = 0, prefBlue = 0;
            for (int i = 0; i < 256; ++i)
            {
                if (!even && num1Red != -1 && num1Green != -1 && num1Blue != -1) break;
                if (even && num1Red != -1 && num2Red != -1 && num1Green != -1 && num2Green != -1 && num1Blue != -1 && num2Blue != -1) break;
                prefRed += redFreq[i];
                prefGreen += greenFreq[i];
                prefBlue += blueFreq[i];
                if (prefRed >= idx1 && num1Red == -1) num1Red = i;
                if (prefRed >= idx2 && num2Red == -1) num2Red = i;
                if (prefGreen >= idx1 && num1Green == -1) num1Green = i;
                if (prefGreen >= idx2 && num2Green == -1) num2Green = i;
                if (prefBlue >= idx1 && num1Blue == -1) num1Blue = i;
                if (prefBlue >= idx2 && num2Blue == -1) num2Blue = i;
            }
            if (even)
            {
                redStats.Med = (num1Red + num2Red) / 2;
                greenStats.Med = (num1Green + num2Green) / 2;
                blueStats.Med = (num1Blue + num2Blue) / 2;
            }
            else
            {
                redStats.Med = num1Red;
                greenStats.Med = num1Green;
                blueStats.Med = num1Blue;
            }

            double redMean = redStats.Mean;
            double greenMean = greenStats.Mean;
            double blueMean = blueStats.Mean;

            double red_sum = 0, green_sum = 0, blue_sum = 0;

            for(int i = 0; i < 256; ++i)
            {
                red_sum += Math.Pow(i - redMean, 2) * redFreq[i];
                green_sum += Math.Pow(i - greenMean, 2) * greenFreq[i];
                blue_sum += Math.Pow(i - blueMean, 2) * blueFreq[i];
            }

            redStats.StdDev = Math.Sqrt(red_sum / ((double)size));
            greenStats.StdDev = Math.Sqrt(green_sum / ((double)size));
            blueStats.StdDev = Math.Sqrt(blue_sum / ((double)size));

            result.RedStats = redStats;
            result.GreenStats = greenStats;
            result.BlueStats = blueStats;

            return result;
        }
        /// <summary>
        /// Load all target images and calculate their stats
        /// </summary>
        /// <param name="targetPaths">Path of each target image</param>
        /// <returns>Calculated stats of each target image</returns>
        public static ImageInfo[] LoadAllImages(string []targetPaths)
        {
            //throw new NotImplementedException();

            ImageInfo[] targetImgStats = new ImageInfo[targetPaths.Length];
            
            Task<ImageInfo>[] calculate_threads = new Task<ImageInfo>[targetPaths.Length];
            for (int i = 0; i < targetPaths.Length; i++)
            {
                calculate_threads[i] = Task.Run(() => CalculateImageStats(targetPaths[i]));
                targetImgStats[i] = calculate_threads[i].Result;
            }
            Task.WaitAll(calculate_threads);
            return targetImgStats;
        }

        /// <summary>
        /// Match the given query image with the given target images and return the TOP matches as specified
        /// </summary>
        /// <param name="queryPath">Path of the query image</param>
        /// <param name="targetImgStats">Calculated stats of each target image</param>
        /// <param name="numOfTopMatches">Desired number of TOP matches to be returned</param>
        /// <returns>Top matches (image path & distance score) </returns>
        public static MatchInfo[] FindTopMatches(string queryPath, ImageInfo[] targetImgStats, int numOfTopMatches) 
        {
            //throw new NotImplementedException();
            ImageInfo queryStats = new ImageInfo();
            queryStats = CalculateImageStats(queryPath);
           
            MatchInfo[] matchedImages = new MatchInfo[numOfTopMatches];

            SortedSet<MatchInfo> matchedImagesSorted = new SortedSet<MatchInfo>();
            
            Task<MatchInfo>[] calculate_threads = new Task<MatchInfo>[targetImgStats.Length];
            for (int i = 0; i < targetImgStats.Length; i++)
            {
                calculate_threads[i] = Task.Run(() => CalculateMatch(queryStats, targetImgStats[i]));
                matchedImagesSorted.Add(calculate_threads[i].Result);
            }
            Task.WaitAll(calculate_threads);

            int k = 0;
            foreach (MatchInfo match in matchedImagesSorted)
            {
                if (k >= numOfTopMatches) break;
                matchedImages[k++] = match;
            }

            return matchedImages;
        }

        public static MatchInfo CalculateMatch(ImageInfo a, ImageInfo b)
        {
            MatchInfo matchingResult = new MatchInfo();
            matchingResult.MatchedImgPath = b.Path;

            double sizeA = a.Width * a.Height;
            double sizeB = b.Width * b.Height;

            double redDP = 0.0, redMagA = 0.0, redMagB = 0.0, 
                greenDP = 0.0, greenMagA = 0.0, greenMagB = 0.0, 
                blueDP = 0.0, blueMagA = 0.0, blueMagB = 0.0;

            object lockObj = new object();

            Parallel.For(0, 256,
            () => new LocalData { redDP = 0.0, greenDP = 0.0, blueDP = 0.0, redMagA = 0.0, redMagB = 0.0, greenMagA = 0.0, greenMagB = 0.0, blueMagA = 0.0, blueMagB = 0.0},
            (i, loopState, localData) =>
            {
                double redai = (a.RedStats.Hist[i]/sizeA), redbi = (b.RedStats.Hist[i]/sizeB), 
                greenai = (a.GreenStats.Hist[i]/sizeA), greenbi = (b.GreenStats.Hist[i]/sizeB),
                blueai = (a.BlueStats.Hist[i]/sizeA), bluebi = (b.BlueStats.Hist[i]/sizeB);
                localData.redDP += redai * redbi;
                localData.greenDP += greenai * greenbi;
                localData.blueDP += blueai * bluebi;

                localData.redMagA += redai * redai;
                localData.redMagB += redbi * redbi;
                localData.greenMagA += greenai * greenai;
                localData.greenMagB += greenbi * greenbi;
                localData.blueMagA += blueai * blueai;
                localData.blueMagB += bluebi * bluebi;
                return localData;
            },
            localData =>
            {
                lock (lockObj)
                {
                    redDP += localData.redDP;
                    greenDP += localData.greenDP;
                    blueDP += localData.blueDP;
                    redMagA += localData.redMagA;
                    redMagB += localData.redMagB;
                    greenMagA += localData.greenMagA;
                    greenMagB += localData.greenMagB;
                    blueMagA += localData.blueMagA;
                    blueMagB += localData.blueMagB;
                }
            });

            double redCosine = redDP/Math.Sqrt(redMagA * redMagB),
                   greenCosine = greenDP/Math.Sqrt(greenMagA * greenMagB),
                   blueCosine = blueDP/Math.Sqrt(blueMagA * blueMagB);

            double redAngle = Math.Acos(redCosine) * (180.0 / Math.PI),
                greenAngle = Math.Acos(greenCosine) * (180.0 / Math.PI),
                blueAngle = Math.Acos(blueCosine) * (180.0 / Math.PI);

            double avg = (redAngle + greenAngle + blueAngle) / 3.0;
            matchingResult.MatchScore = avg;

            return matchingResult;
        }

        private struct LocalData { public double redDP, greenDP, blueDP, redMagA, redMagB, blueMagA, blueMagB, greenMagA, greenMagB;}
    }
}
