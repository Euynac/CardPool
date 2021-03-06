using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CardPool.Core
{
    internal class BinarySearchLine
    {
        /// <summary>
        /// Because the card in search line are all at the right of the index, there is a remaining card at the leftmost.
        /// </summary>
        public Card LeftMostCard { get; init; }
        /// <summary>
        /// Card's probability interval line. Every single probability index are the corresponding card's
        /// probability interval beginning (included for current card) and the previous card's ending
        /// (excluded for previous card).
        /// </summary>
        public KeyValuePair<double, Card>[] CardsBinarySearchLine  { get; init; }
        
        /// <summary>
        /// Search from all cards.
        /// </summary>
        /// <param name="probability"></param>
        /// <returns></returns>
        public Card Search(double probability)
        {
            return BinarySearch(probability, 0, CardsBinarySearchLine.Length - 1);
        }
        /// <summary>
        /// Search in specific index interval.
        /// </summary>
        /// <param name="probability">The probability starts from included 0 to excluded 1, and it will automatically
        /// map to corresponding probability in the specific index interval</param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        public Card Search(double probability, int startIndex, int endIndex)
        {
            var min = CardsBinarySearchLine.ElementAt(startIndex).Key;
            var max = CardsBinarySearchLine.ElementAt(endIndex).Key;
            var correctionProbability = probability * (max - min) + min;
            return BinarySearch(correctionProbability, startIndex, endIndex);
        }
        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        private Card BinarySearch(double probability, int startIndex, int endIndex)
        {
            if (startIndex > endIndex) throw new Exception("startIndex is larger than endIndex");
            while (true)
            {
                if (startIndex == endIndex)
                {
                    var curIndexProbability = CardsBinarySearchLine[startIndex].Key;
                    if (curIndexProbability <= probability) return CardsBinarySearchLine.ElementAt(startIndex).Value;
                    if (curIndexProbability > probability)
                    {
                        return startIndex == 0 ? 
                            LeftMostCard : 
                            CardsBinarySearchLine[startIndex-1].Value;
                    }
                }
                var middle = (endIndex + startIndex) / 2;
                if (CardsBinarySearchLine[middle].Key > probability)
                {
                    endIndex = middle - 1;
                }
                else
                {
                    startIndex = middle + 1;
                }
                if (endIndex < startIndex) endIndex = startIndex;
            }
        }
    }
}