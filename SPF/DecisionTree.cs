using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.ML;
using Emgu.CV;
using Emgu.CV.ML.Structure;
using System.Runtime.InteropServices;

namespace SPF
{
    class DecisionTree : LearningAlgorithm
    {
        private DTree _tree;

        public DecisionTree() : base()
        {
            _tree = new DTree();
        }

        public override bool Train(List<ImageVector> VectorsTrue, List<ImageVector> VectorsFalse)
        {
            // Convert to clasifier vector
            string[] cVectorsTrue, cVectorsFalse;
            Classifier.Classify(VectorsTrue, VectorsFalse, out cVectorsTrue, out cVectorsFalse);

            // exmaple at public.cranfield.ac.uk/c5354/teaching/ml/examples/speech_ex/decisiontree.cc
            // Read data from vectors
            Matrix<float> data, response;
            convertDataVectorsToMatrix(cVectorsTrue, cVectorsFalse, out data, out response);

            // Use the first 100% of data as training sample
            int trainingSampleCount = (int)(data.Rows);

            // Variable types are categorical
            Matrix<Byte> varType = new Matrix<byte>(data.Cols + 1, 1);
            varType.SetValue((byte)Emgu.CV.ML.MlEnum.VAR_TYPE.CATEGORICAL); 

            // prior class probabilities
            float[] priors = null;
            priors = new float[cVectorsTrue.Length+cVectorsFalse.Length];

            for (int i = 0; i < priors.Length; i++)
            {
                priors[i] = (float)(1.2 * i+1.3);
            }
            GCHandle priorsHandle = GCHandle.Alloc(priors, GCHandleType.Pinned);

            // Tree paramaters (see www.cognotics.com/opencv/docs/1.0/ref/opencvref_ml.htm#ch_dtree)
            MCvDTreeParams param = new MCvDTreeParams();
            param.maxDepth = 20;                    // Tree maximum depth
            param.minSampleCount = 1;               // Minimum samples for one node to be split
            param.regressionAccuracy = 0;           // Unused
            param.useSurrogates = false;             // Used for Variable imporatnce estimation and missing parameter handeling
            param.maxCategories = 6;               // Maximum categories for each parameter           
            param.cvFolds = 0;                     // k-folds cross validation. 
            param.use1seRule = false;               // If used, the tree is pruned a bit more, making the tree resistate to noise but less acurate
            param.truncatePrunedTree = false;       // Pruning
            param.priors = priorsHandle.AddrOfPinnedObject();   //
            
         
            // Train
            bool success = _tree.Train(data, Emgu.CV.ML.MlEnum.DATA_LAYOUT_TYPE.ROW_SAMPLE,
                response, null, null, varType, null, param);

            return (success);
        }

        public override bool Predict(List<ImageVector> Vectors, out double[] results)
        {
            string[] cVectors = new string[Vectors.Count];
            for (int i = 0; i < Vectors.Count; i++)
                cVectors[i] = Classifier.ClassifyVector(Vectors[i]);

            Matrix<float> sample;
            byte result;
            results = new double[cVectors.Length];

            
            for (int i = 0; i < cVectors.Length; i++)
            {
                
                // Conver vector i to matrix
                converSampleToMatrix(cVectors[i], out sample);

                // Predict

                result = (byte)_tree.Predict(sample, null, false).value;

                if (result == 0)
                    results[i] = 1000;
                
                //results[i] = (result == 0) ? 0 : 1; /**change here the result**/
               /* if (result == 1)
                {
                    results[i]= find_weight_distance(cVectors[i], sample, data);
                }*/ 
                //results[i] = i * 0.2;
                

            }
            return true;
        }

        public override bool SaveData(string name)
        {
            try
            {
                _tree.Save("tree-" + name);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        public override bool LoadData(string name)
        {
            try
            {
                _tree.Load("tree-" + name);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        private void convertDataVectorsToMatrix(string[] strTrue, string[] strFalse, out Matrix<float> mat, out Matrix<float> response)
        {
            int propCount = strTrue[0].Length;
            int trueVectorCount = strTrue.Length;
            int falseVectorCount = strFalse.Length;
            int vectorCount = trueVectorCount + falseVectorCount;
            mat = new Matrix<float>(vectorCount,propCount);
            response = new Matrix<float>(vectorCount, 1);

            for (int v = 0; v < trueVectorCount; v++)
                for (int p = 0; p < propCount; p++)
                {
                    mat[v, p] = (byte)strTrue[v][p];
                    response[v, 0] = System.Convert.ToInt32(1);
                }
            for (int v = trueVectorCount; v < vectorCount; v++)
                for (int p = 0; p < propCount; p++)
                {
                    mat[v, p] = (byte)strFalse[v - trueVectorCount][p];
                    response[v, 0] = System.Convert.ToInt32(0);
                }
        }

        private void converSampleToMatrix(String str, out Matrix<float> samples)
        {
            int propCount = str.Length;

            samples = new Matrix<float>(1, propCount);

            for (int p = 0; p < propCount; p++)
                samples[0, p] = (byte)str[p];
        }

        /*private bool find_(String str, Matrix<float> sample, Matrix<float> data)
        {

        }*/

        public static void calc_remainder(double[] how_much_pictures, double[] how_much_good, int good_pictures, int bad_pictures, ref double[] remainder)
        {
            double positive = 0;
            double negetive = 0;
            int size = good_pictures + bad_pictures;
            for (int i = 0; i < ImageVector.NUMBER_OF_PARAMETERS; i++)
            {
                function_I(i * 2, how_much_pictures, how_much_good, out positive, out negetive);
                //System.Windows.Forms.MessageBox.Show("pos: " + good_pictures.ToString() +" "+ size.ToString()+ "neg: " + bad_pictures.ToString()+ "pos: " +positive.ToString());
                Classifier.remainder[i] = ((((double)good_pictures / (double)size) * positive) + (((double)bad_pictures / (double)size) * negetive));
            }

            for (int i = 0; i < ImageVector.NUMBER_OF_PARAMETERS; i++)
            {
                if (Classifier.remainder[i] < 0)
                    Classifier.remainder[i] = Math.Abs(Classifier.remainder[i]);
                if (Classifier.remainder[i] > 1)
                    Classifier.remainder[i] = Classifier.remainder[i] - (int)Classifier.remainder[i];
                //remainder[i] = order_remainder[i];
                //System.Windows.Forms.MessageBox.Show(i.ToString() + ": " + remainder[i].ToString());

                Classifier.order_remainder[i] = 1 - Classifier.remainder[i];
            }
        }//eof calc_remainder

        private static void function_I(int i, double[] how_much_pictures, double[] how_much_good, out double positive, out double negetive)
        {

            if (how_much_pictures[i] == 0)
                how_much_pictures[i] = 1;
            if (how_much_pictures[i + 1] == 0)
                how_much_pictures[i + 1] = 1;

            double p = (how_much_good[i] / (how_much_pictures[i])); //good values divide good pictures
            double n = (how_much_good[i + 1] / (how_much_pictures[i]));//bad values divide good pictures
            if (n == 0)
                n = 1;
            positive = -(p * Math.Log(p, 2)) - (n * Math.Log(n, 2));

            p = (how_much_good[i] / (how_much_pictures[i + 1])); //good values divide bad pictures
            n = (how_much_good[i + 1] / (how_much_pictures[i + 1])); //bad values divide bad pictures
            if (n == 0)
                n = 1;
            negetive = Math.Abs((-p * Math.Log(p, 2)) - (n * Math.Log(n, 2)));
            //System.Windows.Forms.MessageBox.Show("p: "+p.ToString() + "positive:" + positive.ToString()+ "n: "+ n.ToString()+ "negetaive: "+ negetive.ToString());
        }//eof functio
       
    }
}
