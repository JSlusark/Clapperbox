#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefaultNamespace;
using UnityEditor;
using UnityEngine;

namespace Clapperbox.Editor
{
    /*
     * Finds the character data a screenplay csv needs but the project doesn't have, and creates it:
     *  - a speaker with no CharacterData   -> Characters/<Name>/ copied from Characters/0_template,
     *                                          plus <Name>_data.asset with the template's expressions and voice imported
     *  - an expression a character has no sprite for -> a white image the size of the character's Neutral sprite,
     *                                          saved as Expressions/<Expression>.png and added to the character's data
     * A name that is close to an existing one (a speaker, or an expression sprite of any character or the template)
     * is reported as a possible typo instead, and is not created unless the user marks it as "not a typo".
     */
    public static class MissingCharacterData
    {
        public const string CharactersFolder = "Assets/Clapperbox/Characters";
        public const string TemplateFolder = CharactersFolder + "/0_template";
        private const string ExpressionsFolderName = "Expressions";
        private const string NeutralName = "Neutral";
        private const int FallbackSize = 512; // white image size when the character has no sprite to measure

        public class Typo
        {
            public string Character;  // the speaker as written in the csv
            public string Expression; // null when the speaker itself is the typo
            public string Suggestion; // the existing name it is close to
            public string Key => TypoKey(Character, Expression);
        }

        public class Report
        {
            public readonly List<string> MissingCharacters = new List<string>();
            public readonly List<(string character, string expression)> MissingExpressions = new List<(string, string)>();
            public readonly List<Typo> Typos = new List<Typo>();
            public bool HasMissing => MissingCharacters.Count > 0 || MissingExpressions.Count > 0;
            public bool IsEmpty => !HasMissing && Typos.Count == 0;
        }

        // Identifies a typo the user marked as intended
        public static string TypoKey(string character, string expression) =>
            expression == null ? $"character:{character}" : $"expression:{character}/{expression}";

        // characters: from CharacterObjectsImport.FindAllCharacters(), passed in so several csvs share one lookup.
        // notTypos: keys (TypoKey) of names the user confirmed, which are then reported as missing instead.
        public static Report Check(string csvText, Dictionary<string, CharacterData> characters, ICollection<string> notTypos)
        {
            var report = new Report();
            var rows = ScreenPlayCsvImporter.ReadRows(csvText);
            var templateExpressions = GetTemplateExpressions();

            // Every expression that exists as a sprite (any character, or the template). Names only written in the
            // csv don't count: "Angry-more" and "Angry-most" there are variants, not typos of each other.
            var knownExpressions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var character in characters.Values)
                if (character.expressions != null)
                    foreach (var e in character.expressions)
                        if (!string.IsNullOrEmpty(e.name) && e.sprite != null) knownExpressions.Add(e.name);
            knownExpressions.UnionWith(templateExpressions);

            // Speakers in csv order, each with the expressions they use (no repeats, case ignored)
            var used = new List<(string speaker, List<string> expressions)>();
            foreach (var (speaker, expression) in rows)
            {
                if (string.IsNullOrEmpty(speaker)) continue;

                int index = used.FindIndex(u => u.speaker == speaker);
                if (index < 0)
                {
                    used.Add((speaker, new List<string>()));
                    index = used.Count - 1;
                }

                if (!string.IsNullOrEmpty(expression)
                    && !used[index].expressions.Contains(expression, StringComparer.OrdinalIgnoreCase))
                    used[index].expressions.Add(expression);
            }

            foreach (var (speaker, expressions) in used)
            {
                if (!characters.TryGetValue(speaker, out CharacterData character))
                {
                    string similar = notTypos.Contains(TypoKey(speaker, null))
                        ? null
                        : FindSimilar(speaker, characters.Keys, caseMatters: true);
                    if (similar != null)
                    {
                        // Probably a misspelled existing speaker: don't create a new character for it
                        report.Typos.Add(new Typo { Character = speaker, Suggestion = similar });
                        continue;
                    }

                    report.MissingCharacters.Add(speaker);
                    // A new character gets the template's sprites; only expressions the template lacks are missing
                    foreach (string expression in expressions)
                        if (!templateExpressions.Contains(expression))
                            AddMissingExpression(report, speaker, expression, templateExpressions, knownExpressions, notTypos);
                    continue;
                }

                var own = new HashSet<string>(
                    (character.expressions ?? new List<ExpressionSprite>()).Where(e => e.sprite != null).Select(e => e.name),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string expression in expressions)
                    if (!own.Contains(expression))
                        AddMissingExpression(report, speaker, expression, own, knownExpressions, notTypos);
            }

            return report;
        }

        // A missing expression is a possible typo when it's close to an existing sprite name. Names are compared
        // as "<base>-<variant>": "Suprised-most" -> "Surprised-most" (the base "Suprised" is close to "Surprised"),
        // while "Angry-more" is a new variant of the existing "Angry" and gets created.
        private static void AddMissingExpression(Report report, string speaker, string expression,
                                                 IEnumerable<string> own, IEnumerable<string> known, ICollection<string> notTypos)
        {
            if (!notTypos.Contains(TypoKey(speaker, expression)))
            {
                string suggestion = SuggestExpression(expression, own.Concat(known).ToList());
                if (suggestion != null)
                {
                    report.Typos.Add(new Typo { Character = speaker, Expression = expression, Suggestion = suggestion });
                    return;
                }
            }
            report.MissingExpressions.Add((speaker, expression));
        }

        private static string SuggestExpression(string expression, List<string> sprites)
        {
            var (name, variant) = SplitVariant(expression);

            // The whole name is close to a sprite that isn't just another variant of the same base
            var notSameBase = sprites.Where(s => !string.Equals(SplitVariant(s).name, name, StringComparison.OrdinalIgnoreCase));
            string whole = FindSimilar(expression, notSameBase, caseMatters: false);
            if (whole != null) return whole;

            // The base is unknown but close to a known one: keep the variant, fix the base
            var bases = sprites.Select(s => SplitVariant(s).name).ToList();
            if (bases.Contains(name, StringComparer.OrdinalIgnoreCase)) return null;
            string similarBase = FindSimilar(name, bases, caseMatters: false);
            return similarBase != null ? similarBase + variant : null;
        }

        // "Angry-more" -> ("Angry", "-more"); "Comforting" -> ("Comforting", "")
        private static (string name, string variant) SplitVariant(string expression)
        {
            int dash = expression.LastIndexOf('-');
            return dash > 0 ? (expression.Substring(0, dash), expression.Substring(dash)) : (expression, "");
        }

        // The closest other name within a small number of edits (1 for names of 3-4 letters, 2 from 5), or null.
        // A false match only costs a "Not a typo" click; a missed one creates an unwanted character or sprite.
        // caseMatters: speakers must match exactly, so "nugget" is a typo of "Nugget"; expressions ignore case.
        private static string FindSimilar(string name, IEnumerable<string> candidates, bool caseMatters)
        {
            int allowed = name.Length >= 5 ? 2 : name.Length >= 3 ? 1 : 0;
            string best = null;
            int bestDistance = int.MaxValue;
            foreach (string candidate in candidates.Distinct())
            {
                if (string.IsNullOrEmpty(candidate) || candidate == name) continue;
                if (!caseMatters && string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)) continue;
                int distance = EditDistance(name.ToLowerInvariant(), candidate.ToLowerInvariant());
                if (distance > allowed || distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        // The fewest single-letter insertions, deletions, substitutions or swaps of two neighbouring letters
        // between two words ("Nugett" -> "Nugget" is one swap)
        private static int EditDistance(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
            return d[a.Length, b.Length];
        }

        // What CreateMissing made, kept per character so the window can group it
        public class Created
        {
            public readonly List<(string character, string folder)> Characters = new List<(string, string)>();
            public readonly List<(string character, string path)> Expressions = new List<(string, string)>();
            public bool IsEmpty => Characters.Count == 0 && Expressions.Count == 0;

            public void Add(Created other)
            {
                foreach (var c in other.Characters) if (!Characters.Contains(c)) Characters.Add(c);
                foreach (var e in other.Expressions) if (!Expressions.Contains(e)) Expressions.Add(e);
            }
        }

        // Creates everything the reports list as missing (each character and expression once, even if several csvs
        // need it). Typos are never created. errors lists what failed.
        public static Created CreateMissing(IEnumerable<Report> reports, out List<string> errors)
        {
            errors = new List<string>();
            var created = new Created();
            var reportList = reports.ToList();
            var characters = CharacterObjectsImport.FindAllCharacters();

            var newCharacters = reportList.SelectMany(r => r.MissingCharacters).Distinct().ToList();
            if (newCharacters.Count > 0 && !AssetDatabase.IsValidFolder(TemplateFolder))
            {
                errors.Add($"Template character folder not found at {TemplateFolder}.");
                newCharacters.Clear();
            }

            foreach (string name in newCharacters)
            {
                if (characters.ContainsKey(name)) continue;
                if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    errors.Add($"{name}: the name can't be used as a folder name.");
                    continue;
                }
                CopyTemplate(name);
                characters[name] = CreateCharacterData(name); // picks up the copied sprites and voice
                created.Characters.Add((name, $"{CharactersFolder}/{name}"));
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, expression) in reportList.SelectMany(r => r.MissingExpressions))
            {
                if (!seen.Add($"{name}/{expression}")) continue;
                if (!characters.TryGetValue(name, out CharacterData character) || character == null) continue;
                if (character.GetExpression(expression) != null) continue;

                string path = CreateWhiteExpression(character, expression, out string error);
                if (path != null) created.Expressions.Add((name, path));
                else errors.Add($"{name}: {error}");
            }

            AssetDatabase.SaveAssets();
            return created;
        }

        private static HashSet<string> GetTemplateExpressions()
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string folder = $"{TemplateFolder}/{ExpressionsFolderName}";
            if (!Directory.Exists(folder)) return found;

            foreach (string file in Directory.GetFiles(folder))
                if (!file.EndsWith(".meta") && !Path.GetFileName(file).StartsWith("."))
                    found.Add(Path.GetFileNameWithoutExtension(file));
            return found;
        }

        // Copies every template file into Characters/<name>/, keeping files that are already there
        private static void CopyTemplate(string name)
        {
            string target = $"{CharactersFolder}/{name}";
            foreach (string file in Directory.GetFiles(TemplateFolder, "*", SearchOption.AllDirectories))
            {
                string source = file.Replace('\\', '/');
                string fileName = Path.GetFileName(source);
                if (fileName.EndsWith(".meta") || fileName.StartsWith(".")) continue; // skips .DS_Store too

                string destination = target + source.Substring(TemplateFolder.Length);
                if (File.Exists(destination)) continue;

                EnsureFolder(Path.GetDirectoryName(destination).Replace('\\', '/'));
                AssetDatabase.CopyAsset(source, destination);
            }
        }

        private static CharacterData CreateCharacterData(string name)
        {
            string path = $"{CharactersFolder}/{name}/{name}_data.asset";
            var character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (character == null)
            {
                character = ScriptableObject.CreateInstance<CharacterData>();
                character.speakerName = name;
                character.speakerColor = Color.white; // the default (0,0,0,0) would make the name invisible
                AssetDatabase.CreateAsset(character, path);
            }

            CharacterDataImport.ImportExpressions(character);
            CharacterDataImport.ImportVoice(character);
            return character;
        }

        // A white image the size of the character's Neutral sprite, with Neutral's import settings so it sits where
        // the real sprite will, saved as Expressions/<expression>.png and added to the character's expressions
        private static string CreateWhiteExpression(CharacterData character, string expression, out string error)
        {
            error = null;
            if (expression.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = $"'{expression}' can't be used as a file name.";
                return null;
            }

            // Measured from Neutral, or any sprite of the character if it has no Neutral
            Sprite reference = character.GetExpression(NeutralName)
                               ?? character.expressions?.Select(e => e.sprite).FirstOrDefault(s => s != null);

            int width = FallbackSize, height = FallbackSize;
            float pixelsPerUnit = 100;
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            TextureImporter referenceImporter = null;
            if (reference != null)
            {
                width = Mathf.RoundToInt(reference.rect.width);
                height = Mathf.RoundToInt(reference.rect.height);
                pixelsPerUnit = reference.pixelsPerUnit;
                pivot = new Vector2(reference.pivot.x / reference.rect.width, reference.pivot.y / reference.rect.height);
                referenceImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(reference)) as TextureImporter;
            }

            string folder = CharacterDataImport.GetExpressionsFolder(character);
            EnsureFolder(folder);
            string path = $"{folder}/{expression}.png";

            if (!File.Exists(path)) // an existing file with that name is used as it is
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color32[width * height];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var settings = new TextureImporterSettings();
                if (referenceImporter != null) referenceImporter.ReadTextureSettings(settings);
                settings.textureType = TextureImporterType.Sprite;
                settings.spriteMode = (int)SpriteImportMode.Single; // Neutral may come from a sheet; this is one image
                settings.spritePixelsPerUnit = pixelsPerUnit;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                if (referenceImporter != null) importer.textureCompression = referenceImporter.textureCompression;
                importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, Mathf.Min(16384, Mathf.NextPowerOfTwo(Mathf.Max(width, height))));
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                error = $"{path} is not imported as a Sprite.";
                return null;
            }

            character.expressions ??= new List<ExpressionSprite>();
            character.expressions.RemoveAll(e => string.Equals(e.name, expression, StringComparison.OrdinalIgnoreCase));
            character.expressions.Add(new ExpressionSprite { name = expression, sprite = sprite });
            EditorUtility.SetDirty(character);
            return path;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
