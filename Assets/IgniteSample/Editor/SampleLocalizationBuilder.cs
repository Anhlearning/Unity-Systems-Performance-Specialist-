using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace IgniteSample.Editor
{
    /// <summary>Tạo Localization Settings, hai locale vi/en và bảng chuỗi "Ignite" cho UI của sample.</summary>
    public static class SampleLocalizationBuilder
    {
        const string Folder = "Assets/IgniteSample/Localization";
        const string Table = "Ignite";

        // key → (Tiếng Việt, English)
        static readonly Dictionary<string, (string vi, string en)> Strings = new Dictionary<string, (string, string)>
        {
            ["menu.title"] = ("IGNITE SAMPLE · MENU", "IGNITE SAMPLE · MAIN MENU"),
            ["menu.play"] = ("Chơi", "Play"),
            ["menu.settings"] = ("Cài đặt", "Settings"),
            ["menu.runs"] = ("Số trận đã chơi: {0}", "Runs played: {0}"),
            ["gameplay.hint"] = ("GAMEPLAY · Esc / nút B để tạm dừng", "GAMEPLAY · Esc / B button to pause"),
            ["pause.title"] = ("TẠM DỪNG", "PAUSED"),
            ["pause.resume"] = ("Chơi tiếp", "Resume"),
            ["pause.menu"] = ("Về Menu", "Main menu"),
            ["settings.title"] = ("CÀI ĐẶT", "SETTINGS"),
            ["settings.master"] = ("Âm lượng chung", "Master volume"),
            ["settings.music"] = ("Nhạc", "Music"),
            ["settings.sfx"] = ("Hiệu ứng", "Effects"),
            ["settings.ui"] = ("Giao diện", "Interface"),
            ["settings.display"] = ("Chế độ màn hình", "Display mode"),
            ["settings.resolution"] = ("Độ phân giải", "Resolution"),
            ["settings.native"] = ("gốc", "native"),
            ["settings.on"] = ("bật", "on"),
            ["settings.off"] = ("tắt", "off"),
            ["settings.quality"] = ("Chất lượng", "Quality"),
            ["settings.language"] = ("Ngôn ngữ", "Language"),
            ["settings.rebind"] = ("Đổi phím Nhảy", "Rebind Jump"),
            ["settings.press"] = ("Bấm phím mới… (Esc để huỷ)", "Press a new key… (Esc to cancel)"),
            ["settings.back"] = ("Quay lại", "Back"),
        };

        [MenuItem("Tools/Ignite/Rebuild Sample Localization")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/IgniteSample", "Localization");

            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, $"{Folder}/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            var vi = GetOrCreateLocale("vi");
            var en = GetOrCreateLocale("en");

            var collection = LocalizationEditorSettings.GetStringTableCollection(Table)
                ?? LocalizationEditorSettings.CreateStringTableCollection(Table, Folder);
            var viTable = (StringTable)collection.GetTable(vi.Identifier);
            var enTable = (StringTable)collection.GetTable(en.Identifier);

            foreach (var pair in Strings)
            {
                Set(viTable, pair.Key, pair.Value.vi);
                Set(enTable, pair.Key, pair.Value.en);
            }

            EditorUtility.SetDirty(viTable);
            EditorUtility.SetDirty(enTable);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Ignite Sample] Localization: {Strings.Count} chuỗi, locale vi + en, bảng '{Table}'.");
        }

        static Locale GetOrCreateLocale(string code)
        {
            var existing = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code));
            if (existing != null)
                return existing;

            var locale = Locale.CreateLocale(new LocaleIdentifier(code));
            AssetDatabase.CreateAsset(locale, $"{Folder}/Locale_{code}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        static void Set(StringTable table, string key, string value)
        {
            var entry = table.GetEntry(key);
            if (entry == null)
                table.AddEntry(key, value);
            else
                entry.Value = value;
        }
    }
}
