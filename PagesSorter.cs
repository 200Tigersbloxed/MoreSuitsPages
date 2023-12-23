using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MoreSuits;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MoreSuitsPages
{
    internal class Pages : SuitSorter
    {
        private static GameObject PageButton;
        private static GameObject PageText;

        private static TMP_FontAsset StolenFont;
        private static Sprite StolenHandIcon;
        private static int InteractLayer;
        private static GameObject CurrentPageTextObject;
        private static TMP_Text CurrentPageText;
        
        public static int CurrentPage;
        private static List<UnlockableSuit[]> SuitsPages = new List<UnlockableSuit[]>();

        private static GameObject rightPageButton;
        private static GameObject leftPageButton;

        public readonly bool Loaded;

        public Pages()
        {
            try
            {
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("MoreSuitsPages.moresuits");
                AssetBundle assetBundle = AssetBundle.LoadFromStream(s);
                if (assetBundle != null)
                {
                    Object[] objects = assetBundle.LoadAllAssets<Object>();
                    PageButton = objects.First(x => x.name == "PageButton") as GameObject;
                    PageText = objects.First(x => x.name == "PageText") as GameObject;
                    Loaded = true;
                }
                else
                    Logger.LogWarning("Failed to load PageButton Asset! Page buttons will not appear.");
            }
            catch (Exception)
            {
                Logger.LogWarning("Failed to load Embedded AssetBundle! Page buttons will not appear.");
            }
        }

        internal void Update()
        {
            if (!IsCurrentSorter) return;
            bool needsRefresh = false;
            MoreSuitsMod.CachedSuits.ForEach(x =>
            {
                if(x != null && x.gameObject != null) return;
                needsRefresh = true;
            });
            if(needsRefresh) HardReset(StartOfRound.Instance);
            if(CurrentPageTextObject == null || CurrentPageText == null) return;
            CurrentPageText.text = $"Page {CurrentPage + 1} of {SuitsPages.Count}";
        }

        public override void SwitchedToSorter(bool isGameScene)
        {
            if(!isGameScene) return;
            StartOfRound startOfRound = StartOfRound.Instance;
            if (startOfRound != null)
                OnGameSceneLoaded(startOfRound);
            HardReset(startOfRound);
        }

        public override void SwitchedAwayFromSorter(bool isGameScene)
        {
            if(!isGameScene) return;
            if(rightPageButton != null)
                Object.Destroy(rightPageButton);
            if(leftPageButton != null)
                Object.Destroy(leftPageButton);
            if(CurrentPageTextObject != null)
                Object.Destroy(CurrentPageTextObject);
            SuitsPages.ForEach(x =>
            {
                foreach (UnlockableSuit unlockableSuit in x)
                {
                    if(unlockableSuit == null || unlockableSuit.gameObject == null) continue;
                    unlockableSuit.gameObject.SetActive(true);
                }
            });
            HardReset(StartOfRound.Instance);
        }

        public override void OnGameSceneLoaded(StartOfRound startOfRound)
        {
            GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            // Steal Font
            GameObject sys = rootGameObjects.First(x => x.name == "Systems");
            StolenFont = sys.transform.Find("UI/Canvas/EndgameStats/Text/HeaderText").GetComponent<TextMeshProUGUI>()
                .font;
            // Steal Icon
            GameObject env = rootGameObjects.First(x => x.name == "Environment");
            Transform ts = env.transform.Find("HangarShip/Terminal/TerminalTrigger/TerminalScript");
            StolenHandIcon = ts.GetComponent<InteractTrigger>().hoverIcon;
            InteractLayer = ts.gameObject.layer;
            //Transform rightRack = GetFarRightRack();
            Transform rightRack = startOfRound.rightmostSuitPosition;
            // Clone Button for Right
            rightPageButton = Object.Instantiate(PageButton, rightRack, true);
            rightPageButton.name = "NextPageButton";
            // Clone Button for Left
            leftPageButton = Object.Instantiate(PageButton, rightRack, true);
            leftPageButton.name = "PreviousPageButton";
            // Create Interactable and Register
            CreateAndRegisterInteract(rightPageButton, "Next", () =>
            {
                CurrentPage += 1;
                RenderPage();
            });
            CreateAndRegisterInteract(leftPageButton, "Previous", () =>
            {
                CurrentPage -= 1;
                RenderPage();
            });
            // Position
            rightPageButton.transform.localPosition = new Vector3(0, -0.5f, -0.6f);
            leftPageButton.transform.localPosition = new Vector3(0, -0.5f, 2.6f);
            // Text
            TMP_Text leftPageText = leftPageButton.transform.GetChild(0).GetComponent<TMP_Text>();
            TMP_Text rightPageText = leftPageButton.transform.GetChild(0).GetComponent<TMP_Text>();
            leftPageText.text = "<";
            leftPageText.font = StolenFont;
            rightPageText.font = StolenFont;
            // Label
            GameObject pageText = Object.Instantiate(PageText, rightRack, true);
            pageText.name = "PageText";
            pageText.transform.localPosition = new Vector3(0, 0.4f, 1);
            CurrentPageTextObject = pageText;
            CurrentPageText = pageText.GetComponent<TMP_Text>();
            CurrentPageText.font = StolenFont;
        }

        public override void SortSuitRack(StartOfRound startOfRound, UnlockableSuit[] suits)
        {
            int index = 0;
            int offset = 0;
            foreach (UnlockableSuit suit in suits)
            {
                AutoParentToShip component = suit.gameObject.GetComponent<AutoParentToShip>();
                component.overrideOffset = true;

                if (offset > MoreSuitsMod.SUITS_PER_RACK - 1)
                    offset = 0;
                
                component.positionOffset = new Vector3(-2.45f, 2.75f, -8.41f) + startOfRound.rightmostSuitPosition.forward * 0.18f * offset;
                component.rotationOffset = new Vector3(0f, 90f, 0f);

                if (index > MoreSuitsMod.SUITS_PER_RACK - 1)
                    suit.gameObject.SetActive(false);
                    
                index++;
                offset++;
            }
            FillPages();
        }

        private static void FillPages()
        {
            SuitsPages.Clear();
            List<UnlockableSuit> currentPage = new List<UnlockableSuit>();
            int pageLimiter = 0;
            foreach (UnlockableSuit unlockableSuit in MoreSuitsMod.CachedSuits)
            {
                if (pageLimiter > MoreSuitsMod.SUITS_PER_RACK - 1)
                {
                    SuitsPages.Add(currentPage.ToArray());
                    currentPage.Clear();
                    pageLimiter = 0;
                }
                currentPage.Add(unlockableSuit);
                pageLimiter++;
            }
            if(currentPage.Count > 0)
                SuitsPages.Add(currentPage.ToArray());
            RenderPage();
        }

        private static void CreateAndRegisterInteract(GameObject gameObject, string dir, Action onInteract)
        {
            gameObject.GetComponent<BoxCollider>().tag = "InteractTrigger";
            gameObject.tag = "InteractTrigger";
            gameObject.layer = InteractLayer;
            InteractTrigger interactTrigger = gameObject.AddComponent<InteractTrigger>();
            if(interactTrigger.onInteract == null)
                interactTrigger.onInteract = new InteractEvent();
            interactTrigger.onInteract.AddListener(playerController =>
            {
                if(playerController.NetworkManager.LocalClientId != playerController.playerClientId) return;
                onInteract.Invoke();
            });
            interactTrigger.hoverTip = $"{dir} Page";
            interactTrigger.hoverIcon = StolenHandIcon;
            interactTrigger.twoHandedItemAllowed = true;
            interactTrigger.interactCooldown = false;
        }

        private static void RenderPage()
        {
            if (CurrentPage < 0)
                CurrentPage = SuitsPages.Count - 1;
            if (CurrentPage > SuitsPages.Count - 1)
                CurrentPage = 0;
            SuitsPages.ForEach(x =>
            {
                foreach (UnlockableSuit unlockableSuit in x)
                    unlockableSuit.gameObject.SetActive(false);
            });
            foreach (UnlockableSuit unlockableSuit in SuitsPages[CurrentPage])
                unlockableSuit.gameObject.SetActive(true);
        }

        private void HardReset(StartOfRound instance)
        {
            if (instance == null)
            {
                // Probably hit Quit
                MoreSuitsMod.CachedSuits.Clear();
                SuitsPages.Clear();
                CurrentPage = 0;
                return;
            }
            SuitsPages.Clear();
            MoreSuitsMod.RefreshSuits();
            PatchedPositionSuits(instance);
            CurrentPage = 0;
            if(IsCurrentSorter)
                RenderPage();
        }
    }
}