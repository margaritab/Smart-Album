using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Drawing;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;

using System.IO;
using System.Xml;
using System.Windows.Forms;
namespace SPF
{
    static class ImageProcessing
    {

        #region Private Members

        // Loaded image
        private static Image<Bgr, byte> _imageRGB;
        private static Image<Gray, byte> _imageGray;
        private static Image<Hsv, byte> _imageHSV;
        private static Image<Gray, byte>[] _channels;


        //histogram
        private static int[] _histogram;

        // Objects found in current image
        private static Rectangle[] _faces;
        private static Rectangle[] _eyes;

        // HaarObjects
        private static HaarCascade _haarEye;
        private static HaarCascade _haarFace;
        private static MCvAvgComp[] _objects;

        #endregion

        #region Public Methods

        // Loading new image in RGB, HSV and Gray. Initializing image related class members
        public static bool LoadImage(string imagePath)
        {
            DeallocateImageMemory();
                    
            _imageRGB = new Image<Bgr, byte>(imagePath);    //Read the files as an 8-bit RGB image
            _imageHSV = new Image<Hsv, byte>(imagePath);   //Read the files as an 8-bit HSV image
            _imageGray = new Image<Gray, byte>(imagePath);  //Read the files as an 8-bit gray image
            
            //!!!!!!!!!!!!!!
            Console.WriteLine("path: " + imagePath);
           // _imageGray.Save("C:\\img\\realpic.JPG");
           // MCvScalar sum = CvInvoke.cvSum(_imageGray);
           // int graySum = (int)(sum.v0);// (_imageGray.Rows * _imageGray.Cols));
           // Console.WriteLine("sum real: " + graySum);


            getHistogram();
       /*     try
            {
                //display the image 
                 ImageViewer viewer = new ImageViewer(); //create an image viewer
                 viewer.Image = _imageRGB;
                 viewer.Show();//show the image viewer
            }
            catch (Exception)
            {
                
                throw;
            }
           
        */
            _faces = null;
            _eyes = null;

            return true;
        }

        // Calculating avrage gray level
        public static double CalcAverageGrayLevel()
        {
            return _imageGray.GetAverage().Intensity;
        }

        // Calculating distance from the center of gravity in the loaded image, and returning the length by image 
        // length (mesured by main diagonal) percentage.
        public static int CalcTotalDistanceFromCenterOfGravity()
        {
            double centerOfGravityX;
            double centerOfGravityY;
            calcFacesCenterOfGravityByPercentage(out centerOfGravityX, out centerOfGravityY);

            // If no faces -> no center -> no distance
            if ((centerOfGravityX == -1) && (centerOfGravityY == -1))
                return -1;

            // Calc centers of faces
            int numberOfFaces = _faces.Length;
            Point[] centers = new Point[numberOfFaces];
            for (int i = 0; i < numberOfFaces; i++)
                centers[i] = new Point(_faces[i].X + (_faces[i].Width / 2), _faces[i].Y + (_faces[i].Height / 2));

            // Convert to percentage
            int imgWidth = _imageRGB.Width;
            int imgHeight = _imageRGB.Height;
            for (int i = 0; i < numberOfFaces; i++)
            {
                centers[i].X = ((centers[i].X *100)/ imgWidth);
                centers[i].Y = ((centers[i].Y *100)/ imgHeight); 
            }

            // Sum distance
            int sum = 0;
            for (int i = 0; i < centers.Length; i++)
                sum += (int)Math.Sqrt((Math.Pow(centerOfGravityX - centers[i].X, 2)) + (Math.Pow(centerOfGravityY - centers[i].Y,2)));

            return sum;
        }

        // Return the center of gravity value by percentage (reffering to X and Y axis sepratly)
        public static bool calcFacesCenterOfGravityByPercentage(out double x, out double y)
        {
            int width = _imageRGB.Width;
            int height = _imageRGB.Height;

            Point center = calcFacesCenterOfGravity();
            if ((center.X == -1) && (center.Y == -1))
            {
                x = -1;
                y = -1;
                return false;
            }
            else
            {
                x = (int)(center.X *100/ width);
                y = (int)(center.Y *100/ height);
                return true;
            }
        }

        // Calculating ratio between faces area in the image and the whole image area.
        public static double calcFacesImageAreaRatio()
        {
            // Find faces in the image if not yet found
            if (_faces == null)
                findFaces();

            // Calc total faces area
            double facesArea = 0;
            for (int i = 0; i < _faces.Length; i++)
                facesArea += (_faces[i].Width * _faces[i].Height);

            // Get image area
            double imageArea = _imageRGB.Width * _imageRGB.Height;

            // return ratio
            return (facesArea / imageArea);
        }

        // Counting number of faces in the image
        public static int calcNumOfPeople()
        {
            if (_faces == null)
                findFaces();

            Console.WriteLine("num of people: " + _faces.Length);
            // return num of faces
            return _faces.Length;
        }

        // Checking if red-eye exisits in the loaded image
        public static double isRedEye()
        {
            _eyes = null;
            findEyes();


            const double EYE_IMAGE_MARGIN_PERCENTAGE = 20.0;  // Margin in eye rectangle that redEye won't be searched
            const int DIVIDE_TO = 4;                          // Division to blocks for each dimention
            const int SUM_MULTIPLAYER = 1;                    // Multiplayer for condition

            const int HUE_MIN_RED_THRESHOLD = 336; //353
            const int HUE_MAX_RED_THRESHOLD = 13;  //12
            const double MIN_SATURATION_RED_THRESHOLD = 0.56; //0.56
            const double MAX_BRIGHTNESS_RED_THRESHOLD = 0.62; //0.62

            Rectangle ROI;                                    // Rectangle of the serached region.
            int marginX, marginY;                             // Margin length for X and Y axis
            int[,] cubes = new int[DIVIDE_TO, DIVIDE_TO];     // Matrix holding each block sum 
            int total, current;                               // Indicators
            Color c;                                          // Pixel color

            int max=0;

            // Find eyes if not yet found
            if (_eyes == null)
                findEyes();
            /*string file="C:/Users/Roe/Desktop/log1.txt";
            TextWriter log = new StreamWriter(file);
            log.WriteLine("55");
            //TextWriter log = new StreamWriter("C:/Users/Roe/Desktop/log.txt");
            log.WriteLine("");
            log.WriteLine(_eyes.Length);
            log.Close();*/
            //Write(log);

            // Look for redEye in each eye found
            for (int i = 0; i < _eyes.Length-1; i++)
            {
                total = 0;

                // Initialize matrix
                for (int x = 0; x < DIVIDE_TO; x++)
                    for (int y = 0; y < DIVIDE_TO; y++)
                        cubes[x, y] = 0;

                // Crop center of the image
                marginX = (int)(EYE_IMAGE_MARGIN_PERCENTAGE / 100 * _eyes[i].Width);
                marginY = (int)(EYE_IMAGE_MARGIN_PERCENTAGE / 100 * _eyes[i].Height);
                ROI = new Rectangle(_eyes[i].X + marginX, _eyes[i].Y + marginY, _eyes[i].Width - marginX, _eyes[i].Height - marginY);
                Bitmap eye = _imageRGB.Copy(ROI).ToBitmap();
                int cWidth = ((eye.Width % DIVIDE_TO) == 0) ? eye.Width / DIVIDE_TO : (eye.Width / DIVIDE_TO) + 1;
                int cHeight = ((eye.Height % DIVIDE_TO) == 0) ? eye.Height / DIVIDE_TO : (eye.Height / DIVIDE_TO) + 1;

                // Count 'red' pixels
                for (int x = 0; x < eye.Width; x++)
                    for (int y = 0; y < eye.Height; y++)
                    {
                        // Check if passing threshold
                        c = eye.GetPixel(x, y);
                        if (((c.GetHue() <= HUE_MAX_RED_THRESHOLD) || (c.GetHue() >= HUE_MIN_RED_THRESHOLD)) &&
                            (c.GetSaturation() > MIN_SATURATION_RED_THRESHOLD) && (c.GetBrightness() < MAX_BRIGHTNESS_RED_THRESHOLD))
                        {
                            cubes[x / cWidth, y / cHeight]++;
                            total++;
                        }
                    }

                // Look for spot
                /*for (int x = 0; x < DIVIDE_TO - 1; x++)
                    for (int y = 0; y < DIVIDE_TO - 1; y++)
                    {
                        // Check (current 4 Neighbor blocks sum):(rest of the blocks sum) ratio
                        current = cubes[x, y] + cubes[x + 1, y] + cubes[x, y + 1] + cubes[x + 1, y + 1];
                        if (current > SUM_MULTIPLAYER * (total - current))
                            return 1;
                    }*/

                for (int x = 0; x < DIVIDE_TO - 1; x++)
                {
                    for (int y = 0; y < DIVIDE_TO - 1; y++)
                    {
                        // Check (current 4 Neighbor blocks sum):(rest of the blocks sum) ratio
                        current = cubes[x, y] + cubes[x + 1, y] + cubes[x, y + 1] + cubes[x + 1, y + 1];
                        if (current > SUM_MULTIPLAYER * (total - current))
                            max+=1;
                    }
                    
                }
                return max;

            }
            return 0;
        }
      
        //!!!!!!!!!!!!!!!
        //blur parameter
        public static double blur()
        {          
            List<double> sumList = cropImg(_imageGray.Convert<Gray, float>());
            return calcSD(sumList); //calc standart diviation and return it
        }

        //!!!!!!!!!!!!!!!!!!!
        public static List<double> faceBlur()
        {
           
            
            // Find faces if not found yet
            if (_faces == null)
                findFaces();

            List<double> faceSumList = new List<double>();

            int j = 0;
            foreach (Rectangle currentFace in _faces)
            {
                // Crop face
                Image<Gray, byte> cropped = _imageGray.Copy(currentFace);
                Image<Gray, float> imgCrop = cropped.Convert<Gray, float>();
                faceSumList.Add(calcLaplacian(imgCrop));
                cropped.Save("C:\\img\\cropedPic" + j + ".JPG");
                j++;
            }

            return faceSumList;
            

            /*Image<Gray, float> imgCrop = _imageGray.Convert<Gray, float>();
            Rectangle sizeToCrop;

            // Find faces if not found yet
            if (_faces == null)
                findFaces();

            // Crop faces of each face rectangle
            int numberOfFaces = _faces.Length;

            /*if(numberOfFaces != 0)
            {
                  for (int i = 0; i < numberOfFaces; i++)
                  {
                      sizeToCrop = _faces[i];
                      imgCrop = _imageGray.Convert<Gray, float>().Copy(sizeToCrop);

                      imgCrop.Save("C:\\img\\cropedPic" + i + ".JPG");
                      Console.WriteLine("C:\\img\\cropedPic" + i + ".JPG");
                  }
            }*/
            
            /*if (numberOfFaces == 0)
            {
                //return new Point(-1, -1);
            }
            else
            if (numberOfFaces != 0)
            {
                // Calc centers of each face rectangle
                Point[] centers = new Point[numberOfFaces];
                Console.WriteLine("width: " + _imageGray.Width + " heigt: " + _imageGray.Height);
                Size size = new Size(_imageGray.Width/4, _imageGray.Height/4);
                Console.WriteLine("width: " + size.Width + " heigt: " + size.Height);
                for (int i = 0; i < numberOfFaces; i++)
                {
                    centers[i] = new Point(_faces[i].X + (_faces[i].Width / 2), _faces[i].Y + (_faces[i].Height / 2));
                    Console.WriteLine("x-center: " + centers[i].X + " y-center: " + centers[i].Y);
                    Point temp = new Point(centers[i].X - ((_imageGray.Width / 4)/2), centers[i].Y + ((_imageGray.Height / 4)/2));
                    Console.WriteLine("x-new: " + temp.X + " y-new: " + temp.Y);
                    if (temp.X + size.Width > _imageGray.Width && temp.Y + size.Height > _imageGray.Height)
                    {
                        size = new Size(_imageGray.Width / 4, _imageGray.Height / 4);
                    }
                    else if (temp.X + size.Width > _imageGray.Width)
                    {
                        size = new Size(_imageGray.Width - temp.X, _imageGray.Height / 4);
                    }
                    else if (temp.Y + size.Height > _imageGray.Height)
                    {
                        size = new Size(_imageGray.Width/4, _imageGray.Height - temp.Y);
                    }
                    sizeToCrop = new Rectangle(temp, size);
                    imgCrop = _imageGray.Convert<Gray, float>().Copy(sizeToCrop);
                    imgCrop.Save("C:\\img\\cropedPic" + i + ".JPG");
                    //Console.WriteLine("C:\\img\\cropedPic" + i + ".JPG");
                }
            }

            //dealllocate images
            if (imgCrop != null)
            {
                imgCrop.Dispose();
            }

            imgCrop = null;*/

        }


        //calculates laplacian using CV method - old
       /* public static int calcLaplacianIntegral(bool crop)
        {
            Image<Gray, float> imgLaplace = _imageGray.Convert<Gray, float>();

            if (crop)
            {
                 Rectangle sizeToCrop = new Rectangle(_imageGray.Cols / 4, _imageGray.Rows / 4, _imageGray.Cols /2, _imageGray.Rows /2);
            // Crop face
             imgLaplace = _imageGray.Convert<Gray, float>().Copy(sizeToCrop);
        //     imgLaplace.Save("C:\\img\\crp.jpg");
         
            }
            //smooth the image a little bit 3x3
            imgLaplace.SmoothGaussian(3);

            //calculates laplacian
            imgLaplace = imgLaplace.Laplace(19);

             //ImageViewer viewer = new ImageViewer(); //create an image viewer
             ////display the image
             //viewer.Image = imgLaplace;
             //viewer.Show();//show the image viewer
             //imgLaplace.Save("C:\\img\\notsharpLaplace1.JPG");
             

            //calculates the integral of the laplacian image
            MCvScalar sum = CvInvoke.cvSum(imgLaplace);
            int laplaceSum = (int)((sum.v0) / (imgLaplace.Rows * imgLaplace.Cols));


            //dealllocate images
            if (imgLaplace != null)
            {
                imgLaplace.Dispose();
            }

            imgLaplace = null;


            GC.Collect();
            return Math.Abs(laplaceSum);
        }*/


        //calculates RGB gray average gray levels seperately
        public static void calcRGBAverageGrayLevel(out double red, out double green, out double blue)
        {
            _channels = _imageRGB.Split();
            blue = _channels[0].GetAverage().Intensity;
            green = _channels[1].GetAverage().Intensity;
            red = _channels[2].GetAverage().Intensity;
        }

        //calculates HSV gray average gray levels seperately
        public static void calcHSVAverageGrayLevel(out double hue, out double saturation)
        {
            Image<Gray, byte>[] channels = _imageHSV.Split();
            hue = channels[0].GetAverage().Intensity;
            saturation = channels[1].GetAverage().Intensity;

        }

        //gets variance
        public static double getVariance()
        {
            double mean = _histogram.Sum() / _histogram.Length;
            double variance = 0;

            foreach (int gray in _histogram)
            {
                variance += Math.Pow((gray - mean), 2);
            }
            variance /= _histogram.Length;
            variance /= (_imageGray.Rows * _imageGray.Cols);
            return variance;

            //    double sqrMean = 0, mean = 0;
            //    double variance = 0;
            //    //calculate mean
            //    mean = _histogram.Sum() / _histogram.Length;
            //    for (int i = 0; i < _histogram.Length; i++)
            //    {
            //        sqrMean += Math.Pow(_histogram[i], 2);
            //    }
            //    sqrMean /= _histogram.Length;

            //    //for (int i = 0; i < _histogram.Length; i++)
            //    //{
            //    //    variance += Math.Pow((i - mean), 2) * _histogram[i];
            //    //}
            //    variance = sqrMean - Math.Pow(mean, 2)/ (_imageGray.Rows * _imageGray.Cols);
            //    return variance;
            //}
        }

        //gets amount of image information (hist*log(hist))
        public static double getImageInformation()
        {
            double info = 0;

            foreach (int gray in _histogram)
            {
                if (gray != 0)
                    info += gray * Math.Log(gray);
            }
            info = info / (_imageGray.Rows * _imageGray.Cols);
            return info;

        }

        /// <summary>
        /// claculates gradient
        /// </summary>
        /// <param name="crop">should crop image. (calculate gradient only for center.)</param>
        /// <returns>integer that represents gradient value</returns>
        public static int calcGradiant(bool crop)
        {

            Image<Gray, float> imgGradiant = _imageGray.Convert<Gray, float>();
            if (crop)
            {
                Rectangle sizeToCrop = new Rectangle(_imageGray.Cols / 4, _imageGray.Rows / 4, _imageGray.Cols /2, _imageGray.Rows /2);
            // Crop face
                imgGradiant = _imageGray.Convert<Gray, float>().Copy(sizeToCrop);
                //    imgGradiant.Save("C:\\img\\crp.jpg");
           
            }


            //smooth the image a little bit
            imgGradiant.SmoothGaussian(3);


            //create filters for differential by X and by Y
            float[,] filterY = {{ 1, 1, 1},
                                {-1,-1,-1}};
            float[,] filterX = {{1,-1},
                                {1,-1},
                                {1,-1}};

            ConvolutionKernelF convFilterY = new ConvolutionKernelF(filterY),
                               convFilterX = new ConvolutionKernelF(filterX);

            Image<Gray, float> diffY = imgGradiant.Convolution(convFilterY);//.Pow(2);
            Image<Gray, float> diffX = imgGradiant.Convolution(convFilterX);//.Pow(2);


            abs(ref diffX, ref diffY);
            // abs(ref diffY);
            imgGradiant = diffX.Add(diffY);

            /*     imgGradiant.Save("C:\\img\\notsharpGradient1.JPG");
                 ////display the image
              ImageViewer viewer = new ImageViewer(); //create an image viewer
                 viewer.Image = imgGradiant;
                 viewer.Show();//show the image viewer
     */

            //calculates the integral of the gradiant image
            MCvScalar sum = CvInvoke.cvSum(imgGradiant);
            int gradientSum = (int)(sum.v0 / (imgGradiant.Rows * imgGradiant.Cols));

            //dealllocate image
            if (imgGradiant != null)
            {
                imgGradiant.Dispose();
            }
            if (diffY != null)
            {
                diffY.Dispose();
            }
            if (diffX != null)
            {
                diffX.Dispose();
            }
            imgGradiant = null;
            diffX = null;
            diffY = null;
            GC.Collect();

            return gradientSum;
        }

        //calculates laplacian using filter. should be faster than other laplacian- doesn't work!
        /*public static int calcLaplacianIntegral2() 
         { 
             ImageViewer viewer = new ImageViewer(); //create an image viewer 

             Image<Gray, float> imgLaplace = _imageGray.Convert<Gray, float>(); 
   
              //smooth the image a little bit 
              imgLaplace.SmoothGaussian(3); 
   
              //create filters for differential by X and by Y 
              float[,] filterY = new float[3, 1]; 
              float[,] filterX = new float[1, 3]; 
              filterY[0, 0] = 1; 
              filterY[1, 0] = -2; 
              filterY[2, 0] = 1; 
   
              filterX[0, 2] = 1; 
              filterX[0, 1] = -2; 
              filterX[0, 0] = 1; 
   
   
   
              ConvolutionKernelF convFilterY = new ConvolutionKernelF(filterY), 
                                 convFilterX = new ConvolutionKernelF(filterX);

              Image<Gray, float> diffY = imgLaplace.Convolution(convFilterY);
              Image<Gray, float> diffX = imgLaplace.Convolution(convFilterX); 
   
   
              imgLaplace = diffX.Add(diffY);

          
              MCvScalar sum = CvInvoke.cvSum(imgLaplace);
              int laplaceSum = (int)(sum.v0); // (imgLaplace.Rows * imgLaplace.Cols));
              //dealllocate images 
              if (imgLaplace != null) 
              { 
                  imgLaplace.Dispose(); 
              } 
             
              imgLaplace = null; 
              GC.Collect();

              return laplaceSum;
          } 
        */
        /* public static void Write(TextWriter writeTo)
        {
            
                //ImageVector vector = new ImageVector(filePath, dictionary);
                writeTo.WriteLine("sdfsfd:");


                //for (int i = 0; i < ImageVector.NUMBER_OF_PARAMETERS; i++)
                //{
                   // ImageVector.ImageParameters currentParam = ImageVector.getParameterNameByIndex(i);
                    //if (dictionary[currentParam])
                findEyes();
                        writeTo.WriteLine("* " + _eyes.Length);
                //}

        }*/

        #endregion

        #region Private Methods

        //!!!!!!!!!!!!
        //crop pictures 16x16
        private static List<double> cropImg(Image<Gray, float> img)
        {
            const int DIVIDE_TO = 4;

            Image<Gray, float> imgCrop = img; //_imageGray.Convert<Gray, float>();
            Rectangle sizeToCrop;

            List<double> sumList = new List<double>();

           // Console.WriteLine("width: " + _imageGray.Width + "height: " + _imageGray.Height);
           // Console.WriteLine("width: " + img.Width + "height: " + img.Height);

            //crop each picture to 16 parts
            for (int i = 0; i < img.Height - img.Height / DIVIDE_TO + 1; i += img.Height / DIVIDE_TO)
            {
                for (int j = 0; j < img.Width - img.Width / DIVIDE_TO + 1; j += img.Width / DIVIDE_TO)
                {
                    //set size to crop and crop picture
                    sizeToCrop = new Rectangle(j, i, img.Width / DIVIDE_TO, img.Height / DIVIDE_TO);
                    imgCrop = img.Convert<Gray, float>().Copy(sizeToCrop);

                    //imgCrop.Save("C:\\img\\cropedPic" + i + "_" + j + ".JPG");
                    //Console.WriteLine("C:\\img\\cropedPic" + i + "_" + j + ".JPG");

                    sumList.Add(calcLaplacian(imgCrop));//, i, j));

                }
            }

            //dealllocate images
            if (imgCrop != null)
            {
                imgCrop.Dispose();
            }

            imgCrop = null;

            return sumList;

        }
        
        //!!!!!!!!!!!!!!!
        //calculate laplacian using CV method
        private static double calcLaplacian(Image<Gray, float> img)//, int i, int j)
        {
            //smooth the image a little bit 3x3
            img.SmoothGaussian(3);

            /*specifying aperture_size=1 gives the fastest variant that is equal to convolving the image 
            with the following kernel: |0 1 0| |1 -4 1| |0 1 0|*/


            //Bitmap bmp = img.Laplace(1).ToBitmap(img.Width, img.Height);
            //Image<Gray, float> imgLaplace = new Image<Gray, float>(bmp);

            Image<Gray, float> imgLaplace = img.Laplace(1);

            /*
             float[,] k = { { 0, 1, 0 }, 
                            { 1, -4, 1 },
                            { 0, 1, 0 } };

             ConvolutionKernelF kernel = new ConvolutionKernelF(k);
             Image<Gray, float> convoluted = img * kernel;
             if(imgLaplace.Equals(convoluted))
             Console.WriteLine("**************true******************");
            */

         //   imgLaplace.Save("C:\\img\\notsharpLaplace" + i + "_" + j + ".JPG");
         //  Console.WriteLine("C:\\img\\notsharpLaplace" + i + "_" + j + ".JPG");

            imgLaplace = imgLaplace.Pow(2);
            MCvScalar sum = CvInvoke.cvSum(imgLaplace);
            double laplaceSum = (double)(sum.v0 / (imgLaplace.Rows * imgLaplace.Cols));

            //Console.WriteLine("sumlap: " + Math.Abs(laplaceSum));

            //dealllocate images
            if (imgLaplace != null)
            {
                imgLaplace.Dispose();
            }

            imgLaplace = null;

            return Math.Abs(laplaceSum);

        }
        
        //!!!!!!!!!!!!
        /*calculates standart diviation by using it's formula
        * for a finite set of numbers X1,...,Xn the standard deviation is found by taking 
        * the square root of the average of the squared differences of the values from their 
        * average value: sqrt(1/n * sigma((Xi - avg)^2)), 1 < i < n
        */
        private static double calcSD(List<double> sumList)
        {
            double avg = sumList.Average();
            double sumOfSquares = 0;

            foreach (int val in sumList)
            {
                sumOfSquares += Math.Pow((val - avg), 2);
            }
            return Math.Sqrt(sumOfSquares / (sumList.Count - 1));
        }



        // Calculating the center of gravity of the faces in the loaded image
        private static Point calcFacesCenterOfGravity()
        {
            // Find faces if not found yet
            if (_faces == null)
                findFaces();

            // Calc centers of each face rectangle
            int numberOfFaces = _faces.Length;
            if (numberOfFaces == 0)
            {
                return new Point(-1, -1);
            }
            else
            {
                // Calc centers of each face rectangle
                Point[] centers = new Point[numberOfFaces];
                for (int i = 0; i < numberOfFaces; i++)
                    centers[i] = new Point(_faces[i].X + (_faces[i].Width / 2), _faces[i].Y + (_faces[i].Height / 2));

                // Sum all centers
                int avgX = 0;
                int avgY = 0;
                for (int i = 0; i < numberOfFaces; i++)
                {
                    avgX += centers[i].X;
                    avgY += centers[i].Y;
                }

                // Get center avrage
                avgX /= numberOfFaces;
                avgY /= numberOfFaces;

                // Return average
                return new Point(avgX, avgY);
            }
        }

        // Finding faces in the loaded image using haarCascade
        private static void findFaces()
        {
            // Haar parameters
            string path = Environment.CurrentDirectory.ToString() + "\\HaarCascade\\haarcascade_frontalface_default.xml";
            string HAAR_FACE_PATH = path;
            const int MIN_FACE_PIC_WIDTH_RATIO = 12; //old: 13 my good: 10
            const int MIN_FACE_PIC_HEIGHT_RATIO = 10; //old: 13 my good: 12
            const double FACE_SCALE_FACTOR = 1.05;//old: 1.1 my good:1.05
            const int FACE_MIN_NEIGHBORS = 5; //4

            // Load haar if not loaded
            if (_haarFace == null)
                _haarFace = new HaarCascade(HAAR_FACE_PATH);

            // Haar parameters objects
            Rectangle[] faces;
            Size faceMinSize = new Size(_imageGray.Width / MIN_FACE_PIC_WIDTH_RATIO, _imageGray.Height / MIN_FACE_PIC_HEIGHT_RATIO);

            // Do haar
            doHaarCascade(ref _imageGray, ref _haarFace, FACE_SCALE_FACTOR, FACE_MIN_NEIGHBORS, faceMinSize, out faces);

            // Save faces positions
            _faces = removeRedundant(faces);
        }

        // Finding eyes in the loaded image using haarCascade
        private static void findEyes()
        {

            // Check that faces positions exists
            if (_faces == null)
                findFaces();

            // Haar parameters
            string path = Environment.CurrentDirectory.ToString() + "\\HaarCascade\\haarcascade_eye.xml";
            string HAAR_EYE_PATH = path;
            const int MIN_EYE_PIC_WIDTH_RATIO = 8;
            const int MIN_EYE_PIC_HEIGHT_RATIO = 8;
            const double EYE_SCALE_FACTOR = 1.4;
            const int EYE_MIN_NEIGHBORS = 2;

            // Load haar if not loaded
            if (_haarEye == null)
                _haarEye = new HaarCascade(HAAR_EYE_PATH);

            // Haar parameters objects
            Rectangle[] eyes;
            Rectangle eye;
            Size eyeMinSize;


            // Find eyes in each face
            List<Rectangle> eyeList = new List<Rectangle>();
            int count = 0;

            foreach (Rectangle currentFace in _faces)
            {
                // Crop face
                Image<Gray, byte> cropped = _imageGray.Copy(currentFace);
                //cropped.Save("C:\\img\\tmp\\crp" + count.ToString() + ".jpg");
                count++;

                // Look for eyes
                eyeMinSize = new Size(cropped.Width / MIN_EYE_PIC_WIDTH_RATIO, cropped.Height / MIN_EYE_PIC_HEIGHT_RATIO);
                doHaarCascade(ref cropped, ref _haarEye, EYE_SCALE_FACTOR, EYE_MIN_NEIGHBORS, eyeMinSize, out eyes);
                eyes = removeRedundant(eyes);

                // List found eyes
                foreach (Rectangle currentEye in eyes)
                {
                    eye = new Rectangle(currentFace.X + currentEye.X, currentFace.Y + currentEye.Y, currentEye.Width, currentEye.Height);
                    eyeList.Add(eye);
                }
            }

            // Save all eyes positions
            _eyes = eyeList.ToArray();
        }

        // Returns a new list of rectangles which none is contained by another
        private static Rectangle[] removeRedundant(Rectangle[] list)
        {
            if (list.Length < 2)
                return list;

            int count = list.Length;
            bool[] ok = new bool[list.Length];

            for (int i = 0; i < ok.Length; i++)
                ok[i] = true;

            for (int i = 0; i < ok.Length; i++)
                for (int j = 0; j < ok.Length; j++)
                {
                    if (i == j)
                        continue;

                    if (list[i].Contains(list[j]) && ok[i])
                    {
                        ok[i] = false;
                        count--;
                    }
                }

            Rectangle[] newList = new Rectangle[count];
            count = 0;
            for (int i = 0; i < ok.Length; i++)
            {
                if (ok[i])
                {
                    newList[count] = list[i];
                    count++;
                }
            }

            return newList;
        }

        // Perform haarCascade with the given parameters
        private static void doHaarCascade(ref Image<Gray, byte> pic, ref HaarCascade haar, double scaleFactor, int minNeighbors, Size minSize, out Rectangle[] rects)
        {
            int count = 0;
            int index = 0;
            
            // Do HaarCascade
            _objects = pic.DetectHaarCascade(haar, scaleFactor, minNeighbors, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, minSize)[0];
            
            // Count findings
            foreach (var i in _objects)
                count++;

            // put into rects array
            rects = new Rectangle[count];
            foreach (var i in _objects)
                rects[index++] = i.rect;

            // Dealocate objects memory
            _objects = null;
            GC.Collect();
        }

        // Deallocates memory allocated for loading image in RGB, HSV, Gray Scale
        private static void DeallocateImageMemory()
        {
            // Deallocate unmanaged memory
            if (_imageRGB != null)
            {
                _imageRGB.Dispose();
                _imageRGB = null;
            }
            if (_imageHSV != null)
            {
                _imageHSV.Dispose();
                _imageHSV = null;
            }
            if (_imageGray != null)
            {
                _imageGray.Dispose();
                _imageGray = null;
            }
            if (_channels != null)
            {
                if (_channels[0] != null)
                {
                    _channels[0].Dispose();
                    _channels[0] = null;
                }
                if (_channels[1] != null)
                {
                    _channels[1].Dispose();
                    _channels[1] = null;
                }
                if (_channels[2] != null)
                {
                    _channels[2].Dispose();
                    _channels[2] = null;
                }
            }

            // Deallocate managed memory
            GC.Collect();



        }
       
        // Crop image
        //private static Image<Gray, float> cropGrayImage()
        //{
        //   // const int size = 4;
        //    Rectangle sizeToCrop = new Rectangle(_imageGray.Cols / 4, _imageGray.Rows / 4, _imageGray.Cols /2, _imageGray.Rows /2);
        //    // Crop face
        //    Image<Gray, float> cropped = _imageGray.Convert<Gray, float>().Copy(sizeToCrop);
        ////    cropped.Save("C:\\img\\crp.jpg");
        //    return cropped;
        //}

        //Abs
        private static void abs(ref Image<Gray, float> img1, ref Image<Gray, float> img2)
        {
            for (int i = 0; i < img1.Rows; i++)
                for (int j = 0; j < img1.Cols; j++)
                {
                    img1.Data[i, j, 0] = Math.Abs(img1.Data[i, j, 0]);
                    img2.Data[i, j, 0] = Math.Abs(img2.Data[i, j, 0]);
                }
        }

        //initializes histogram
        private static void getHistogram()
        {
            _histogram = new int[256];
            int bins = 256;
            int[] hsize = new int[1] { bins };
            int[] maxid = new int[1];
            int[] minid = new int[1];

            //ranges - grayscale 0 to 256
            float[] xranges = new float[2] { 0, 256 };
            IntPtr inPtr1 = new IntPtr(0);

            GCHandle gch1 = GCHandle.Alloc(xranges, GCHandleType.Pinned);

            try
            {
                inPtr1 = gch1.AddrOfPinnedObject();
            }
            finally
            {
                gch1.Free();
            }
            IntPtr[] ranges = new IntPtr[1] { inPtr1 };

            //planes to obtain the histogram, in this case just one
            IntPtr[] planes = new IntPtr[1] { _imageGray.Ptr };
            IntPtr hist;
            //get the histogram and some info about it
            hist = CvInvoke.cvCreateHist(1, hsize, Emgu.CV.CvEnum.HIST_TYPE.CV_HIST_ARRAY, ranges, 1);
            CvInvoke.cvCalcHist(planes, hist, false, IntPtr.Zero);


            //go over histogram and put it into class property
            for (int i = 0; i < hsize[0]; i++)
            {
                _histogram[i] = (int)CvInvoke.cvQueryHistValue_1D(hist, i);
            }
        }

        #endregion
    }
}
