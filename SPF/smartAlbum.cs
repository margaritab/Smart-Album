﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Emgu.CV;
using System.Windows.Forms;


namespace SPF
{
    class smartAlbum
    {
        private string allPath;
        private string truePath;
        private string userPath;
        private string decidePath;
        private LearningAlgorithmML learningAlgo;
        private SVM_Matlab learningAlgoSvm;



        private string[] allImages, learnedTrue, learnedFalse;
        int[] subset;
        bool[] isImageGood;


        public static int sd02 = 0, sd2plus = 0;

        public smartAlbum(string allPathL, string truePathL, string userPathL, string decidePathL)
        {
            allPath = allPathL;
            truePath = truePathL; //good images
            userPath = userPathL;
            decidePath = decidePathL;
        }

        public smartAlbum(string allPathL, string truePathL)
        {
            allPath = allPathL;
            truePath = truePathL;
        }


        public smartAlbum(string all, string[] allImages, bool[] isImageGood, int[] subset)
        {
            allPath = all;
            userPath = getPicturesDirectory();
            decidePath = getPicturesDirectory() + "\\Decide";
            this.allImages = allImages;
            this.isImageGood = isImageGood;
            this.subset = subset;

            Arrange(allImages, isImageGood, subset);
            DataConverter.copyImagesToFolder(decidePath, allImages);
        }

        public static string getRootDirectory()
        {
            string path = Environment.CurrentDirectory.ToString();
            return path;
        }

        public static string getPicturesDirectory()
        {
            string pictures = Path.Combine(getRootDirectory(), "pictures");
            return pictures;
        }

        public static string getMatlabDirectory()
        {
            string path = Path.Combine(getRootDirectory(), "papa_matlab");
            return path;
        }

        public static string getHaarCascadeDirectory()
        {
            string path = Path.Combine(Environment.CurrentDirectory.ToString(), "HaarCascade");
            return path;
        }

        public void Arrange(string[] filesAll, bool[] isImageGood, int[] subset)
        {
            int trueCount = 0;
            int falseCount = 0;

            for (int i = 0; i < isImageGood.Length; i++)
            {
                if (isImageGood[i])
                    trueCount++;
                else
                    falseCount++;
            }

            learnedTrue = new string[trueCount];
            learnedFalse = new string[falseCount];
            int trueIndex = 0;
            int falseIndex = 0;

            for (int i = 0; i < subset.Length; i++)
            {
                if (isImageGood[i])
                    learnedTrue[trueIndex++] = filesAll[subset[i]];
                else
                    learnedFalse[falseIndex++] = filesAll[subset[i]];
            }

        }

        public bool Learn()
        {

            // Get list of files in folders
            string[] filesAll = Testing.GetFilesFromDirectory(decidePath).ToArray();
            if (filesAll.Length <= 0)
                return false;

            ImageVector[] allImages;
            extractAlbum(out allImages, filesAll, false);
            //allImages = Internal_GetImages(filesAll);
            //ImageVector[] ImagesLearnedTrue = Internal_GetImages(learnedTrue);
            //ImageVector[] ImagesLearnedFalse = Internal_GetImages(learnedFalse);

            learningAlgoSvm = new SVM_Matlab(userPath, 1, 1, SVM_Matlab.Kernel.SVM_POLY, allImages);
            learningAlgoSvm.Learn(subset, isImageGood);

            return true;
        }

        public bool Decide()
        {
            // Get list of files in folders
            //String[] files = Testing.GetFilesFromDirectory(decidePath).ToArray();
            string csvPath = userPath + "SVM\\Decide.txt";
            string resultPath = userPath + "SVM\\Result.txt";

            // Get list of files in folders
            string[] filesAll = Testing.GetFilesFromDirectory(allPath).ToArray();
            string[] filesTrue = Testing.GetFilesFromDirectory(truePath).ToArray();
            if (filesAll.Length <= 0 || filesTrue.Length <= 0)
                return false;

            // Make False vector list - All the files in all that are not in true.

            string[] filesFalse = DataConverter.Internal_ExtractFalseFiles(filesAll, filesTrue);

            Console.WriteLine("Process good images: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
            ImageVector[] goodImages = Internal_GetImages(filesTrue);
            Console.WriteLine("Process bad images: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
            ImageVector[] badImages = Internal_GetImages(filesFalse);


            learningAlgo.Decide();

            LearningAlgorithmML.Algorithm[] filesTrueResults = new LearningAlgorithmML.Algorithm[filesTrue.Length];
            LearningAlgorithmML.Algorithm[] filesFalseResults = new LearningAlgorithmML.Algorithm[filesFalse.Length];

            // let the algorithm decide for each good image if it's is good or bad
            Console.WriteLine("deciding on good images Time: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
            for (int i = 0; i < filesTrue.Length; i++)
            {
                //filesTrueResults[i] = Internal_DecideImage(goodImages[i]);
                //if(filesTrueResults[i] == learningAlgo.Algo)
                //{
                //    string[] path = goodImages[i].Path.Split('\\');
                //    string fileName = path[path.Length - 1];
                //    File.Copy(goodImages[i].Path, decidePath + "\\" + fileName);
                //}
            }

            // let the algorithm decide for each bad image if it's good or bad
            Console.WriteLine("deciding on bad images Time: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
            for (int i = 0; i < filesFalse.Length; i++)
            {
                //filesFalseResults[i] = Internal_DecideImage(badImages[i]);
                //if (filesFalseResults[i] == learningAlgo.Algo)
                //{
                //    string[] path = goodImages[i].Path.Split('\\');
                //    string fileName = path[path.Length - 1];
                //    File.Copy(goodImages[i].Path, decidePath + "\\" + fileName);
                //}
            }

            // analyze the results
            Console.WriteLine("Analyzing the results Time: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
            learningAlgo.checkDecision();


            learningAlgo.Quit();
            return true;

        }

        public void Test()
        {

            // Get list of files in folders
            string[] filesAll = Testing.GetFilesFromDirectory(allPath).ToArray();
            string[] filesTrue = Testing.GetFilesFromDirectory(truePath).ToArray();
            if (filesAll.Length <= 0 || filesTrue.Length <= 0)
                return;

            string[] filesFalse = DataConverter.Internal_ExtractFalseFiles(filesAll, filesTrue);

            //contains the parameters for each picture and the picture path
            ImageVector[] goodImages = null;
            ImageVector[] badImages = null;

            extractAlbum(out goodImages, out badImages, filesTrue, filesFalse, userPath, false);


            // remove comment if you want to optimize one of the algorithms

            // optimize svm
            // Internal_SVM(goodImages, badImages);
            // optimize knn
            // Internal_KNN(goodImages, badImages);
            // optimize knn and svm`
            Internal_KNN_SVM(goodImages, badImages); //regular intersection
            //testIntersection(goodImages, badImages); //smart intersection

            // test using cross validation
            //learningAlgo = new SVM_Matlab(userPath, -5, -5, SVM_Matlab.Kernel.SVM_RBF);
            //svm = new SVM(SVM.Kernel.SVM_RBF, 1, 1);
            //crossValidation(goodImages, badImages);
            // test on different albums
            //differentAlbums(goodImages, badImages);

            copyGoodImages(filesTrue, filesFalse);



            Console.WriteLine("Done: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
            learningAlgo.Quit();
        }

        void copyGoodImages(string[] filesTrue, string[] filesFalse)
        {
            if (!Directory.Exists(decidePath))
                Directory.CreateDirectory(decidePath);

            for (int i = 0; i < filesTrue.Length; i++)
            {
                string[] fileName = filesTrue[i].Split('\\');
                string name = fileName[fileName.Length - 1];
                if (learningAlgo.FilesTrueResults[i] == LearningAlgorithmML.Algorithm.GOOD)
                    File.Copy(filesTrue[i], decidePath + "\\" + name);
            }

            for (int i = 0; i < filesFalse.Length; i++)
            {
                string[] fileName = filesFalse[i].Split('\\');
                string name = fileName[fileName.Length - 1];
                if (learningAlgo.FilesFalseResults[i] == LearningAlgorithmML.Algorithm.GOOD)
                    File.Copy(filesFalse[i], decidePath + "\\" + name);
            }
        }

        /*
         * this function is for testing a single album using cross validation technique
         * learn from 10% - 60% of the album randomly and the decide on the rest
         * each percent is tested 10 times and the results are averaged
         */
        private void crossValidation(ImageVector[] goodImages, ImageVector[] badImages)
        {


            double totalImages = goodImages.Length + badImages.Length;
            double lowest, goodPercent = goodImages.Length / totalImages;
            lowest = 1 - goodPercent;
            lowest -= 0.10;
            bool quit = false;
            for (double percent = 0.1; percent < 0.2; percent += 0.1)
            {
                int p = (int)(percent * 100);

                for (int i = 1; i <= 1; i++)
                {

                    Console.WriteLine("Percent = " + percent + ",Iteration =  " + i + ", Time: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
                    Console.WriteLine("Learning from " + p + "% of the album");

                    int[] subsetFilesTrue = DataConverter.Internal_ExtractSubset(goodImages.Length, percent);
                    int[] subsetFilesFalse = DataConverter.Internal_ExtractSubset(badImages.Length, percent);

                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    if (!learningAlgo.Learn(percent, subsetFilesTrue, subsetFilesFalse))
                    {
                        MessageBox.Show("learn failed");
                        learningAlgo.Quit();
                        return;
                    }
                    if (!learningAlgo.Decide())
                    {
                        MessageBox.Show("decide failed");
                        learningAlgo.Quit();
                        return;
                    }
                    learningAlgo.checkDecision();
                    stopWatch.Stop();
                    double elapsed = (double)stopWatch.ElapsedMilliseconds / 1000;
                    Console.WriteLine("Total time " + elapsed);
                }

                //**********calculate the most importent attributes***********//
                double[][] pk = new double[ImageVector.NUMBER_OF_PARAMETERS][];
                double[][] nk = new double[ImageVector.NUMBER_OF_PARAMETERS][];
                double[] remainder = new double[ImageVector.NUMBER_OF_PARAMETERS];
                double[] gain = new double[ImageVector.NUMBER_OF_PARAMETERS];

                ClassifierNew cn;
                cn = new ClassifierNew();



                cn.initArray(remainder);
                cn.initArray(gain);

                for (int k = 0; k < ImageVector.NUMBER_OF_PARAMETERS; k++)
                {
                    //positive negative example
                    pk[k] = new double[cn.sizeRange(ImageVector.getParameterNameByIndex(k)) + 1];
                    nk[k] = new double[cn.sizeRange(ImageVector.getParameterNameByIndex(k)) + 1];
                }

                for (int i = 0; i < goodImages.Length; i++)
                {
                    // Console.WriteLine("pk["+i+"]");
                    for (int k = 0; k < ImageVector.NUMBER_OF_PARAMETERS; k++)
                    {
                        cn.calacPositiveNegative(ref pk[k], goodImages[i].getParameterByIndex(k), ImageVector.getParameterNameByIndex(k));
                    }
                }

                for (int i = 0; i < badImages.Length; i++)
                {
                    //   Console.WriteLine("nk[" + i + "]");
                    for (int k = 0; k < ImageVector.NUMBER_OF_PARAMETERS; k++)
                    {
                        cn.calacPositiveNegative(ref nk[k], badImages[i].getParameterByIndex(k), ImageVector.getParameterNameByIndex(k));
                    }

                }

                int size = goodImages.Length + badImages.Length;
                double max = 1;
                int bestIndex = -1;

                for (int i = 0; i < ImageVector.NUMBER_OF_PARAMETERS; i++)
                {
                    cn.calcRemainder(ref remainder[i], pk[i], nk[i], size);
                    //   Console.WriteLine("remainder["+i+"] " +remainder[i]);
                    cn.calcGain(ref gain[i], remainder[i], goodImages.Length, badImages.Length);
                    Console.WriteLine("gain[" + i + "] " + gain[i]);
                    if (max > remainder[i])
                    {
                        max = remainder[i];
                        bestIndex = i;
                    }
                }



                string AttributeImgPath = userPath + "\\decisionTreeBestAttributes.txt";
                string goodPath = userPath + "\\imagesGood.txt";
                string badPath = userPath + "\\imagesBad.txt";

                if (!File.Exists(AttributeImgPath) || new FileInfo(AttributeImgPath).Length == 0)
                {

                    for (int i = 0; i < ImageVector.NUMBER_OF_PARAMETERS; i++)
                    {
                        cn.calcAttributes(goodImages, badImages, pk[i], nk[i], bestIndex, goodPath, badPath, AttributeImgPath);
                    }

                    //fileBestAttributeImages.Close();

                }
                File.Delete(AttributeImgPath);



                //********************************//

                string algoPath = learningAlgo.ResultPath;
                if (!Directory.Exists(algoPath))
                    Directory.CreateDirectory(algoPath);
                string DataPath = algoPath + "\\Data_" + p + "_" + SVM_Matlab.CONFIDENCE_BAR + ".txt";
                System.IO.StreamWriter file = new System.IO.StreamWriter(@DataPath, false);
                string data = learningAlgo.ToString();
                file.WriteLine(data);
                file.Close();
                if (quit)
                    return;

                // starting to test new percent so reset all the data to zero
                //learningAlgo.restartTest();

            }
        }

        /*
         * this test is for finding the best optimization for SVM algorithm
         * trying sigma and gamma for the rbf kernel for best accuracy.
         */
        private void Internal_SVM(ImageVector[] goodImages, ImageVector[] badImages)
        {
            for (double i = 0; i <= 5; i += 1)
            {
                for (double j = 0; j <= 0; j += 1)
                {
                    double sigma = Math.Pow(2, i);
                    double gamma = Math.Pow(2, j);
                    //learningAlgo = new SVM(userPath, i, 1, SVM.Kernel.SVM_POLY, goodImages, badImages);
                    learningAlgo = new SVM_Matlab(userPath, sigma, 1, SVM_Matlab.Kernel.SVM_RBF, goodImages, badImages);
                    crossValidation(goodImages, badImages);
                }
            }
        }

        /*
         * this test is for finding the best optimization for SVM algorithm
         * trying sigma and gamma for the rbf kernel for best accuracy.
         */
        private void Internal_KNN_SVM(ImageVector[] goodImages, ImageVector[] badImages)
        {
            int k = 2;
            for (double i = 0; i <= 5; i += 1)
            {
                for (double j = 0; j <= 0; j += 1)
                {
                    double sigma = Math.Pow(2, i);
                    double gamma = Math.Pow(2, j);
                    learningAlgo = new KNN_SVM(userPath, sigma, gamma, KNN_SVM.Kernel.SVM_RBF, k, goodImages, badImages);
                    crossValidation(goodImages, badImages);
                }
            }
        }

        public void testIntersection(ImageVector[] goodImages, ImageVector[] badImages)
        {
            for (double conf = 0.5; conf <= 0.5; conf += 0.1)
            {
                SVM_Matlab.CONFIDENCE_BAR = conf;
                learningAlgo = new smartIntersection(userPath, goodImages, badImages);
                crossValidation(goodImages, badImages);
            }

        }

        /*
         * this test is for finding the best optimization for KNN algorithm
         * finding the best k for this classification
         */
        public void Internal_KNN(ImageVector[] goodImages, ImageVector[] badImages)
        {
            for (int k = 2; k <= 2; k++)
            {
                learningAlgo = new KNN_Matlab(userPath, k, goodImages, badImages);
                crossValidation(goodImages, badImages);
            }

        }

        private ImageVector[] Internal_GetImages(string[] imagesStr)
        {
            ImageVector[] images = new ImageVector[imagesStr.Length];
            int index = 0;
            foreach (string path in imagesStr)
            {
                int i = index + 1;
                Console.WriteLine("Processing image parameters #" + i + " out of " + imagesStr.Length);
                images[index] = new ImageVector(path, null);
                index++;
            }
            return images;
        }

        private void Internal_getImageFromFile(ref ImageVector[] allImages, string path, int allSize)
        {
            System.IO.StreamReader fileImages = new System.IO.StreamReader(path);
            int size = File.ReadAllLines(path).Count();
            string line;
            int goodIndex = 0;
            int badIndex = 0;
            allImages = new ImageVector[allSize];

            while ((line = fileImages.ReadLine()) != null)
            {
                string[] imageParams = line.Split(',');
                double[] parameters = new double[imageParams.Length - 1];

                for (int i = 0; i < imageParams.Length - 1; i++)
                    parameters[i] = Convert.ToDouble(imageParams[i]);

                allImages[goodIndex++] = new ImageVector(parameters);
            }

            fileImages.Close();
        }

        private void Internal_getImageFromFile(ref ImageVector[] goodImages, ref ImageVector[] badImages, string goodPath, string badPath, int goodSize, int badSize)
        {
            System.IO.StreamReader fileImages = new System.IO.StreamReader(goodPath);
            int size = File.ReadAllLines(goodPath).Count();
            string line;
            int goodIndex = 0;
            int badIndex = 0;
            goodImages = new ImageVector[goodSize];
            badImages = new ImageVector[badSize];

            while ((line = fileImages.ReadLine()) != null)
            {
                string[] imageParams = line.Split(',');
                double[] parameters = new double[imageParams.Length - 1];

                for (int i = 0; i < imageParams.Length - 1; i++)
                    parameters[i] = Convert.ToDouble(imageParams[i]);

                if (imageParams[imageParams.Length - 1] == "1")
                    goodImages[goodIndex++] = new ImageVector(parameters);
                else
                    badImages[badIndex++] = new ImageVector(parameters);
            }

            fileImages.Close();
            fileImages = new System.IO.StreamReader(badPath);

            while ((line = fileImages.ReadLine()) != null)
            {
                string[] imageParams = line.Split(',');
                double[] parameters = new double[imageParams.Length - 1];

                for (int i = 0; i < imageParams.Length - 1; i++)
                    parameters[i] = Convert.ToDouble(imageParams[i]);

                if (imageParams[imageParams.Length - 1] == "1")
                    goodImages[goodIndex++] = new ImageVector(parameters);
                else
                    badImages[badIndex++] = new ImageVector(parameters);
            }
            fileImages.Close();
        }

        private void extractAlbum(out ImageVector[] allImages, string[] filesAll, bool checkFiles)
        {
            allImages = null;
            string allImagesPath = userPath + "\\allImages.txt";
            if ((!File.Exists(allImagesPath) || new FileInfo(allImagesPath).Length == 0 || checkFiles))
            {
                Console.WriteLine("Process images: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
                allImages = Internal_GetImages(filesAll);

                System.IO.StreamWriter fileImages = new System.IO.StreamWriter(allImagesPath, false);
                for (int index = 0; index < allImages.Length; index++)
                {
                    fileImages.WriteLine(allImages[index].getAllParameters(true));
                }

                fileImages.Close();
            }
            else
                Internal_getImageFromFile(ref allImages, allImagesPath, filesAll.Length);
        }

        private void extractAlbum(out ImageVector[] goodImages, out ImageVector[] badImages, string[] filesTrue,
            string[] filesFalse, string path, bool checkFiles)
        {
            goodImages = null;
            badImages = null;
            string goodImagesPath = path + "\\imagesGood.txt";
            string badImagesPath = path + "\\imagesBad.txt";
            if ((!File.Exists(goodImagesPath) || !File.Exists(badImagesPath) ||
                new FileInfo(goodImagesPath).Length == 0 || new FileInfo(badImagesPath).Length == 0) || checkFiles)
            {
                Console.WriteLine("Process good images: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
                goodImages = Internal_GetImages(filesTrue);
                Console.WriteLine("Process bad images: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
                badImages = Internal_GetImages(filesFalse);

                System.IO.StreamWriter fileGoodImages = new System.IO.StreamWriter(goodImagesPath, false);
                System.IO.StreamWriter fileBadImages = new System.IO.StreamWriter(badImagesPath, false);
                for (int index = 0; index < goodImages.Length; index++)
                {
                    fileGoodImages.WriteLine(goodImages[index].getAllParameters(true));

                }
                for (int index = 0; index < badImages.Length; index++)
                {
                    fileBadImages.WriteLine(badImages[index].getAllParameters(false));
                }

                fileGoodImages.Close();
                fileBadImages.Close();
            }
            else
                Internal_getImageFromFile(ref goodImages, ref badImages, goodImagesPath, badImagesPath, filesTrue.Length, filesFalse.Length);


        }


    }
}
