using System;
using System.Collections.Generic;
using System.Linq;
using Core.Computer.PathoNet;
using Core.Interaction;
using Core.MergeLibrary;
using Core.Scene;
using Core.SFX;
using Framework.Controller;
using UnityEngine;

namespace Core.OnBoarding
{
    public class OnBoardingController : BaseController<OnBoardingController>
    {
        public int actualOnBoardingIndex = 0;

        public OnBoardingData GetActualOnBoarding()
        {
            return OnBoardingDatabase.Instance.OnBoardings[actualOnBoardingIndex];
        }

        [SerializeField]
        private BrewInteractable brewInteractable;
        [SerializeField]
        private CookInteractable cookInteractable;
        [SerializeField]
        private PathoNetInterface pathoNetInterface;
        [SerializeField]
        private SendItemInteractable sendItemInteractable;
        [SerializeField]
        private RecipeListInteractable recipeListInteractable;

        [SerializeField] private GameObject libraryInteractableObject;
        [SerializeField] private GameObject brewInteractableObject;
        [SerializeField] private GameObject cookInteractableObject;
        [SerializeField] private GameObject computerObject;
        [SerializeField] private GameObject cartObject;
        [SerializeField] private GameObject sendItemInteractableObject;

        [SerializeField] private float wavySpeed = 2f;
        [SerializeField] private float wavyAmplitude = 2f;

        private int _itemToBuy1Count = 0;
        private int _itemToBuy2Count = 0;
        private List<Outline> _currentActiveOutlines = new List<Outline>();
        private Dictionary<Outline, float> _baseWidths = new Dictionary<Outline, float>();
        private Dictionary<Outline, Color> _originalColors = new Dictionary<Outline, Color>();
        private bool _eventsRegistered;

        private void Pop(GameObject[] objects)
        {
            SFXController.Instance.PlayInteraction(SFXDatabase.Instance.popUiClip);
            
            foreach (var obj in objects)
            {
                obj.SetActive(true);
                obj.transform.localScale = Vector3.zero;
                LeanTween.scale(obj, Vector3.one, 0.35f).setEaseSpring();
            }

            SetWavyOutline(objects);
        }

        private void SetWavyOutline(GameObject[] objects)
        {
            foreach (var outline in _currentActiveOutlines)
            {
                StopWavyOutline(outline);
            }

            _currentActiveOutlines.Clear();
            _baseWidths.Clear();
            _originalColors.Clear();

            foreach (var obj in objects)
            {
                var outlines = obj.GetComponentsInChildren<Outline>(true);
                foreach (var outline in outlines)
                {
                    _currentActiveOutlines.Add(outline);
                    _baseWidths[outline] = outline.OutlineWidth;
                    _originalColors[outline] = outline.OutlineColor;
                }
            }

            float startTime = Time.time;
            foreach (var outline in _currentActiveOutlines)
            {
                StartWavyOutline(outline, startTime);
            }
        }

        private void StartWavyOutline(Outline outline, float startTime)
        {
            outline.OutlineColor = Color.white;
            float baseWidth = _baseWidths[outline];
            
            LeanTween.value(outline.gameObject, 0f, 1f, wavySpeed)
                .setLoopPingPong()
                .setEase(LeanTweenType.easeInOutSine)
                .setOnUpdate((float val) =>
                {
                    outline.OutlineWidth = baseWidth - (val * wavyAmplitude);
                });
        }

        private void StopWavyOutline(Outline outline)
        {
            if (outline != null)
            {
                LeanTween.cancel(outline.gameObject);
                if (_baseWidths.ContainsKey(outline))
                {
                    outline.OutlineWidth = _baseWidths[outline];
                }
                if (_originalColors.ContainsKey(outline))
                {
                    outline.OutlineColor = _originalColors[outline];
                }
                else
                {
                    outline.OutlineColor = Color.black;
                }
            }
        }

        public void Start()
        {
            computerObject.SetActive(false);
            cookInteractableObject.SetActive(false);
            sendItemInteractableObject.SetActive(false);
            cartObject.SetActive(false);
            brewInteractableObject.SetActive(false);
            computerObject.transform.localScale = Vector3.zero;
            cookInteractableObject.transform.localScale = Vector3.zero;
            sendItemInteractableObject.transform.localScale = Vector3.zero;
            cartObject.transform.localScale = Vector3.zero;
            brewInteractableObject.transform.localScale = Vector3.zero;

            Pop(new GameObject[] { libraryInteractableObject });
            
            OnBoardingInterface.Instance.ShowActualBoard();

            recipeListInteractable.OnInteractRecipeList += OnRecipeListInteracted;
            sendItemInteractable.onItemSent += OnItemSent;
            brewInteractable.onItemMerged += OnItemMerged;
            cookInteractable.onItemCooked += OnItemCooked;
            pathoNetInterface.OnBuyItem += OnItemBought;
            _eventsRegistered = true;
        }

        public void OnDestroy()
        {
            if (!_eventsRegistered)
                return;

            recipeListInteractable.OnInteractRecipeList -= OnRecipeListInteracted;
            sendItemInteractable.onItemSent -= OnItemSent;
            brewInteractable.onItemMerged -= OnItemMerged;
            cookInteractable.onItemCooked -= OnItemCooked;
            pathoNetInterface.OnBuyItem -= OnItemBought;
        }

        private void OnRecipeListInteracted()
        {
            var boardingData = GetActualOnBoarding();
            if (boardingData.onBoardingState != OnBoardingState.OpenLibrary)
                return;

            Pop(new GameObject[] { computerObject, cartObject });
            GoNextOnBoarding();
        }

        private void OnItemSent(Core.Item.Item item)
        {
            var boardingData = GetActualOnBoarding();
            if (boardingData.onBoardingState == OnBoardingState.SendItem &&
                item == OnBoardingDatabase.Instance.itemToSent)
            {
                GoNextOnBoarding();
            }
        }

        private void OnItemMerged(Core.Item.Item item)
        {
            var boardingData = GetActualOnBoarding();
            if (boardingData.onBoardingState == OnBoardingState.MergeItems &&
                item == OnBoardingDatabase.Instance.itemToMerge)
            {
                Pop(new GameObject[] { sendItemInteractableObject });
                GoNextOnBoarding();
            }
        }

        private void OnItemCooked(Core.Item.Item item)
        {
            var boardingData = GetActualOnBoarding();
            if (boardingData.onBoardingState == OnBoardingState.CookItem &&
                item == OnBoardingDatabase.Instance.itemToCook)
            {
                Pop(new GameObject[] { brewInteractableObject });
                GoNextOnBoarding();
            }
        }

        private void OnItemBought(Core.Item.Item item)
        {
            var boardingData = GetActualOnBoarding();
            if (boardingData.onBoardingState != OnBoardingState.BuyItems ||
                (_itemToBuy1Count == 1 && _itemToBuy2Count == 1))
                return;

            if (item == OnBoardingDatabase.Instance.itemToBuy1)
                _itemToBuy1Count++;

            if (item == OnBoardingDatabase.Instance.itemToBuy2)
                _itemToBuy2Count++;

            if (_itemToBuy1Count == 1 && _itemToBuy2Count == 1)
            {
                GoNextOnBoarding();
                Pop(new GameObject[] { cookInteractableObject });
            }
        }

        public void GoNextOnBoarding()
        {
            if (actualOnBoardingIndex >= OnBoardingDatabase.Instance.OnBoardings.Count) return;
            actualOnBoardingIndex++;
            if (actualOnBoardingIndex >= OnBoardingDatabase.Instance.OnBoardings.Count)
            {
                FinishOnBoarding();
                return;
            }
            OnBoardingInterface.Instance.ShowActualBoard();
        }

        public void FinishOnBoarding()
        {
            var newScene = SceneDatabase.Instance.GetSceneByName("Game");
            SceneTransitor.Instance.LoadScene(newScene);
        }
    }
}
