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
    class DecisionTreeNumerical : LearningAlgorithm
    {
        private DTree _tree;

        public DecisionTreeNumerical() : base()
        {
            _tree = new DTree();
        }

        public override bool Train(List<ImageVector> vectorsTrue, List<ImageVector> vectorsFalse)
        {
            // Convert to clasifier vector (Convert only images that was given in the method parameters)
            double[,] VectorsArrTrue, VectorsArrFalse;
            VectorsArrTrue = new double[vectorsTrue.Count, 15];
            VectorsArrFalse = new double[vectorsFalse.Count, 15];
            for (int i = 0; i < vectorsTrue.Count; i++)
                for (int j = 0; j < 15; j++)
                    VectorsArrTrue[i, j] = (float)vectorsTrue[i].getParameterByIndex(j);
            for (int i = 0; i < vectorsFalse.Count; i++)
                for (int j = 0; j < 15; j++)
                    VectorsArrFalse[i, j] = (float)vectorsFalse[i].getParameterByIndex(j);

            // exmaple at public.cranfield.ac.uk/c5354/teaching/ml/examples/speech_ex/decisiontree.cc

            // Read data from vectors
            Matrix<float> data, response;
            convertDataVectorsToMatrix(VectorsArrTrue, VectorsArrFalse, out data, out response);

            // Use the first 100% of data as training sample
            int trainingSampleCount = (int)(data.Rows);

            // Variable types are categorical
            Matrix<Byte> varType = new Matrix<byte>(data.Cols + 1, 1);
            varType.SetValue((byte)Emgu.CV.ML.MlEnum.VAR_TYPE.NUMERICAL);

            // prior class probabilities
            float[] priors = null;
            GCHandle priorsHandle = GCHandle.Alloc(priors, GCHandleType.Pinned);

            // Tree paramaters (see www.cognotics.com/opencv/docs/1.0/ref/opencvref_ml.htm#ch_dtree)
            MCvDTreeParams param = new MCvDTreeParams();
            param.maxDepth = 20;                    // Tree maximum depth
            param.minSampleCount = 1;               // Minimum samples for one node to be split
            param.regressionAccuracy = 0;           // Unused
            param.useSurrogates = false;             // Used for Variable imporatnce estimation and missing parameter handeling
            param.maxCategories = 10;               // Maximum categories for each parameter           
            param.cvFolds = 0;                     // k-folds cross validation. 
            param.use1seRule = false;               // If used, the tree is pruned a bit more, making the tree resistate to noise but less acurate
            param.truncatePrunedTree = false;       // Pruning
            param.priors = priorsHandle.AddrOfPinnedObject();   //

            // Train
            bool success = _tree.Train(data, Emgu.CV.ML.MlEnum.DATA_LAYOUT_TYPE.ROW_SAMPLE,
                response, null, null, varType, null, param);

            return (success);
        }

        public override bool Predict(List<ImageVector> VectorsList, out double[] results)
        {
            double[,] Vectors = new double [VectorsList.Count, ImageVector.NUMBER_OF_PARAMETERS];
            for (int i = 0; i < VectorsList.Count; i++)
            {
                for (int j = 0; j < 15; j++)
                    Vectors[i, j] = VectorsList[i].getParameterByIndex(j);
            }

            Matrix<float> sample;
            byte result;
            results = new double[Vectors.GetLength(0)];
            double[] doubleVector = new double[Vectors.GetLength(1)];

            for (int i = 0; i < Vectors.GetLength(0); i++)
            {
                for (int j = 0; j < Vectors.GetLength(1); j++)
                    doubleVector[j] = (double)Vectors[i, j];

                // Conver vector i to matrix
                converSampleToMatrix(doubleVector, out sample);

                // Predict
                result = (byte)_tree.Predict(sample, null, false).value;
                results[i] = (result == 0) ? 0 : 1; /** change here the result**/
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

        private void convertDataVectorsToMatrix(double[,] True, double[,] False, out Matrix<float> mat, out Matrix<float> response)
        {
            int propCount = True.GetLength(1);
            int trueVectorCount = True.GetLength(0);
            int falseVectorCount = False.GetLength(0);
            int vectorCount = trueVectorCount + falseVectorCount;
            mat = new Matrix<float>(vectorCount,propCount);
            response = new Matrix<float>(vectorCount, 1);

            for (int v = 0; v < trueVectorCount; v++)
                for (int p = 0; p < propCount; p++)
                {
                    mat[v, p] = (byte)True[v,p];
                    response[v, 0] = (float)(1);
                }
            for (int v = trueVectorCount; v < vectorCount; v++)
                for (int p = 0; p < propCount; p++)
                {
                    mat[v, p] = (byte)False[v - trueVectorCount,p];
                    response[v, 0] = (float)(0);
                }
        }

        private void converSampleToMatrix(double[] arr, out Matrix<float> samples)
        {
            int propCount = arr.Length;

            samples = new Matrix<float>(1, propCount);

            for (int p = 0; p < propCount; p++)
                samples[0, p] = (byte)arr[p];
        }

    }
}
