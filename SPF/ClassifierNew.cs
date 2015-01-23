using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPF
{
    class ClassifierNew
    {
        // Bounds used for classifing each parameter

        public static double[] CENTER_OF_GRAVITY_BOUNDS = { 0, 33, 66 };
        public static double[] DISTANCE_FROM_COG_BOUNDS = { 0, 15, 50, 100, 200 };
        public static double[] IMAGE_FACES_AREA_RATIO_BOUNDS = { 0.0001, 0.1, 0.3, 0.6, 1 };
        public static double[] RED_EYE_BOUNDS = { 0.5 };
        public static double[] AVERAGE_GRAY_LEVEL_BOUNDS = { 80, 100, 120, 150 };
        public static double[] AVERAGE_RED_LEVEL_BOUNDS = { 80, 100, 120, 160 };
        public static double[] AVERAGE_GREEN_LEVEL_BOUNDS = { 80, 110, 150 };
        public static double[] AVERAGE_BLUE_LEVEL_BOUNDS = { 80, 110, 150 };
        public static double[] AVERAGE_HUE_LEVEL_BOUNDS = { 30, 50, 80 };
        public static double[] AVERAGE_SATURATION_LEVEL_BOUNDS = { 50, 80, 110, 150 };
        public static double[] NUM_OF_PEOPLE_BOUNDS = { 1, 3, 5 };
        public static double[] EDGES_BOUNDS = { 10, 30, 70, 120 };
        public static double[] IMAGE_INFORMATION_BOUNDS = { 7.8, 8, 8.5 };
        public static double[] VARIANCE_BOUNDS = { 5, 10, 20, 60 };
        public static double[] STANDART_DIVIATION = { 2, 10 };
        public static double[] MIN_MAX = { 30, 60 };

        
        int numOfParameters;
       
        
        public ClassifierNew()
        {
          
            this.numOfParameters = ImageVector.NUMBER_OF_PARAMETERS;
          
        }


        public void initArray(double[] array)
        {
            for (int i = 0; i < numOfParameters; i++)
            {
              
                array[i] = 0;

            
            }
        
        }
        /*return the size of bound Parameter array*/
        public int sizeRange(ImageVector.ImageParameters param)
        {
            double[] temp = new double[0];
            getBoundArray(param, ref temp);
            return temp.Length;
        }

        public double[] calacPositiveNegative(ref double[] attributeRange,double value, ImageVector.ImageParameters param)
        {

           
            double[] ParamBounds = new double[0];
            getBoundArray(param, ref ParamBounds);
            int paramBoundsLength = ParamBounds.Length;

            //first bound
            if (value <= ParamBounds[0])
                attributeRange[0] = attributeRange[0] + 1;

            for (int i = 1; i < paramBoundsLength; i++)
            {
                if (value <= ParamBounds[i] && value > ParamBounds[i-1])
                    attributeRange[i] = attributeRange[i] + 1;   
            }
          //last bound
            if (value > ParamBounds[ParamBounds.Length - 1])
                attributeRange[attributeRange.Length - 1] = attributeRange[attributeRange.Length - 1] + 1;
           
         /*   for (int k = 0; k < paramBoundsLength; k++)
            {
                Console.WriteLine("attributeRange[" + k + "] : " + attributeRange[k]);
            }
           */
            return attributeRange;         
        }

        /*Remainder(A) = sum( k=0 to d) of ((pk+nk)/p+n)*B(pk/(pk+nk))
         when attribute A has d distinct valuse divides the traning set
         for example : gray level d = 5 0-80,80-100,100-120,120-150
         in the book: Ptrons: d=3 , none,some,full*/
        public void calcRemainder(ref double remainder, double[] numOfGood, double[] numOfBad, int size)
        {

            for (int i = 0; i < numOfGood.Length; i++) // numOfGood.length = numOfBad.length = sizeOfRange+1
            {
                double q;
                double B;
                double sumGoodBad = (numOfGood[i] + numOfBad[i]);
                if (sumGoodBad == 0)
                    q = 0;
                else
                    q = numOfGood[i] / sumGoodBad;

                if (q == 0 || q == 1)
                    B = 0;
                else
                    B = -q * Math.Log(q, 2) - (1 - q) * Math.Log(1 - q, 2);

                remainder += (sumGoodBad / size) * B;

            }            
            
        }

        /*the calculation of Gain give as the information about the Importance Attribute
         * the bigger Gain is the importante the attribute is
         Gain(A) = B(p/(p+n)) - remainder(A)*/
        public void calcGain(ref double gain,double remainder,double numGood, double numBad)
        {
            double q = numGood / (numGood + numBad);
            double B;//entropy
            if(q == 0 || q == 1)
                B = 0;
            else
               B = - (q * Math.Log(q, 2)) - ((1 - q) * Math.Log(1 - q, 2));
          
            for (int i = 0; i < numOfParameters; i++)
            {
                gain = B - remainder;
            }
             
            
        
        }

     /*   public void print(double arr)
        {
            for (int i = 0; i < numOfParameters; i++)
            Console.WriteLine(i + " - "+ arr[i]);
        
        }
        */




        /* Get classification bounds array of given parameter*/

      
        public static void getBoundArray(ImageVector.ImageParameters param, ref double[] array)
        {
            array = null;
            switch (param)
            {
                case ImageVector.ImageParameters.averageGrayLevel:
                    array = AVERAGE_GRAY_LEVEL_BOUNDS;
                    break;
                case ImageVector.ImageParameters.averageGreenLevel:
                    array = AVERAGE_GREEN_LEVEL_BOUNDS;
                    break;
                case ImageVector.ImageParameters.averageRedLevel:
                    array = AVERAGE_RED_LEVEL_BOUNDS;
                    break;
                case ImageVector.ImageParameters.averageBlueLevel:
                    array = AVERAGE_BLUE_LEVEL_BOUNDS;
                    break;
                case ImageVector.ImageParameters.averageHueLevel:
                    array = AVERAGE_HUE_LEVEL_BOUNDS;
                    break;
                case ImageVector.ImageParameters.averageSaturationLevel:
                    array = AVERAGE_SATURATION_LEVEL_BOUNDS;
                    break;
                case ImageVector.ImageParameters.numOfPoeple:
                    array = NUM_OF_PEOPLE_BOUNDS;
                    break;
                case ImageVector.ImageParameters.edges:
                    array = EDGES_BOUNDS;
                    break;
                case ImageVector.ImageParameters.redEye:
                    array = RED_EYE_BOUNDS;
                    break;
                case ImageVector.ImageParameters.distanceFromGravityCenter:
                    array = DISTANCE_FROM_COG_BOUNDS;
                    break;
                case ImageVector.ImageParameters.facesImageAreaRatio:
                    array = IMAGE_FACES_AREA_RATIO_BOUNDS;
                    break;
                case ImageVector.ImageParameters.facesCenterOfGravityX:
                    array = CENTER_OF_GRAVITY_BOUNDS;
                    break;
                case ImageVector.ImageParameters.facesCenterOfGravityY:
                    array = CENTER_OF_GRAVITY_BOUNDS;
                    break;
                case ImageVector.ImageParameters.variance:
                    array = VARIANCE_BOUNDS;
                    break;
                case ImageVector.ImageParameters.imageInformation:
                    array = IMAGE_INFORMATION_BOUNDS;
                    break;
                case ImageVector.ImageParameters.standartDiviation:
                    array = STANDART_DIVIATION;
                    break;
                case ImageVector.ImageParameters.minMax:
                    array = MIN_MAX;
                    break;
                default:
                    throw (new Exception("Classification for " + param.ToString() + " is not implemented"));
            }
        }
    }
}
