using System.Collections.Generic;
using Core.Item.Holder;
using UnityEngine;

namespace Core.Item.Merge
{
    [CreateAssetMenu(fileName = "MergeRecipe", menuName = "Item/MergeRecipe")]
    public class MergeRecipe<TInput, TResult> : MergeRecipeBase where TInput: Item where TResult: Item
    {
        [Header("Items to merge")]
        public TInput[] inputItems;

        [Header("Resulting item")]
        public TResult resultItem;

        public override bool Matches(Item[] items)
        {
            if (items == null || inputItems == null || items.Length != inputItems.Length) return false;

            var remainingItems = new List<Item>(items);
            foreach (var input in inputItems)
            {
                int matchingIndex = remainingItems.FindIndex(item => item == input);
                if (matchingIndex < 0)
                    return false;

                remainingItems.RemoveAt(matchingIndex);
            }

            return true;
        }

        public override HoldItem GetResultItem()
        {
            return resultItem.GetHoldItem();
        }
    }
}
