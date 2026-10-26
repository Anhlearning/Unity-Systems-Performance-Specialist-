using System;
using Ignite.Core.Logging;
using Ignite.Unity.Settings;
using UnityEngine;
using VContainer.Unity;
#if IGNITE_LOCALIZATION
using UnityEngine.Localization.Settings;
#endif

namespace Ignite.Unity.Localization
{
    /// <summary>
    /// Bọc Unity Localization: đổi ngôn ngữ theo Settings.Language ("vi", "en"…), lấy chuỗi theo key.
    /// Chỉ hoạt động khi đang chạy game; ở Edit mode, project chưa cài com.unity.localization,
    /// hoặc chưa khởi tạo xong thì trả về chính key.
    /// Không bao giờ chặn main thread chờ khởi tạo: chờ trong Edit mode sẽ treo Editor
    /// (thao tác khởi tạo cần chính vòng update đang bị chặn).
    /// </summary>
    public sealed class LocalizationService : IStartable, IDisposable
    {
        public static readonly LogChannel Log = LogChannels.Register("Localization");
        public const string DefaultTable = "Ignite";

        readonly SettingsService _settings;

        public LocalizationService(SettingsService settings)
        {
            _settings = settings;
        }

        public string CurrentLanguage { get; private set; }

        /// <summary>Phát sau khi đổi ngôn ngữ xong: chữ trên UI cập nhật lại.</summary>
        public event Action LanguageApplied;

        /// <summary>Đã cài Localization, có Localization Settings, đang chạy game.</summary>
        public bool IsAvailable
        {
            get
            {
#if IGNITE_LOCALIZATION
                return Application.isPlaying && LocalizationSettings.HasSettings;
#else
                return false;
#endif
            }
        }

        /// <summary>Sẵn sàng trả chuỗi đã dịch ngay (khởi tạo đã xong).</summary>
        public bool IsReady
        {
            get
            {
#if IGNITE_LOCALIZATION
                return IsAvailable && LocalizationSettings.InitializationOperation.IsDone;
#else
                return false;
#endif
            }
        }

        public void Start()
        {
            if (_settings != null)
            {
                _settings.LanguageChanged += Apply;
                Apply(_settings.Current.Language);
            }
        }

        public void Apply(string languageCode)
        {
            CurrentLanguage = languageCode;
#if IGNITE_LOCALIZATION
            if (IsAvailable)
            {
                var init = LocalizationSettings.InitializationOperation;
                if (init.IsDone)
                    SelectLocale(languageCode);
                else
                    init.Completed += _ => SelectLocale(CurrentLanguage);
                return;
            }
#endif
            LanguageApplied?.Invoke();
        }

#if IGNITE_LOCALIZATION
        void SelectLocale(string languageCode)
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
            if (locale != null)
                LocalizationSettings.SelectedLocale = locale;
            else
                GameLog.Warning(Log, $"Không có locale '{languageCode}' trong Localization Settings.");

            // Nạp sẵn bảng chuỗi của ngôn ngữ mới rồi mới báo UI cập nhật, để Get trả bản dịch ngay.
            var table = LocalizationSettings.StringDatabase.GetTableAsync(DefaultTable);
            if (table.IsDone)
                LanguageApplied?.Invoke();
            else
                table.Completed += _ => LanguageApplied?.Invoke();
        }
#endif

        /// <summary>Chuỗi đã dịch theo ngôn ngữ hiện tại. Chưa sẵn sàng hoặc thiếu bản dịch thì trả về key để dễ phát hiện.</summary>
        public string Get(string key, string table = DefaultTable, params object[] arguments)
        {
#if IGNITE_LOCALIZATION
            if (IsReady)
            {
                var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key, arguments);
                if (operation.IsDone && !string.IsNullOrEmpty(operation.Result))
                    return operation.Result;
            }
#endif
            return arguments.Length > 0 ? $"{key} {string.Join(" ", arguments)}" : key;
        }

        public void Dispose()
        {
            if (_settings != null)
                _settings.LanguageChanged -= Apply;
        }
    }
}
