using UnityEngine;
using UnityEngine.UI;

namespace Ignite.Unity.Localization
{
    /// <summary>Gắn lên Text: hiện chuỗi theo key và tự cập nhật khi đổi ngôn ngữ.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] string key;
        [SerializeField] string table = LocalizationService.DefaultTable;

        LocalizationService _localization;
        Text _text;
        object[] _arguments = System.Array.Empty<object>();

        public string Key => key;

        public void Bind(LocalizationService localization, string entryKey, params object[] arguments)
        {
            if (_localization != null)
                _localization.LanguageApplied -= Refresh;

            _localization = localization;
            key = entryKey;
            _arguments = arguments ?? System.Array.Empty<object>();
            _localization.LanguageApplied += Refresh;
            Refresh();
        }

        public void SetArguments(params object[] arguments)
        {
            _arguments = arguments;
            Refresh();
        }

        void Refresh()
        {
            if (_text == null)
                _text = GetComponent<Text>();
            if (_localization != null)
                _text.text = _localization.Get(key, table, _arguments);
        }

        void OnDestroy()
        {
            if (_localization != null)
                _localization.LanguageApplied -= Refresh;
        }
    }
}
