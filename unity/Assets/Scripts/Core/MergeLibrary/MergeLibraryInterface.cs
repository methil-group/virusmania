using System.Collections.Generic;
using System.Linq;
using Core.Item.Cook;
using Core.Item.Merge;
using Core.Input;
using Framework.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Core.MergeLibrary
{
    public class MergeLibraryInterface : InterfaceController<MergeLibraryInterface>
    {
        public RectTransform mergeRecipeTransform;
        public RectTransform boilingRecipeTransform;

        [SerializeField] private GameObject instructionUiPrefab;
        [SerializeField] private bool onlyOnBoarding = false;
        
        public override void Start()
        {
            base.Start();
        }

        public void CreateRecipes()
        {
            for (int i = mergeRecipeTransform.childCount - 1; i >= 0; i--)
            {
                var child = mergeRecipeTransform.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            for (int i = boilingRecipeTransform.childCount - 1; i >= 0; i--)
            {
                var child = boilingRecipeTransform.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            
            MergeDatabase mergeDatabase = MergeDatabase.Instance;

            var mergeList = mergeDatabase.Database
                .FindAll(mergeRecipe => mergeRecipe.isOnBoarding == onlyOnBoarding);
            if (onlyOnBoarding == false) { mergeList = mergeDatabase.Database; }
            foreach (MergeRecipeBase mergeRecipe in mergeList)
            {
                if (mergeRecipe is MergeVirusRecipe mergeVirusRecipe)
                {
                    var go = Instantiate(instructionUiPrefab, mergeRecipeTransform);
                    go.name = "Instruction for : " + mergeRecipe.name;
                    go.GetComponent<MergeInstruction>().SetupItem(
                        mergeVirusRecipe.inputItems.ToList().Cast<Item.Item>().ToList(), 
                        mergeRecipe.GetResultItem().Item
                        );
                }
            }

            var cookingList = CookDatabase.Instance.Database
                .FindAll(cookRecipe => cookRecipe.isOnBoarding == onlyOnBoarding);
            if (onlyOnBoarding == false) { cookingList = CookDatabase.Instance.Database; }
            foreach (CookRecipe cookRecipe in cookingList)
            {
                var go = Instantiate(instructionUiPrefab, boilingRecipeTransform);
                go.name = "Instruction for : " + cookRecipe.name;
                go.GetComponent<MergeInstruction>().SetupItem(
                    new List<Item.Item>()
                    {
                        cookRecipe.inputItem 
                    } , 
                    cookRecipe.resultItem
                );
            }

            RebuildLayouts();
        }

        public void RebuildLayouts()
        {
            Canvas.ForceUpdateCanvases();
            foreach (Transform child in mergeRecipeTransform)
            {
                if (child is RectTransform rt)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            foreach (Transform child in boilingRecipeTransform)
            {
                if (child is RectTransform rt)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(mergeRecipeTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(boilingRecipeTransform);
            if (mergeRecipeTransform.parent is RectTransform parentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }

        public override void OpenPanel()
        {
            base.OpenPanel();
            CreateRecipes();
            GamepadNavigation.SelectFirstSelectable(panel);
        }

        public override void ClosePanel()
        {
            GamepadNavigation.ClearSelection(panel);
            base.ClosePanel();
        }
    }
}
