// SummaryAndCheck/Services/WebSummaryService.cs
namespace SummaryAndCheck.Services
{
    public static class WebCapturePatches
    {
        public const string JsPatch= @"
(() => {
  'use strict';

  // --- 0. 高階偽裝工具 ---
  const originalToString = Function.prototype.toString;
  const toStringPatchedObjects = new Map();
  Function.prototype.toString = function() {
    if (toStringPatchedObjects.has(this)) {
      return toStringPatchedObjects.get(this);
    }
    return originalToString.apply(this, arguments);
  };

  // --- 1. Webdriver 偽裝 ---
  try {
    const webdriverGetter = () => false;
    Object.defineProperty(Navigator.prototype, 'webdriver', { get: webdriverGetter, configurable: true });
    toStringPatchedObjects.set(webdriverGetter, 'function get webdriver() { [native code] }');
  } catch {}

  // --- 2. window.chrome 偽裝 (完整版) ---
  if (!window.chrome) {
    window.chrome = {};
  }
  Object.assign(window.chrome, {
    runtime: {
      connect: () => ({ onMessage: { addListener: () => {} }, disconnect: () => {} }),
      sendMessage: () => {},
    },
    // **新增 webstore 偽裝**
    webstore: {},
  });


  // --- 3. WebGL 偽裝 (無GPU環境防禦版) ---
  try {
    // 攔截 getContext，在無 GPU 環境下直接回傳 null
    const originalGetContext = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = function(contextType, ...args) {
      if (contextType === 'webgl' || contextType === 'webgl2' || contextType === 'experimental-webgl') {
        return null;
      }
      return originalGetContext.apply(this, [contextType, ...args]);
    };
    
    // 對 WebGLRenderingContext 的原型進行無害化處理，防止腳本崩潰
    if (window.WebGLRenderingContext) {
        const glProto = WebGLRenderingContext.prototype;
        // 將 getParameter 偽裝成總是回傳一些無害的值
        glProto.getParameter = function(parameter) {
            // UNMASKED_VENDOR_WEBGL
            if (parameter === 37445) return 'Google Inc. (Apple)';
            // UNMASKED_RENDERER_WEBGL
            if (parameter === 37446) return 'ANGLE (Apple, Apple M1 Pro, OpenGL 4.1)';
            return null;
        };
    }
  } catch {}

  // --- 4. 偽造資料 ---
  const fakeData = {
    plugins: [
      { name: 'Chrome PDF Plugin', filename: 'internal-pdf-viewer', description: 'Portable Document Format' },
      { name: 'Chrome PDF Viewer', filename: 'internal-pdf-viewer', description: 'Portable Document Format' },
      { name: 'Native Client', filename: 'internal-nacl-plugin', description: '' },
    ],
    mimeTypes: [
      { type: 'application/pdf', suffixes: 'pdf', description: 'Portable Document Format' },
      { type: 'application/x-nacl', suffixes: '', description: '' },
      { type: 'application/x-pnacl', suffixes: '', description: '' },
    ],
  };

  /**
   * 建立一個通用的 Proxy Handler
   */
  const createCollectionProxyHandler = (fakeArray, type, nameKey = 'name') => ({
    get(target, key) {
      // **攔截 constructor 檢查**
      if (key === 'constructor') {
        return type;
      }
      if (key === Symbol.toStringTag) {
        return type.name;
      }
      if (key === 'length') {
        return fakeArray.length;
      }
      if (key === 'item') {
        return (index) => fakeArray[index];
      }
      if (key === 'namedItem') {
        return (name) => fakeArray.find(item => item[nameKey] === name) || null;
      }
      if (Symbol.iterator === key) {
        return fakeArray[Symbol.iterator]();
      }
      if (typeof key === 'string' && /^\d+$/.test(key)) {
        return fakeArray[parseInt(key, 10)];
      }
      const value = Reflect.get(target, key);
      return typeof value === 'function' ? value.bind(target) : value;
    },
    ownKeys: () => Array.from({ length: fakeArray.length }, (_, i) => String(i)).concat(['length', 'item', 'namedItem']),
    getOwnPropertyDescriptor(target, key) {
      if (this.ownKeys(target).includes(key)) {
        return { value: this.get(target, key), writable: false, enumerable: true, configurable: true };
      }
      return Reflect.getOwnPropertyDescriptor(target, key);
    }
  });

  // --- 5. 建立並應用 Navigator Proxy ---
  const navigatorProxyHandler = {
    get(target, key) {
      switch (key) {
        case 'webdriver': return false;
        case 'plugins': return new Proxy(target.plugins, createCollectionProxyHandler(fakeData.plugins, window.PluginArray));
        case 'mimeTypes': return new Proxy(target.mimeTypes, createCollectionProxyHandler(fakeData.mimeTypes, window.MimeTypeArray, 'type'));
        case 'languages': return ['zh-TW', 'zh', 'en-US', 'en'];
        case 'language': return 'zh-TW';
        case 'platform': return 'Win32';
        case 'vendor': return 'Google Inc.';
        case 'sayswho': return 'Chrome';
        case 'hardwareConcurrency': return 8;
        case 'deviceMemory': return 8;
        case 'getBattery': return () => Promise.resolve({ charging: true, chargingTime: 0, dischargingTime: Infinity, level: 1 });
        default:
          const value = Reflect.get(target, key);
          return typeof value === 'function' ? value.bind(target) : value;
      }
    },
    has: (target, key) => key !== 'webdriver' && Reflect.has(target, key)
  };

  const navigatorProxy = new Proxy(window.navigator, navigatorProxyHandler);

  Object.defineProperty(window, 'navigator', {
    value: navigatorProxy,
    writable: false,
    configurable: false,
    enumerable: true
  });
try {
  const originalCanPlayType = HTMLMediaElement.prototype.canPlayType;
  const newCanPlayType = function(type) {
    if (type && (type.includes('h264') || type.includes('aac') || type.includes('avc1'))) {
      return 'probably';
    }
    // 對於其他類型，仍使用原始函式，避免破壞正常功能
    return originalCanPlayType.apply(this, arguments);
  };
  
  HTMLMediaElement.prototype.canPlayType = newCanPlayType;
  
  // 同樣地，偽裝 toString()
  toStringPatchedObjects.set(newCanPlayType, 'function canPlayType() { [native code] }');
} catch (e) {
  console.error('Failed to patch canPlayType', e);
}
})();
";
    }
}