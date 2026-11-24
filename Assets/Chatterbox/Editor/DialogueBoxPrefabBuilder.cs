#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Chatterbox.Editor
{
    /*
     * Tools > ChatterBox > Build Dialogue Box Prefabs
     * Reads dialogue-boxes/layout.json and saves one prefab per design folder to dialogue-boxes/Prefabs/<folder>.prefab.
     * Each prefab is a 1920x1080 (layout "canvas") RectTransform with one Image per layer, placed at the layout's
     * pixel position (x, y from the top-left) and stacked in layout.json order (first = back),
     * then a "Name Text" and "Dialogue Text" (TextMeshPro, with sample text) placed per design from TextLayouts below.
     * Drop it under a Canvas whose CanvasScaler reference resolution is 1920x1080. Re-running overwrites the prefabs.
     */
    public static class DialogueBoxPrefabBuilder
    {
        private const string BoxesFolder = "Assets/Chatterbox/dialogue-boxes";
        private const string PrefabsFolder = BoxesFolder + "/Prefabs";
        private const string FontsFolder = "Assets/Chatterbox/Fonts";

        [MenuItem("Tools/ChatterBox/Build Dialogue Box Prefabs")]
        public static void BuildAll()
        {
            string jsonPath = BoxesFolder + "/layout.json";
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[DialogueBoxPrefabBuilder] Missing {jsonPath}");
                return;
            }

            var root = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(jsonPath));
            var canvas = (List<object>)root["canvas"];
            var canvasSize = new Vector2(ToFloat(canvas[0]), ToFloat(canvas[1]));
            var designs = (Dictionary<string, object>)root["designs"];

            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
                AssetDatabase.CreateFolder(BoxesFolder, "Prefabs");

            int built = 0;
            foreach (var design in designs)
            {
                var layers = (Dictionary<string, object>)design.Value;
                if (layers.Count == 0) continue;

                // Name the prefab after the folder its files live in, e.g. "06-farm"
                string firstFile = (string)((Dictionary<string, object>)layers.Values.First())["file"];
                string folderName = Path.GetDirectoryName(firstFile)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(folderName)) folderName = design.Key.Replace(' ', '-');

                if (BuildPrefab(folderName, layers, canvasSize)) built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DialogueBoxPrefabBuilder] Built {built} dialogue box prefab(s) in {PrefabsFolder}");
        }

        private static bool BuildPrefab(string name, Dictionary<string, object> layers, Vector2 canvasSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            try
            {
                var rootRect = (RectTransform)go.transform;
                rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.sizeDelta = canvasSize;

                foreach (var layer in layers)
                {
                    var data = (Dictionary<string, object>)layer.Value;
                    string file = BoxesFolder + "/" + (string)data["file"];
                    Sprite sprite = LoadMainSprite(file);
                    if (sprite == null)
                        Debug.LogWarning($"[DialogueBoxPrefabBuilder] {name}: no sprite found at {file}");

                    var bounds = new Rect(ToFloat(data["x"]), ToFloat(data["y"]), ToFloat(data["w"]), ToFloat(data["h"]));
                    var child = CreateChild(ToLayerName(layer.Key), rootRect, bounds, typeof(Image));

                    var image = child.GetComponent<Image>();
                    image.sprite = sprite;
                    image.raycastTarget = false;
                }

                if (TextLayouts.TryGetValue(name, out var text)) AddText(rootRect, text);
                else Debug.LogWarning($"[DialogueBoxPrefabBuilder] {name}: no text layout, prefab has artwork only");

                string path = $"{PrefabsFolder}/{name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, path, out bool success);
                if (!success) Debug.LogError($"[DialogueBoxPrefabBuilder] Could not save {path}");
                return success;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ---- Text -------------------------------------------------------------------------------------------
        // layout.json only holds artwork, so where the name and dialogue go is set here per design,
        // in the same 1920x1080 top-left pixel space (x, y, w, h).

        private class TextLayout
        {
            public string SampleName;
            public Rect NameRect;
            public Color NameColor;
            public float NameSize;
            public FontStyles NameStyle = FontStyles.Bold;
            public TextAlignmentOptions NameAlign = TextAlignmentOptions.MidlineLeft;
            public float NameSpacing;               // extra character spacing, room for outlines
            public string NameFont;                // .ttf in Chatterbox/Fonts matching the design's lettering; null = TMP default
            public string NameMaterialStyle;        // material preset name, saved next to the font asset
            public System.Action<Material> StyleNameMaterial; // outline / shadow settings for that preset

            public string SampleText;
            public Rect TextRect;
            public Color TextColor;
            public float TextSize = 40;
            public string TextFont;                 // .ttf in Chatterbox/Fonts for the dialogue; null = TMP default

            public string HideLayer;     // artwork layer with a baked-in "Name" placeholder, set inactive
            public Rect? NameCover;      // solid patch painted over a "Name" baked into the box art itself
            public Color NameCoverColor;
        }

        private static readonly Dictionary<string, TextLayout> TextLayouts = new Dictionary<string, TextLayout>
        {
            ["01-brush-lily"] = new TextLayout
            {
                SampleName = "Name",
                // Position, size and border below were tuned by hand in the editor, keep them in sync with the prefab
                NameRect = new Rect(747, 702.2f, 590.1144f, 128.0999f),
                // Matches name.png: serif (Galorine), cream fill, thick solid dark border
                NameFont = "Galorine.ttf", NameMaterialStyle = "Lily Outline",
                NameColor = Hex("FBE6DC"), NameSize = 150, NameStyle = FontStyles.Normal, NameSpacing = 2,
                StyleNameMaterial = m =>
                {
                    // No outline: the border is the underlay, dilated around the letters and nudged down a touch
                    m.DisableKeyword("OUTLINE_ON");
                    m.SetColor("_OutlineColor", Hex("221619"));
                    m.SetFloat("_OutlineWidth", 0f);
                    m.SetFloat("_FaceDilate", -0.06f);
                    m.EnableKeyword("UNDERLAY_ON");
                    m.SetColor("_UnderlayColor", new Color(0.16f, 0.12f, 0.13f, 1f)); // fully solid
                    m.SetFloat("_UnderlayOffsetX", -0.06f);
                    m.SetFloat("_UnderlayOffsetY", -0.16f);
                    m.SetFloat("_UnderlayDilate", 0.47f);
                    m.SetFloat("_UnderlaySoftness", 0f);
                },
                SampleText = "The lilies only bloom after the first storm of spring. Will you wait for them with me?",
                TextRect = new Rect(830, 880, 780, 150),
                TextFont = "CormorantGaramond-Medium.ttf",
                TextColor = Hex("3A0D1A"), TextSize = 46, // Garamond runs small, so a little bigger than the others
            },
            ["04-comic"] = new TextLayout
            {
                SampleName = "AKIRA",
                NameRect = new Rect(1029, 698, 185, 62),
                // Matches the plate's "NAME": dry-brush capitals (Edo SZ) in cream
                NameFont = "EdoSZ.ttf",
                NameColor = Hex("FFF1E8"), NameSize = 52, NameStyle = FontStyles.UpperCase, // Edo is already slanted
                NameAlign = TextAlignmentOptions.Center,
                SampleText = "You really thought you could sneak past me? Not a chance!",
                TextRect = new Rect(900, 800, 570, 140),
                TextFont = "ShortStack-Regular.ttf",
                TextColor = Hex("2A1A20"), TextSize = 42,
                NameCover = new Rect(1027, 697, 190, 68), NameCoverColor = Hex("231A1E"),
            },
            ["05-windows"] = new TextLayout
            {
                SampleName = "Kai.exe",
                NameRect = new Rect(446, 668, 800, 52),
                NameFont = "W95FA.otf", NameStyle = FontStyles.Bold, // W95FA has one weight, so this is TMP's bold
                NameColor = Color.white, NameSize = 36,
                SampleText = "Hey. Got a minute? There's something weird going on with the server logs.",
                TextRect = new Rect(750, 760, 600, 220),
                TextFont = "W95FA.otf",
                TextColor = Color.black, TextSize = 36,
            },
            ["06-farm"] = new TextLayout
            {
                SampleName = "Junko",
                NameRect = new Rect(640, 545, 440, 80),
                NameFont = "Nunito-BlackItalic.ttf", NameStyle = FontStyles.Normal, // the font file is already Black Italic
                NameColor = Hex("FFF1E0"), NameSize = 48,
                NameAlign = TextAlignmentOptions.Center,
                SampleText = "Morning! The carrots came up overnight. Want to help me pull a few before the crows find them?",
                TextRect = new Rect(610, 665, 900, 230),
                TextFont = "Nunito-Regular.ttf",
                TextColor = Hex("5A2A22"),
            },
            ["06-gta"] = new TextLayout
            {
                SampleName = "Roxy",
                NameRect = new Rect(640, 720, 1000, 70),
                NameFont = "BebasNeue-Regular.ttf", NameStyle = FontStyles.Normal, // Bebas is already heavy, no faux bold
                NameColor = Hex("FFB020"), NameSize = 56,
                SampleText = "Listen up. The deal goes down at midnight by the docks. Don't be late, and don't bring anyone.",
                TextRect = new Rect(640, 800, 1150, 220),
                TextFont = "Oswald-Regular.ttf",
                TextColor = Color.white, TextSize = 42,
            },
        };

        private static void AddText(RectTransform root, TextLayout layout)
        {
            if (layout.HideLayer != null)
            {
                Transform hidden = root.Find(layout.HideLayer);
                if (hidden != null) hidden.gameObject.SetActive(false); // kept as a style reference
            }

            if (layout.NameCover.HasValue)
            {
                var cover = CreateChild("Name Plate Cover", root, layout.NameCover.Value, typeof(Image)).GetComponent<Image>();
                cover.color = layout.NameCoverColor;
                cover.raycastTarget = false;
            }

            var nameText = CreateText("Name Text", root, layout.NameRect, layout.SampleName, layout.NameColor, layout.NameSize);
            nameText.fontStyle = layout.NameStyle;
            nameText.alignment = layout.NameAlign;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.characterSpacing = layout.NameSpacing;
            if (layout.NameFont != null)
            {
                TMP_FontAsset font = GetOrCreateFontAsset(layout.NameFont);
                if (font != null)
                {
                    nameText.font = font;
                    if (layout.StyleNameMaterial != null)
                        nameText.fontSharedMaterial = GetOrCreateMaterialPreset(font, layout.NameMaterialStyle, layout.StyleNameMaterial);
                }
            }

            var dialogueText = CreateText("Dialogue Text", root, layout.TextRect, layout.SampleText, layout.TextColor, layout.TextSize);
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            if (layout.TextFont != null)
            {
                TMP_FontAsset font = GetOrCreateFontAsset(layout.TextFont);
                if (font != null) dialogueText.font = font;
            }
        }

        private static TextMeshProUGUI CreateText(string name, RectTransform parent, Rect bounds, string text, Color color, float size)
        {
            var tmp = CreateChild(name, parent, bounds, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.color = color;
            // Shrink long lines to fit instead of spilling out of the box
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = size;
            tmp.fontSizeMin = Mathf.Min(18, size);
            tmp.raycastTarget = false;
            return tmp;
        }

        // "Shrikhand-Regular.ttf" -> Fonts/Shrikhand-Regular SDF.asset (dynamic, so any character name works)
        private static TMP_FontAsset GetOrCreateFontAsset(string ttfName)
        {
            string ttfPath = $"{FontsFolder}/{ttfName}";
            string assetName = Path.GetFileNameWithoutExtension(ttfName) + " SDF";
            string assetPath = $"{FontsFolder}/{assetName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogWarning($"[DialogueBoxPrefabBuilder] Missing font {ttfPath}, using the TMP default");
                return null;
            }

            // Wide padding leaves room in the atlas for thick outlines and drop shadows (TMP can't draw past it)
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 20, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic);
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, assetPath);

            asset.atlasTextures[0].name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // A TMP material preset (font atlas + outline/shadow settings), updated in place on every build
        private static Material GetOrCreateMaterialPreset(TMP_FontAsset font, string styleName, System.Action<Material> style)
        {
            string path = $"{FontsFolder}/{font.name} - {styleName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material) { name = $"{font.name} - {styleName}" };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.CopyPropertiesFromMaterial(font.material);
                material.shaderKeywords = font.material.shaderKeywords;
            }

            style(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Child placed by its top-left corner in layout pixels (y grows downwards, like layout.json)
        private static GameObject CreateChild(string name, RectTransform parent, Rect bounds, params System.Type[] components)
        {
            var child = new GameObject(name, new[] { typeof(RectTransform) }.Concat(components).ToArray());
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            rect.sizeDelta = bounds.size;
            return child;
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;

        // Textures are imported as Multiple, and auto-slicing left a few stray specks as extra sprites,
        // so take the largest sprite in the file (the actual artwork).
        private static Sprite LoadMainSprite(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderByDescending(s => s.rect.width * s.rect.height)
                .FirstOrDefault();
        }

        // "junko-and-hare-expression-sheet-1-copy" -> "Junko And Hare Expression Sheet 1 Copy", "box" -> "Box"
        private static string ToLayerName(string key)
        {
            var words = key.Split('-', '_', ' ').Where(w => w.Length > 0)
                .Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1));
            return string.Join(" ", words);
        }

        private static float ToFloat(object value) => System.Convert.ToSingle(value, CultureInfo.InvariantCulture);

        // Minimal JSON reader: JsonUtility can't read objects with arbitrary keys like "designs".
        // Objects -> Dictionary<string, object> (key order kept), arrays -> List<object>, numbers -> double.
        private static class MiniJson
        {
            public static object Parse(string json)
            {
                int i = 0;
                return ReadValue(json, ref i);
            }

            private static object ReadValue(string s, ref int i)
            {
                SkipWhitespace(s, ref i);
                char c = s[i];
                if (c == '{') return ReadObject(s, ref i);
                if (c == '[') return ReadArray(s, ref i);
                if (c == '"') return ReadString(s, ref i);
                if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
                if (s.Substring(i).StartsWith("false")) { i += 5; return false; }
                if (s.Substring(i).StartsWith("null")) { i += 4; return null; }
                return ReadNumber(s, ref i);
            }

            private static Dictionary<string, object> ReadObject(string s, ref int i)
            {
                var result = new Dictionary<string, object>();
                i++; // {
                while (true)
                {
                    SkipWhitespace(s, ref i);
                    if (s[i] == '}') { i++; return result; }
                    string key = ReadString(s, ref i);
                    SkipWhitespace(s, ref i);
                    i++; // :
                    result[key] = ReadValue(s, ref i);
                    SkipWhitespace(s, ref i);
                    if (s[i] == ',') i++;
                }
            }

            private static List<object> ReadArray(string s, ref int i)
            {
                var result = new List<object>();
                i++; // [
                while (true)
                {
                    SkipWhitespace(s, ref i);
                    if (s[i] == ']') { i++; return result; }
                    result.Add(ReadValue(s, ref i));
                    SkipWhitespace(s, ref i);
                    if (s[i] == ',') i++;
                }
            }

            private static string ReadString(string s, ref int i)
            {
                var sb = new StringBuilder();
                i++; // opening quote
                while (s[i] != '"')
                {
                    if (s[i] == '\\')
                    {
                        i++;
                        char e = s[i];
                        if (e == 'u') { sb.Append((char)System.Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                        else sb.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == 'r' ? '\r' : e == 'b' ? '\b' : e == 'f' ? '\f' : e);
                    }
                    else sb.Append(s[i]);
                    i++;
                }
                i++; // closing quote
                return sb.ToString();
            }

            private static double ReadNumber(string s, ref int i)
            {
                int start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
            }

            private static void SkipWhitespace(string s, ref int i)
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }
        }
    }
}
#endif
