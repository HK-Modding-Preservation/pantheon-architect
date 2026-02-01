using System;
using System.Collections.Generic;
using System.Linq;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PantheonArchitect
{
    public class PantheonArchitect : Mod
    {
        private static PantheonArchitect? _instance;

        internal static PantheonArchitect Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(PantheonArchitect)} was never constructed");
                }
                return _instance;
            }
        }

        public override string GetVersion() => "1.0.0.0";

        // Pantheon 5 boss scenes (based on actual Pantheon of Hallownest)
        private readonly List<string> pantheon5Bosses = new List<string>
        {
            "GG_Vengefly_V",       // 1. Vengefly King x2
            "GG_Gruz_Mother_V",    // 2. Gruz Mother
            "GG_False_Knight",     // 3. False Knight
            "GG_Mega_Moss_Charger",// 4. Massive Moss Charger
            "GG_Hornet_1",         // 5. Hornet Protector
            "GG_Ghost_Gorb_V",     // 6. Gorb
            "GG_Dung_Defender",    // 7. Dung Defender
            "GG_Mage_Knight_V",    // 8. Soul Warrior
            "GG_Brooding_Mawlek_V",// 9. Brooding Mawlek
            "GG_Nailmasters",      // 10. Oro & Mato
            "GG_Ghost_Xero_V",     // 11. Xero
            "GG_Crystal_Guardian", // 12. Crystal Guardian
            "GG_Soul_Master",      // 13. Soul Master
            "GG_Oblobbles",        // 14. Oblobbles
            "GG_Mantis_Lords_V",   // 15. Sisters of Battle
            "GG_Ghost_Marmu_V",    // 16. Marmu
            "GG_Flukemarm",        // 17. Flukemarm
            "GG_Broken_Vessel",    // 18. Broken Vessel
            "GG_Galien",           // 19. Galien
            "GG_Painter",          // 20. Paintmaster Sheo
            "GG_Hive_Knight",      // 21. Hive Knight
            "GG_Ghost_Hu",       // 22. Elder Hu
            "GG_Collector_V",      // 23. The Collector
            "GG_God_Tamer",        // 24. God Tamer
            "GG_Grimm",            // 25. Troupe Master Grimm
            "GG_Watcher_Knights",  // 26. Watcher Knights
            "GG_Uumuu_V",          // 27. Uumuu
            "GG_Nosk_Hornet",      // 28. Winged Nosk
            "GG_Sly",              // 29. Great Nailsage Sly
            "GG_Hornet_2",         // 30. Hornet Sentinel
            "GG_Crystal_Guardian_2",// 31. Enraged Guardian
            "GG_Lost_Kin",         // 32. Lost Kin
            "GG_Ghost_NoEyes_V",   // 33. No Eyes
            "GG_Traitor_Lord",     // 34. Traitor Lord
            "GG_White_Defender",   // 35. White Defender
            "GG_Soul_Tyrant",      // 36. Soul Tyrant
            "GG_Ghost_Markoth_V",  // 37. Markoth
            "GG_Grey_Prince_Zote", // 38. Grey Prince Zote (skipped if conditions not met)
            "GG_Failed_Champion",  // 39. Failed Champion
            "GG_Grimm_Nightmare",  // 40. Nightmare King Grimm
            "GG_Hollow_Knight",    // 41. Pure Vessel
            "GG_Radiance"          // 42. Absolute Radiance
        };

        private List<string>? randomizedOrder = null;
        private int currentBossIndex = 0;
        private System.Random rng;
        private int currentSeed;
        private bool inPantheon5 = false;
        private string customLineupCode = ""; // Stores the current custom lineup code if used
        private bool isCustomLineup = false; // True if using custom lineup instead of random seed
        
        // UI Elements
        private GameObject lineupInputCanvas;
        private InputField lineupInputField;
        private Text currentLineupText;

        public void SetLineupCode(string code)
        {
            try
            {
                // Validate and decode the lineup code
                List<string> lineup = DecodeLineupCode(code);
                if (lineup != null && lineup.Count > 0)
                {
                    customLineupCode = code;
                    isCustomLineup = true;
                    Log($"Custom lineup code set: {code} ({lineup.Count} bosses)");
                    UpdateLineupDisplayUI();
                }
                else
                {
                    Log("Invalid lineup code: incorrect format or empty");
                }
            }
            catch (Exception ex)
            {
                Log($"Error decoding lineup code: {ex.Message}");
                isCustomLineup = false;
                customLineupCode = "";
            }
        }

        public PantheonArchitect() : base()
        {
            _instance = this;
            currentSeed = Environment.TickCount;
            rng = new System.Random(currentSeed);
        }

        public override void Initialize()
        {
            Log("Initializing Pantheon Architect");
            Log($"Starting seed: {currentSeed}");

            ModHooks.BeforeSceneLoadHook += OnSceneLoad;
            ModHooks.AfterSavegameLoadHook += OnSaveLoaded;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
            ModHooks.HeroUpdateHook += CheckToggleKey;
            
            Log("Pantheon Architect Initialized");
            Log("Press F1 in Godhome to toggle lineup input UI");
        }

        private void CheckToggleKey()
        {
            // Check if F1 is pressed to toggle seed UI
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                Log("F1 key pressed");
                
                // Toggle lineup input UI in Godhome
                if (lineupInputCanvas != null)
                {
                    bool newState = !lineupInputCanvas.activeSelf;
                    lineupInputCanvas.SetActive(newState);
                    Log($"Lineup input UI toggled to: {(newState ? "shown" : "hidden")}");
                }
            }
        }

        private void OnSceneChanged(Scene from, Scene to)
        {
            Log($"Scene changed from '{from.name}' to '{to.name}', inPantheon5={inPantheon5}");
            
            // Reset if we return to Godhome after completing custom lineup
            if (inPantheon5 && (to.name == "GG_Atrium" || to.name == "GG_Atrium_Roof" || to.name.Contains("GG_Workshop")))
            {
                Log("Returned to Godhome, resetting");
                ResetRandomization();
            }
            
            // Destroy old UI when changing scenes
            DestroyUI();
            
            // Create lineup input UI in Godhome
            if (to.name == "GG_Atrium" || to.name == "GG_Atrium_Roof")
            {
                Log("In Godhome, creating lineup input UI");
                CreateLineupInputUI();
            }
        }

        private void OnSaveLoaded(SaveGameData data)
        {
            // Reset when loading a save
            ResetRandomization();
        }

        private string OnSceneLoad(string targetScene)
        {
            // Check if we're entering Pantheon 5
            if (targetScene == "GG_Vengefly_V" && randomizedOrder == null)
            {
                RandomizePantheon5();
                currentBossIndex = 0;
                inPantheon5 = true;
                
                // Log pantheon start
                Log("==============================================");
                Log("PANTHEON 5 STARTING");
                Log("==============================================");
            }

            // If we have a randomized order and this is a P5 boss scene
            if (randomizedOrder != null && pantheon5Bosses.Contains(targetScene))
            {
                // Return the next boss in our randomized order
                if (currentBossIndex < randomizedOrder.Count)
                {
                    string nextBoss = randomizedOrder[currentBossIndex];
                    currentBossIndex++;
                    Log($"Redirecting from {targetScene} to {nextBoss} (Boss {currentBossIndex}/{randomizedOrder.Count})");
                    return nextBoss;
                }
                
                // Lineup complete - redirect to main Godhome area
                Log($"Custom lineup complete! ({randomizedOrder.Count} bosses defeated)");
                Log("Redirecting to GG_Atrium...");
                ResetRandomization();
                return "GG_Atrium";
            }

            return targetScene;
        }

        private void RandomizePantheon5()
        {
            // Check if we're using a custom lineup code
            if (isCustomLineup && !string.IsNullOrEmpty(customLineupCode))
            {
                Log($"Using custom lineup code: {customLineupCode}");
                randomizedOrder = DecodeLineupCode(customLineupCode);
                
                if (randomizedOrder != null)
                {
                    // Custom lineup - use as-is (user controls final boss)
                    Log($"Custom lineup loaded with {randomizedOrder.Count} bosses");
                    for (int i = 0; i < randomizedOrder.Count; i++)
                    {
                        Log($"  {i + 1}. {randomizedOrder[i]}");
                    }
                    return;
                }
                else
                {
                    Log("Failed to decode custom lineup - cannot start pantheon without valid code");
                    isCustomLineup = false;
                    randomizedOrder = null;
                    return;
                }
            }
            
            // If no custom lineup, show error
            Log("ERROR: No custom lineup code provided. Please enter a lineup code in Godhome.");
            randomizedOrder = null;
        }

        private void ResetRandomization()
        {
            Log("Resetting randomization (Pantheon complete or exited)");
            randomizedOrder = null;
            currentBossIndex = 0;
            inPantheon5 = false;
            DestroyUI();
        }

        // Encode a lineup of any length into a shareable Base64 code
        private string EncodeLineupCode(List<string> lineup)
        {
            if (lineup == null || lineup.Count == 0 || lineup.Count > 255)
            {
                Log($"Cannot encode lineup: must contain 1-255 bosses (got {lineup?.Count ?? 0})");
                return "";
            }

            // First byte is the length, remaining bytes are boss indices
            byte[] data = new byte[lineup.Count + 1];
            data[0] = (byte)lineup.Count;
            
            for (int i = 0; i < lineup.Count; i++)
            {
                int index = pantheon5Bosses.IndexOf(lineup[i]);
                if (index >= 0 && index < pantheon5Bosses.Count) // Valid boss/bench index
                {
                    data[i + 1] = (byte)index;
                }
                else
                {
                    Log($"Warning: Boss {lineup[i]} not found in boss list, using 0");
                    data[i + 1] = 0;
                }
            }

            return Convert.ToBase64String(data);
        }

        // Decode a Base64 lineup code into a list of boss scene names
        private List<string> DecodeLineupCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            try
            {
                byte[] data = Convert.FromBase64String(code);
                
                if (data.Length < 2)
                {
                    Log($"Invalid lineup code: too short ({data.Length} bytes)");
                    return null;
                }

                // First byte is the length
                int length = data[0];
                
                if (data.Length != length + 1)
                {
                    Log($"Invalid lineup code: length mismatch (expected {length + 1}, got {data.Length})");
                    return null;
                }

                List<string> lineup = new List<string>();
                for (int i = 0; i < length; i++)
                {
                    int index = data[i + 1];
                    if (index >= 0 && index < pantheon5Bosses.Count) // Valid boss/bench index
                    {
                        lineup.Add(pantheon5Bosses[index]);
                    }
                    else
                    {
                        Log($"Invalid boss index {index} at position {i}, using default");
                        lineup.Add(pantheon5Bosses[0]);
                    }
                }

                Log($"Decoded lineup with {lineup.Count} bosses");
                return lineup;
            }
            catch (FormatException ex)
            {
                Log($"Invalid Base64 format: {ex.Message}");
                return null;
            }
        }

        private void CreateLineupInputUI()
        {
            if (lineupInputCanvas != null) return;
            
            // Create canvas
            lineupInputCanvas = new GameObject("PantheonArchitectLineupInput");
            Canvas canvas = lineupInputCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            CanvasScaler scaler = lineupInputCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            lineupInputCanvas.AddComponent<GraphicRaycaster>();
            
            // Start hidden - press F1 to show
            lineupInputCanvas.SetActive(false);
            
            // Create panel
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(lineupInputCanvas.transform);
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 100);
            panelRect.sizeDelta = new Vector2(400, 200);
            
            // Title text
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 20;
            titleText.color = Color.white;
            titleText.text = "Custom Pantheon Lineup";
            titleText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(0, 30);
            
            // Current lineup text
            GameObject currentLineupObj = new GameObject("CurrentLineup");
            currentLineupObj.transform.SetParent(panel.transform);
            currentLineupText = currentLineupObj.AddComponent<Text>();
            currentLineupText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            currentLineupText.fontSize = 16;
            currentLineupText.color = Color.yellow;
            currentLineupText.text = "No lineup loaded";
            currentLineupText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform currentLineupRect = currentLineupObj.GetComponent<RectTransform>();
            currentLineupRect.anchorMin = new Vector2(0, 1);
            currentLineupRect.anchorMax = new Vector2(1, 1);
            currentLineupRect.pivot = new Vector2(0.5f, 1);
            currentLineupRect.anchoredPosition = new Vector2(0, -45);
            currentLineupRect.sizeDelta = new Vector2(0, 25);
            
            // Input field
            GameObject inputObj = new GameObject("LineupInput");
            inputObj.transform.SetParent(panel.transform);
            
            Image inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(1, 1, 1, 0.9f);
            
            lineupInputField = inputObj.AddComponent<InputField>();
            lineupInputField.textComponent = CreateInputText(inputObj.transform);
            lineupInputField.placeholder = CreatePlaceholderText(inputObj.transform);
            lineupInputField.characterLimit = 100; // Allow long lineup codes
            lineupInputField.contentType = InputField.ContentType.Standard; // Allow alphanumeric
            
            RectTransform inputRect = inputObj.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.1f, 0.5f);
            inputRect.anchorMax = new Vector2(0.9f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = new Vector2(0, 10);
            inputRect.sizeDelta = new Vector2(0, 30);
            
            // Load Lineup button
            CreateButton(panel.transform, "Load Lineup", new Vector2(0, -35), () => {
                string input = lineupInputField.text;
                if (string.IsNullOrEmpty(input))
                    return;
                    
                // Treat input as lineup code
                SetLineupCode(input);
                lineupInputField.text = "";
            });
            
            Log("Lineup input UI created");
        }

        private Text CreateInputText(Transform parent)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent);
            
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.color = Color.black;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(5, 0);
            rect.offsetMax = new Vector2(-5, 0);
            
            return text;
        }

        private Text CreatePlaceholderText(Transform parent)
        {
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(parent);
            
            Text placeholder = placeholderObj.AddComponent<Text>();
            placeholder.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholder.fontSize = 16;
            placeholder.color = new Color(0, 0, 0, 0.5f);
            placeholder.text = "Enter lineup code...";
            placeholder.alignment = TextAnchor.MiddleLeft;
            
            RectTransform rect = placeholderObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(5, 0);
            rect.offsetMax = new Vector2(-5, 0);
            
            return placeholder;
        }

        private void CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(text + "Button");
            buttonObj.transform.SetParent(parent);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);
            
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(onClick);
            
            // Button color transition
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 1f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.9f, 1f);
            button.colors = colors;
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.pivot = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(140, 35);
            
            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.text = text;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void UpdateLineupDisplayUI()
        {
            if (currentLineupText != null)
            {
                if (isCustomLineup && !string.IsNullOrEmpty(customLineupCode))
                {
                    currentLineupText.text = $"Lineup: {customLineupCode.Substring(0, Math.Min(15, customLineupCode.Length))}...";
                }
                else
                {
                    currentLineupText.text = "No lineup loaded";
                }
            }
        }

        private void DestroyUI()
        {
            if (lineupInputCanvas != null)
            {
                Log("Destroying lineup input canvas");
                GameObject.Destroy(lineupInputCanvas);
                lineupInputCanvas = null;
                lineupInputField = null;
                currentLineupText = null;
            }
        }
    }
}
