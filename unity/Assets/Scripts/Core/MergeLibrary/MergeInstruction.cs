using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Core.MergeLibrary
{
    public class MergeInstruction: MonoBehaviour
    {
        [SerializeField] private GameObject imageIngredientItem;
        [SerializeField] private GameObject imageResultItem;
        [SerializeField] private GameObject arrowImage;
        [SerializeField] private GameObject plusImage;
        [SerializeField] private float instructionHeight = 180f;
        
        public void SetupItem(List<Item.Item> itemOnLeft, Item.Item itemOnRight)
        {
            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = instructionHeight;
            layoutElement.preferredHeight = instructionHeight;

            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, instructionHeight);
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            
            for (int i = 0; i < itemOnLeft.Count; i++)
            {
                if (i > 0 && plusImage != null)
                {
                    Instantiate(plusImage, this.transform);
                }

                // Left Item
                var goItem = Instantiate(imageIngredientItem, this.transform);
                goItem.transform.GetComponent<MergeItem>().SetupItem(itemOnLeft[i]);
            }
            
            Instantiate(arrowImage, this.transform);
            
            // Right Item
            var goItemRight = Instantiate(imageResultItem, this.transform);
            goItemRight.transform.GetComponent<MergeItem>().SetupItem(itemOnRight, true);
        }
    }
}