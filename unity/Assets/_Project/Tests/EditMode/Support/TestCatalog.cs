using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Tests
{
    // Danh sách level xuất từ prototype, đọc một lần cho mọi test.
    internal static class TestCatalog
    {
        private static readonly Lazy<List<LevelDefinition>> levels = new Lazy<List<LevelDefinition>>(Load);

        public static string FilePath => Path.Combine(Application.dataPath, "_Project", "Levels", "LevelCatalog.txt");

        public static IReadOnlyList<LevelDefinition> Levels => levels.Value;

        public static LevelDefinition Find(string id) => Levels.Single(level => level.Id == id);

        private static List<LevelDefinition> Load() => LevelCatalogParser.Parse(File.ReadAllText(FilePath));
    }
}
