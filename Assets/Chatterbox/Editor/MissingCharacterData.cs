#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefaultNamespace;
using UnityEditor;
using UnityEngine;

namespace Chatterbox.Editor
{
    /*
     * Finds the character data a screenplay csv needs but the project doesn't have, and creates it:
     *  - a speaker with no CharacterData  -> Characters/<Name>/ copied from Characters/0_template,
     *                                         plus <Name>_data.asset with the template's expressions and voice imported
     *  - an expression a character has no sprite for -> the character's Neutral sprite (or the template's)
     *                                         copied into its Expressions/ folder as <Expression>.png
     * Expressions that aren't in the Expression enum are skipped: there is no sprite name to create for them.
     */
    public static class MissingCharacterData
    {
        public const string CharactersFolder = "Assets/Chatterbox/Characters";
        public const string TemplateFolder = CharactersFolder + "/0_template";
        private const string ExpressionsFolderName = "Expressions";

        public class Report
        {
            public readonly List<string> MissingCharacters = new List<string>();
            public readonly List<(string character, Expression expression)> MissingExpressions = new List<(string, Expression)>();
            public bool IsEmpty => MissingCharacters.Count == 0 && MissingExpressions.Count == 0;
        }

        // characters: from CharacterObjectsImport.FindAllCharacters(), passed in so several csvs share one lookup
        public static Report Check(string csvText, Dictionary<string, CharacterData> characters)
        {
            var report = new Report();
            var templateExpressions = GetTemplateExpressions();

            // Speakers in csv order, each with the expressions they use (no repeats)
            var used = new List<(string speaker, List<Expression> expressions)>();
            foreach (var (speaker, expressionField) in ScreenPlayCsvImporter.ReadRows(csvText))
            {
                if (string.IsNullOrEmpty(speaker)) continue;

                int index = used.FindIndex(u => u.speaker == speaker);
                if (index < 0)
                {
                    used.Add((speaker, new List<Expression>()));
                    index = used.Count - 1;
                }

                if (Enum.TryParse(expressionField, true, out Expression expression)
                    && expression != Expression.Unrecognised
                    && !used[index].expressions.Contains(expression))
                    used[index].expressions.Add(expression);
            }

            foreach (var (speaker, expressions) in used)
            {
                if (!characters.TryGetValue(speaker, out CharacterData character))
                {
                    report.MissingCharacters.Add(speaker);
                    // A new character gets the template's sprites; only expressions the template lacks are missing
                    foreach (var expression in expressions)
                        if (!templateExpressions.Contains(expression))
                            report.MissingExpressions.Add((speaker, expression));
                    continue;
                }

                foreach (var expression in expressions)
                    if (!HasSprite(character, expression))
                        report.MissingExpressions.Add((speaker, expression));
            }

            return report;
        }

        // Creates everything in the reports (each character and expression once, even if several csvs need it).
        // Returns the created folders and files; errors lists what could not be created.
        public static List<string> CreateMissing(IEnumerable<Report> reports, out List<string> errors)
        {
            errors = new List<string>();
            var created = new List<string>();
            var reportList = reports.ToList();

            if (!AssetDatabase.IsValidFolder(TemplateFolder))
            {
                errors.Add($"Template character folder not found at {TemplateFolder}.");
                return created;
            }

            var characters = CharacterObjectsImport.FindAllCharacters();

            foreach (string name in reportList.SelectMany(r => r.MissingCharacters).Distinct())
            {
                if (characters.ContainsKey(name)) continue;
                if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    errors.Add($"{name}: the name can't be used as a folder name.");
                    continue;
                }
                CopyTemplate(name);
                characters[name] = CreateCharacterData(name); // picks up the copied sprites and voice
                created.Add($"{CharactersFolder}/{name}");
            }

            foreach (var (name, expression) in reportList.SelectMany(r => r.MissingExpressions).Distinct())
            {
                if (!characters.TryGetValue(name, out CharacterData character) || character == null) continue;
                if (HasSprite(character, expression)) continue;

                string path = CreateExpressionSprite(character, expression, out string error);
                if (path != null) created.Add(path);
                else errors.Add($"{name}: {error}");
            }

            AssetDatabase.SaveAssets();
            return created;
        }

        private static bool HasSprite(CharacterData character, Expression expression) =>
            character.expressions != null && character.expressions.Any(e => e.type == expression && e.sprite != null);

        private static HashSet<Expression> GetTemplateExpressions()
        {
            var found = new HashSet<Expression>();
            string folder = $"{TemplateFolder}/{ExpressionsFolderName}";
            if (!Directory.Exists(folder)) return found;

            foreach (string file in Directory.GetFiles(folder))
                if (!file.EndsWith(".meta") && Enum.TryParse(Path.GetFileNameWithoutExtension(file), true, out Expression expression))
                    found.Add(expression);
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

        // Copies the character's Neutral sprite (or the template's) to Expressions/<Expression>.png and adds it to the data
        private static string CreateExpressionSprite(CharacterData character, Expression expression, out string error)
        {
            error = null;
            string source = null;
            var neutral = character.expressions?.FirstOrDefault(e => e.type == Expression.Neutral).sprite;
            if (neutral != null) source = AssetDatabase.GetAssetPath(neutral);
            if (string.IsNullOrEmpty(source))
                source = Directory.GetFiles($"{TemplateFolder}/{ExpressionsFolderName}")
                    .Select(f => f.Replace('\\', '/'))
                    .FirstOrDefault(f => !f.EndsWith(".meta")
                        && Path.GetFileNameWithoutExtension(f).Equals(nameof(Expression.Neutral), StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(source))
            {
                error = $"no Neutral sprite to copy for {expression}.";
                return null;
            }

            string folder = CharacterDataImport.GetExpressionsFolder(character);
            EnsureFolder(folder);
            string destination = $"{folder}/{expression}{Path.GetExtension(source)}";
            if (!File.Exists(destination) && !AssetDatabase.CopyAsset(source, destination))
            {
                error = $"could not copy {source} to {destination}.";
                return null;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(destination);
            if (sprite == null)
            {
                error = $"{destination} is not imported as a Sprite.";
                return null;
            }

            character.expressions ??= new List<ExpressionSprite>();
            character.expressions.RemoveAll(e => e.type == expression); // drops an entry left without a sprite
            character.expressions.Add(new ExpressionSprite { type = expression, sprite = sprite });
            EditorUtility.SetDirty(character);
            return destination;
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
