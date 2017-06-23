// Copyright (c) 2017 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class DistributionCore
    {
        public DistributionCore()
        {
        }

        public enum BalanceAction
        {
            LoseWeight,
            GainWeight
        }

        internal class Tuple
        {
            int _first, _second;

            public Tuple(int first, int second)
            {
                _first = first;
                _second = second;
            }

            public int First
            {
                get { return _first; }
            }

            public int Second
            {
                get { return _second; }
            }
        }

        //
        // In case a member joins, each node must contribute its share of buckets to grant and weight to divide.
        //
        public static ArrayList Distribute(ArrayList currentMap, int bucketsPerNode, int weightPerNode)
        {
            return null;
        }

        //
        // In case a member leaves, new bucketMap needs to share the  orphan buckets among existing nodes.
        //

        public static ArrayList Merge(ArrayList currentMap, int bucketsPerNode)
        {
            return null;
        }

        //
        // This would create N separate lists for N number of nodes.
        //

        public static ArrayList FilterBucketsByNode(Hashtable currentMap)
        {
            return null;
        }

        //We have the matrix. We need no weight balancing, but we need shuffled indexes to be selected. Here comes the routine and algo.
        // Shuffle works the way of moving diagonally within a matrix. If we reach end column, move to first row but extended column.
        public static RowsBalanceResult ShuffleSelect(DistributionMatrix bMatrix)
        {
            RowsBalanceResult rbResult = new RowsBalanceResult();
            int[] selectedBuckets = new int[bMatrix.MatrixDimension.Cols];
            for (int i = 0; i < bMatrix.MatrixDimension.Cols; )
            {
                for (int j = 0; j < bMatrix.MatrixDimension.Rows && i < bMatrix.MatrixDimension.Cols; j++, i++)
                {
                    if (bMatrix.Matrix[j, i] == -1)
                    {
                        break;
                    }
                    else
                    {
                        selectedBuckets[i] = (j * bMatrix.MatrixDimension.Cols) + i;
                    }

                }
            }
            rbResult.ResultIndicies = selectedBuckets;
            rbResult.TargetDistance = 0;
            return rbResult;
        }

        //returns selected array of indicies that are the resultant set to be sacrificed
        public static RowsBalanceResult CompareAndSelect(DistributionMatrix bMatrix)
        {
            //First Pass:: Check if any individual row can fulfill the reqs .
            int rowNum = IndividualSelect(bMatrix);

            if (rowNum >= 0)
            {
                int[] selectedBuckets = new int[bMatrix.MatrixDimension.Cols];
                for (int i = 0; i < bMatrix.MatrixDimension.Cols; i++)
                {
                    selectedBuckets[i] = (rowNum * bMatrix.MatrixDimension.Cols) + i;
                }
                RowsBalanceResult rbResult = new RowsBalanceResult();
                rbResult.ResultIndicies = selectedBuckets;
                rbResult.TargetDistance = Math.Abs(bMatrix.WeightPercentMatrix[rowNum, 1] - bMatrix.WeightToSacrifice);
                return rbResult;
            }
            else //Second pass Compare all pairs.
            {
                ArrayList allTuples = CandidateTuples(bMatrix);
                RowsBalanceResult rbResultCurr, rbResultToKeep;
                rbResultCurr = null; rbResultToKeep = null;

                foreach (Tuple pair in allTuples)
                {
                    rbResultCurr = BalanceWeight(pair, bMatrix);
                    if (rbResultToKeep == null)
                        rbResultToKeep = rbResultCurr;

                    //If the current result is more optimized then previous then current is the candidate.
                    if (rbResultCurr.TargetDistance < rbResultToKeep.TargetDistance)
                        rbResultToKeep = rbResultCurr;
                }
                return rbResultToKeep;
            }            
        }

        private static int IndividualSelect(DistributionMatrix bMatrix)
        {
            for (int i = 0; i < bMatrix.MatrixDimension.Rows; i++)
            {
                long rowWeight = bMatrix.WeightPercentMatrix[i, 0];
                if (rowWeight == bMatrix.PercentWeightToSacrifice)
                    return i; //this row;

                //Lets see if if required data is within +- of cushion factor
                if (rowWeight < bMatrix.PercentWeightToSacrifice)
                {
                    rowWeight += bMatrix.CushionFactor;
                    if (rowWeight >= bMatrix.PercentWeightToSacrifice)
                        return i; //this row                
                }
                else
                {
                    rowWeight -= bMatrix.CushionFactor;
                    if (rowWeight <= bMatrix.PercentWeightToSacrifice)
                        return i; //this row                
                }
            }
            return -1; //We got no single row that can be selected under required criteria
        }

        //Backbone to be written now.//Two arrays ....both need to give a right combination against required weight.
        private static RowsBalanceResult BalanceWeight(Tuple rowPair, DistributionMatrix bMatrix)
        {
            BalanceAction balAction = new BalanceAction();
            long weightToMove = 0;
            long primaryRowWeight = bMatrix.WeightPercentMatrix[rowPair.First, 0];
            long secondaryRowWeight = bMatrix.WeightPercentMatrix[rowPair.Second, 0];

            if (primaryRowWeight < bMatrix.PercentWeightToSacrifice) //
            {
                weightToMove = bMatrix.PercentWeightToSacrifice - primaryRowWeight;
                weightToMove = (weightToMove * bMatrix.TotalWeight) / 100;
                balAction = BalanceAction.GainWeight;
            }
            else
            {
                weightToMove = primaryRowWeight - bMatrix.PercentWeightToSacrifice;
                weightToMove = (weightToMove * bMatrix.TotalWeight) / 100;
                balAction = BalanceAction.LoseWeight;
            }

            long[] primaryRowData = new long[bMatrix.MatrixDimension.Cols];
            long[] secondaryRowData = new long[bMatrix.MatrixDimension.Cols];

            //Fills the local copy of two rows to be manipulated
            for (int i = 0; i < bMatrix.MatrixDimension.Cols; i++)
            {
                primaryRowData[i] = bMatrix.Matrix[rowPair.First, i];
                secondaryRowData[i] = bMatrix.Matrix[rowPair.Second, i];
            }
            RowsBalanceResult rbResult= null;
            switch (balAction)
            {
                case BalanceAction.GainWeight:                    
                    rbResult = RowBalanceGainWeight(rowPair, primaryRowData, secondaryRowData, weightToMove, bMatrix);
                    break;
                case BalanceAction.LoseWeight:
                    rbResult = RowBalanceLoseWeight(rowPair, weightToMove, bMatrix);
                    break;
                default:
                    break;
            }

            return rbResult;
        }


        //All 2-tuples of  the given set. Set is array of %weights against each row.
        private static ArrayList CandidateTuples(DistributionMatrix bMatrix)
        {
            // here we'll have n choose r scenario. Where we need to choose all possible pairs from the given set
            // for n choose r
            int n = bMatrix.MatrixDimension.Rows;
            int r = 2;
            int tupleCount = (int)(Factorial(n) / (Factorial(r) * Factorial(n - r)));
            ArrayList listTuples = new ArrayList(tupleCount);

            for (int i = 0; i < bMatrix.MatrixDimension.Rows; i++)
            {
                for (int j = i + 1; j < bMatrix.MatrixDimension.Rows; j++)                    
                    listTuples.Add(new Tuple(i, j));
            }
            return listTuples;
        }

        //Balances two rows in a way that primary need to add some more weight to get the resultant weight.

        private static RowsBalanceResult RowBalanceGainWeight(Tuple rowPair, long[] primaryRowData, long[] secondaryRowData, long weightToGain, DistributionMatrix bMatrix)
        {
            int[] primaryIndicies = new int[bMatrix.MatrixDimension.Cols];
            int[] secondaryIndicies = new int[bMatrix.MatrixDimension.Cols];
            int  tmpIndex, primaryLockAt, secondaryLockAt;            
            long primaryRowWeight = bMatrix.WeightPercentMatrix[rowPair.First, 1];
            long secondaryRowWeight = bMatrix.WeightPercentMatrix[rowPair.Second, 1];
            long weightToAchieve, primaryDistance, secondaryDistance, tmpWeightPri, tmpWeightSec, weightDifference;

            bool primarySelect = false;
            primaryLockAt = -1;
            secondaryLockAt = -1;
            primaryDistance = 0;
            secondaryDistance = 0;
            bool bSecondaryNeedsToLoose = true;
            //total weight to be made 
            weightToAchieve = primaryRowWeight + weightToGain;

            RowsBalanceResult rbResult = new RowsBalanceResult();

            // for example first-row weight = 1000, second-row weight = 2000, required weight = 3000, 
            // in this case second row need not to lose weight, so no need to keep it as a candidate.
            if (secondaryRowWeight < weightToAchieve)
                bSecondaryNeedsToLoose = false;

            //lets first populated indicies list for each row.This would help in geting the final set of indicies.
            for (int i = 0; i < bMatrix.MatrixDimension.Cols; i++)
            {
                primaryIndicies[i] = (rowPair.First * bMatrix.MatrixDimension.Cols) + i;
                secondaryIndicies[i] = (rowPair.Second * bMatrix.MatrixDimension.Cols) + i;
            }


            //in this loop I am checking both ways. The one that needs to gain weight and the one that looses 
            //weight in result. So any row can match the required weight. After each loop, each swap I check
            //for the criteria against both rows. In the end I get two indexes against both rows along with
            //possible extra/deficient count.
            //In Loose weight, primary is already high from the target,so no chance of secondary 

            for (int i = 0; i < bMatrix.MatrixDimension.Cols; i++)
            {
                tmpWeightPri = primaryRowData[i];
                tmpWeightSec = secondaryRowData[i];

                weightDifference = tmpWeightSec - tmpWeightPri;

                primaryRowWeight += weightDifference;
                secondaryRowWeight -= weightDifference;

                if (primaryRowWeight > weightToAchieve && primaryLockAt == -1)
                {
                    long diffAfterSwap = primaryRowWeight - weightToAchieve;
                    long diffBeforeSwap = weightToAchieve - (primaryRowWeight - weightDifference);
                    if (diffAfterSwap >= diffBeforeSwap)
                    {
                        primaryLockAt = i - 1;
                        primaryDistance = diffBeforeSwap;
                    }
                    else
                    {
                        primaryLockAt = i;
                        primaryDistance = diffAfterSwap;
                    }
                }

                //Do secondary really needs to loose weight ?? Not all the time.
                
                if (secondaryRowWeight < weightToAchieve && secondaryLockAt == -1 && bSecondaryNeedsToLoose)
                {
                    long diffAfterSwap = weightToAchieve - secondaryRowWeight;
                    long diffBeforeSwap = (secondaryRowWeight + weightDifference) - weightToAchieve;

                    if (diffAfterSwap >= diffBeforeSwap)
                    {
                        secondaryLockAt = i - 1;
                        secondaryDistance = diffBeforeSwap;
                    }
                    else
                    {
                        secondaryLockAt = i;
                        secondaryDistance = diffAfterSwap;
                    }
                }
            }

            if (primaryLockAt != -1 && secondaryLockAt != -1) //if we found both rows be candidates then select one with less error
            {
                if (primaryDistance <= secondaryDistance)
                    primarySelect = true;
                else
                    primarySelect = false;
            }
            else
            {
                if (primaryLockAt != -1)
                    primarySelect = true;

                if (secondaryLockAt != -1)
                    primarySelect = false;
            }

            //unfortunately we found nothing ... So give the first row back with overhead value
            if (primaryLockAt == -1 && secondaryLockAt == -1)
            {
                primarySelect = true;
                primaryDistance = weightToAchieve - primaryRowWeight;
            }


            int swapCount = (primarySelect == true) ? primaryLockAt : secondaryLockAt;

            //do the items swapping according to swap count value
            for (int i = 0; i <= swapCount; i++)
            {
                tmpIndex = primaryIndicies[i];
                primaryIndicies[i] = secondaryIndicies[i];
                secondaryIndicies[i] = tmpIndex;
            }

            if (primarySelect == true)
            {
                rbResult.ResultIndicies = primaryIndicies;
                rbResult.TargetDistance = primaryDistance;
            }
            else
            {
                rbResult.ResultIndicies = secondaryIndicies;
                rbResult.TargetDistance = secondaryDistance;
            }

            return rbResult;
        }


        //Balances two rows in a way that primary need to lose some weight to get the resultant weight.
        //As secondary is always higher weight then primary, so primary cant lose weight. This makes it 
        //straight forward.

        private static RowsBalanceResult RowBalanceLoseWeight(Tuple rowPair, long weightToLose, DistributionMatrix bMatrix)
        {
            int[] primaryIndicies = new int[bMatrix.MatrixDimension.Cols];

            RowsBalanceResult rbResult = new RowsBalanceResult();
            //lets first populated indicies list for each row.This would help in geting the final set of indicies.
            for (int i = 0; i < bMatrix.MatrixDimension.Cols; i++)
                primaryIndicies[i] = (rowPair.First * bMatrix.MatrixDimension.Cols) + i;

            rbResult.ResultIndicies = primaryIndicies;
            rbResult.TargetDistance = weightToLose;

            return rbResult;
        }

        /// <summary>
        /// Returns the factorial of any UInt64 less than 22
        /// </summary>
        /// <param name="n">The number to get a factorial for.</param>
        /// <returns>The factorial of n.</returns>
        /// <remarks>Throws an exception if the number passed is greater than 21</remarks>
        public static long Factorial(int n)
        {
            if (n < 0) { return -1; }    //error result - undefined
            if (n > 256) { return -2; }  //error result - input is too big

            if (n == 0) { return 1; }

            // Calculate the factorial iteratively rather than recursively:

            long tempResult = 1;
            for (int i = 1; i <= n; i++)
            {
                tempResult *= i;
            }
            return tempResult;
        }

    }
}
