using Hcs.LightI18n.Core;
using System.Collections.Immutable;

namespace Hcs.LightI18n
{
    public record LanguageScopeData(string Lang, string ScopePrefix);

    public static class L
    {
        private static ILocalizationService? _service;

        private static readonly AsyncLocal<ImmutableStack<LanguageScopeData>> _asyncLocalScopeStack = new();

        /// <summary>
        /// 初始化 L 靜態類別，設定底層的本地化服務實例。
        /// 應在應用程式啟動時呼叫一次。
        /// </summary>
        /// <param name="service">ILocalizationService 的實例。</param>
        public static void Initialize(ILocalizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service), "Localization service cannot be null.");
        }

        /// <summary>
        /// 獲取翻譯文本。會自動從當前作用域 (AsyncLocal) 取得語系和範圍前綴。
        /// </summary>
        /// <param name="path">翻譯 Key 的相對路徑。</param>
        /// <param name="args">具名參數物件。</param>
        /// <returns>翻譯後的字串。如果服務未初始化或翻譯未找到，則回傳原始路徑。</returns>
        public static string T(string path, params object[] args)
        {
            if (_service == null)
            {
                return path;
            }

            var currentScopeData = _asyncLocalScopeStack.Value?.Peek();

            string langToUse;
            string scopePrefixToUse;

            if (currentScopeData != null)
            {
                langToUse = currentScopeData.Lang;
                scopePrefixToUse = currentScopeData.ScopePrefix;
            }
            else
            {
                langToUse = string.Empty;
                scopePrefixToUse = string.Empty;
            }

            var finalPath = string.IsNullOrEmpty(scopePrefixToUse) ? path : $"{scopePrefixToUse}:{path}";
            return _service.Get(langToUse, finalPath, args) ?? path;
        }

        /// <summary>
        /// 獲取指定語系下的翻譯文本。此方法用於不使用作用域時，直接指定語系。
        /// </summary>
        /// <param name="lang">指定的語系 ID。</param>
        /// <param name="path">翻譯 Key 的路徑。</param>
        /// <param name="args">具名參數物件。</param>
        /// <returns>翻譯後的字串。</returns>
        public static string T(string lang, string path, params object[] args)
        {
            if (_service == null)
            {
                return path;
            }
            return _service.Get(lang, path, args) ?? path;
        }

        /// <summary>
        /// 獲取指定翻譯 Key 在所有語系下的翻譯映射。
        /// </summary>
        /// <param name="path">翻譯 Key 的路徑。</param>
        /// <param name="args">具名參數物件。</param>
        /// <returns>一個字典，Key 是語系名稱，Value 是翻譯文本。</returns>
        public static IDictionary<string, string> GetMap(string path, params object[] args)
        {
            // 如果服務未初始化，回傳空字典並警告
            if (_service == null)
            {
                return new Dictionary<string, string>();
            }
            return _service.GetMap(path, args);
        }

        /// <summary>
        /// 建立一個新的翻譯作用域。在此作用域內，L.T() 將使用指定的語系和範圍前綴。
        /// </summary>
        /// <param name="lang">此作用域的語系 ID。</param>
        /// <param name="scopePrefix">此作用域的範圍前綴 (可選)。</param>
        /// <returns>一個可處置的物件，當其被 Dispose 時會恢復到前一個作用域。</returns>
        public static IDisposable GetScope(string lang, string scopePrefix = "")
        {
            var originalStack = _asyncLocalScopeStack.Value ??= [];
            var newScopeData = new LanguageScopeData(lang, scopePrefix);

            _asyncLocalScopeStack.Value = originalStack.Push(newScopeData);

            // Dispose 時恢復到原來的堆疊
            return new Disposable(() => _asyncLocalScopeStack.Value = originalStack);
        }
        /// <summary>
        /// 建立一個新的翻譯子作用域，繼承當前作用域的語系，並在現有範圍前綴基礎上附加新的前綴。
        /// 如果沒有父作用域，則語系為空字串，前綴為單獨的 childScopePrefix。
        /// </summary>
        /// <param name="childScopePrefix">要附加到現有前綴的新子前綴。</param>
        /// <returns>一個可處置的物件，當其被 Dispose 時會恢復到前一個作用域。</returns>
        public static IDisposable GetChildScope(string childScopePrefix)
        {
            var originalStack = _asyncLocalScopeStack.Value ??= [];
            var currentScopeData = originalStack.IsEmpty ? null : originalStack.Peek(); // 檢查是否有父作用域

            string langToUse;
            string newFullScopePrefix;

            if (currentScopeData != null)
            {
                langToUse = currentScopeData.Lang;
                newFullScopePrefix = string.IsNullOrEmpty(currentScopeData.ScopePrefix)
                                     ? childScopePrefix
                                     : $"{currentScopeData.ScopePrefix}.{childScopePrefix}";
            }
            else
            {
                langToUse = string.Empty;
                newFullScopePrefix = childScopePrefix;
            }

            var newScopeData = new LanguageScopeData(langToUse, newFullScopePrefix);
            _asyncLocalScopeStack.Value = originalStack.Push(newScopeData);

            return new Disposable(() => _asyncLocalScopeStack.Value = originalStack);
        }

        private class Disposable(Action onDispose) : IDisposable
        {
            public void Dispose() => onDispose();
        }
    }
    // <summary>
    /// 提供字串的擴充方法，用於簡化翻譯呼叫。
    /// </summary>
    public static class StringLocalizationExtensions
    {
        /// <summary>
        /// 在設定的作用域內，將當前字串作為翻譯 Key 進行翻譯。
        /// </summary>
        /// <param name="path">翻譯 Key。</param>
        /// <param name="args">具名參數物件。</param>
        /// <returns>翻譯後的字串。</returns>
        public static string T(this string path, params object[] args)
        {
            // 直接呼叫 L 靜態類別的 T 方法，該方法會自動從 AsyncLocal 作用域中獲取上下文
            return L.T(path, args);
        }
        public static string T(this string path, string lang, params object[] args)
        {
            return L.T(lang, path, args);
        }
    }
}

