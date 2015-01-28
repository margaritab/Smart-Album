using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace SPF
{
    
    [Serializable]
    class ImageVector
    {
        public const int NUMBER_OF_PARAMETERS = 16;                 // Number of all possible parameters
        public const int GOOD_IMAGE = 1;
        //!!!!!!!!!!!!!!!!
        public const int BAD_IMAGE = 0; //was BAD_IMAGE = -1;
        private string _path;                                       // The path of the picture represented by this vector

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }
        private double[] _parameterVector;                          // Array holding parameters values
        private Dictionary<ImageParameters, bool> _DicParameter;    // Enabled parameters mask

        /* All posible image parameters */
        public enum ImageParameters
        {
            averageGrayLevel = 0,
            numOfPoeple = 1,
            edges = 2,
            facesCenterOfGravityX = 3,  //shows whether the people shot in the picture are in the center
            facesCenterOfGravityY = 4,  //shows whether the people shot in the picture are in the center
            facesImageAreaRatio = 5,    //how close to the camera the people in the picture are 
            averageRedLevel = 6,
            averageBlueLevel = 7,
            averageGreenLevel = 8,
            averageHueLevel = 9,
            averageSaturationLevel = 10, 
            distanceFromGravityCenter = 11, //indicates how close to the camera the people in the pictures are
            imageInformation = 12,
            variance = 13,
            redEye = 14,
            standartDiviation = 15, //!!!!!!!!! 
            unknown
        }

        internal Dictionary<ImageParameters, bool> DicParameter
        {
            get { return _DicParameter; }
            set { _DicParameter = new Dictionary<ImageParameters, bool>(value); }
        }

        /* Return image parameter by its index */
        public static ImageParameters getParameterNameByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return ImageParameters.averageGrayLevel;
                case 1:
                    return ImageParameters.numOfPoeple;
                case 2:
                    return ImageParameters.edges;
                case 3:
                    return ImageParameters.facesCenterOfGravityX;
                case 4:
                    return ImageParameters.facesCenterOfGravityY;
                case 5:
                    return ImageParameters.facesImageAreaRatio;
                case 6:
                    return ImageParameters.averageRedLevel;
                case 7:
                    return ImageParameters.averageBlueLevel;
                case 8:
                    return ImageParameters.averageGreenLevel;
                case 9:
                    return ImageParameters.averageHueLevel;
                case 10:
                    return ImageParameters.averageSaturationLevel;
                case 11:
                    return ImageParameters.distanceFromGravityCenter;
                case 12:
                    return ImageParameters.imageInformation;
                case 13:
                    return ImageParameters.variance;
                case 14:
                    return ImageParameters.redEye;
                case 15:
                    return ImageParameters.standartDiviation; //!!!!!!!!
                default:
                    return ImageParameters.unknown;
            }
        }

        /* Return an given parameter of current vector */
        public double getParameter(ImageParameters param)
        {
            int paramIndex = (int)param;
            return _parameterVector[paramIndex];
        }

        /* Return an given parameter of current vector */
        public double getParameterByIndex(int index)
        {
            return _parameterVector[index];
        }

        public string getAllParameters(bool isGood)
        {
            string s = Convert.ToString(_parameterVector[0]);
            for (int i = 1; i < NUMBER_OF_PARAMETERS; i++)
                s = s + "," + _parameterVector[i];
            if (isGood)
                s = s + "," + 1;
            else
                s = s + "," + 0;
            return s;
        }
        /* Settign parameter value */
        public void setParameter(ImageParameters param, double value)
        {
            int paramIndex = (int)param;
            _parameterVector[paramIndex] = value;
        }

        /* Copy constructor for image vector */
        public ImageVector(ImageVector other)       
        {
            Path = other.Path;
            _DicParameter = other._DicParameter;
            _parameterVector = new double[NUMBER_OF_PARAMETERS];
            for (int i = 0; i < _parameterVector.Length; i++)
                _parameterVector[i] = other._parameterVector[i];
        }

        public ImageVector(double[] other)
        {
            _parameterVector = new double[NUMBER_OF_PARAMETERS];
            for (int i = 0; i < other.Length; i++)
                _parameterVector[i] = other[i];
        }
  
        /* Constructor: Builds the vector that describes the image from imagepath. */
        public ImageVector(string imagePath, Dictionary<ImageParameters,bool> parameterDic)    
        {
            // save image path
            Path = imagePath;

            // construct new array
            _parameterVector = new double[NUMBER_OF_PARAMETERS];

            // Copy parameter dictionary
            //DicParameter = new Dictionary<ImageParameters, bool>(parameterDic);

            // Try to load image to imageProcessing class
            try
            {              
                ImageProcessing.LoadImage(imagePath);
            }
            catch (Exception exc)
            {
                //MessageBox.Show(exc.Message);
            }

            // Get RGB, H&S values if needed
            double r = -1,
                  g = -1,
                  b = -1,
                  h = -1,
                  s = -1;

            ImageProcessing.calcRGBAverageGrayLevel(out r, out g, out b);
            ImageProcessing.calcHSVAverageGrayLevel(out h, out s);


            _parameterVector[(int)ImageParameters.averageGrayLevel] = ImageProcessing.CalcAverageGrayLevel();
            _parameterVector[(int)ImageParameters.numOfPoeple] = ImageProcessing.calcNumOfPeople();
            _parameterVector[(int)ImageParameters.edges] = ImageProcessing.calcGradiant(true);
            _parameterVector[(int)ImageParameters.averageRedLevel] = r;
            _parameterVector[(int)ImageParameters.averageGreenLevel] = g;
            _parameterVector[(int)ImageParameters.averageBlueLevel] = b;
            _parameterVector[(int)ImageParameters.averageHueLevel] = h;
            _parameterVector[(int)ImageParameters.averageSaturationLevel] = s;
            _parameterVector[(int)ImageParameters.imageInformation] = ImageProcessing.getImageInformation();
            _parameterVector[(int)ImageParameters.variance] = ImageProcessing.getVariance();
            _parameterVector[(int)ImageParameters.redEye] = ImageProcessing.isRedEye();
            double x, y;
            ImageProcessing.calcFacesCenterOfGravityByPercentage(out x, out y);
            _parameterVector[(int)ImageParameters.facesCenterOfGravityX] = x;
            _parameterVector[(int)ImageParameters.facesCenterOfGravityY] = y;
            _parameterVector[(int)ImageParameters.facesImageAreaRatio] = ImageProcessing.calcFacesImageAreaRatio();
            _parameterVector[(int)ImageParameters.distanceFromGravityCenter] = ImageProcessing.CalcTotalDistanceFromCenterOfGravity();
           // List<double> sumList = ImageProcessing.cropImg();
            _parameterVector[(int)ImageParameters.standartDiviation] = ImageProcessing.blur();   
        
            //!!!!!!!!!!!!!!!!!
           // ImageProcessing.faceBlur();

            //double sd = ImageProcessing.calcSD(sumList);
           // Console.WriteLine("sd: " + _parameterVector[(int)ImageParameters.standartDiviation]);

            /*List<double> faceSumList = ImageProcessing.faceBlur();
            foreach(double facesum in faceSumList)
            {
                Console.WriteLine("sum: " + facesum);
            }*/
           /* List<double> faceSumList = ImageProcessing.faceBlur();
            if (faceSumList.Count != 0)
            {
                if (faceSumList.Count == 1)
                {
                    Console.WriteLine("sum: " + faceSumList.First());
                }
                else
                {
                    double fsd = ImageProcessing.calcSD(faceSumList);
                    Console.WriteLine("sd: " + fsd);
                }

            }*/
            
            //calculate standart diviation
            double sd = _parameterVector[(int)ImageParameters.standartDiviation];
            Console.WriteLine("sd: " + sd);
            
            
            if (sd > 0 && sd < 2)
                smartAlbum.sd02++;
            if (sd > 2)
                smartAlbum.sd2plus++;
            
           
            Console.WriteLine("count sd: 0-2:" + smartAlbum.sd02 + " ,2+:" + smartAlbum.sd2plus);          
            
            
        }

        /* Return a string that describe current image vector*/
        public override String ToString()
        {
            string str = String.Empty;

            for (int i = 0; i < NUMBER_OF_PARAMETERS; i++)
                str += i.ToString() + ". " + getParameterByIndex(i).ToString() + ": " + _parameterVector[i].ToString() + " \n";

            return str;
        }

    }
}
