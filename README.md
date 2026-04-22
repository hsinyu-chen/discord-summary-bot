# SummaryAndCheck

一個以 .NET 10 撰寫的 Discord Bot，接收使用者指令後呼叫 Google Gemini 對 YouTube 影片或網頁內容進行摘要。附帶一個 Blazor Server 的後台 (AdminInterface) 管理使用者與額度。

> 這是內部專案。README 只描述現況，不涵蓋未實作的功能。
>
> **狀態**：Bot 主程式可用。後台 (AdminInterface) 目前仍 **WIP**，登入 / Passkey / 使用者與額度管理等頁面只有部分能動，不建議直接拿去給終端使用者用。

## 專案組成

Solution 內分成幾個專案：

| Project | 類型 | 說明 |
|---|---|---|
| [SummaryAndCheck/](SummaryAndCheck/) | Console Host | 主程式。Discord Bot 本體、Gemini 呼叫、網頁擷取、排程。 |
| [SummaryAndCheck.Models/](SummaryAndCheck.Models/) | Class Library | EF Core `DbContext`、Entity、Migrations。 |
| [AdminInterface/](AdminInterface/) | Blazor Server | 後台：帳號、額度、Passkey (WebAuthn) 登入。 |
| [Hcs.Discord/](Hcs.Discord/) | Class Library | Discord Slash Command 的註冊抽象層，基於 Discord.Net。 |
| [Hcs.LightI18n/](Hcs.LightI18n/) | Class Library | 輕量本地化元件，從 JSON 讀取語系字串。見 [Hcs.LightI18n/README.md](Hcs.LightI18n/README.md)。 |
| [Hcs.LightI18n.AspNetCore/](Hcs.LightI18n.AspNetCore/) | Class Library | LightI18n 的 ASP.NET Core 整合。 |
| [Hcs.LightI18n.Tests/](Hcs.LightI18n.Tests/) | xUnit | LightI18n 的測試。 |

## 需求

- .NET SDK 10.0
- PostgreSQL (一個主資料庫 + 一個寫 Serilog log 的資料庫，可以是同一台)
- Google Gemini API Key
- Discord Bot Token
- **Playwright + Chromium**：`WebCaptureService` 用 Playwright 抓網頁內容（含繞過部分 bot 偵測的 patch），所以本機第一次跑之前要先裝 browser。先 build 一次，然後執行 Playwright 的安裝 script：

  ```bash
  dotnet build SummaryAndCheck
  pwsh SummaryAndCheck/bin/Debug/net10.0/playwright.ps1 install chromium
  # 或 Linux/Mac：
  # ./SummaryAndCheck/bin/Debug/net10.0/playwright.sh install chromium
  ```

  沒裝的話跑起來會在第一次呼叫網頁摘要時噴錯。Docker image 不需要這步，因為 base image 已經內建 browser（見下方 [Docker](#docker)）。

## 組態

組態分成三層：

1. **連線字串**：`appsettings.json` 內的 `ConnectionStrings:Postgres` 與 `ConnectionStrings:PostgresLog`。公開 repo 這兩個值是空字串，請用 `local.json`、user-secrets 或 `ConnectionStrings__Postgres` 環境變數覆寫。`local.json` 已被 csproj 設定 `CopyToOutputDirectory`，放本機連線不會被不小心漏掉。
2. **本地化字串**：`local.json` 與 `local.zh-TW.json`。格式見檔案內容，走 `Hcs.LightI18n`。
3. **執行期參數 (Gemini / Discord)**：存在 Postgres 的 `SystemConfig` 表。Scope = Options 類別名稱，Key = 屬性名稱。需要先手動 insert：

   ```sql
   -- Scope 為 Options 類別名，Key 為 property 名
   INSERT INTO "SystemConfig" ("Scope", "Key", "Value") VALUES
     ('DiscordOptions', 'ApiKey',  '<discord-bot-token>'),
     ('GeminiOptions',  'ApiKey',  '<gemini-api-key>'),
     ('GeminiOptions',  'Model',   'gemini-2.5-flash'),
     ('GeminiOptions',  'Prompts', '<system prompt for video>'),
     ('GeminiOptions',  'WebPrompts', '<system prompt for web>');
   ```

   這個對應在 [DbConfigureOptions.cs](SummaryAndCheck/DbConfigureOptions.cs)，可以用 `[ConfigKey]` 改 Key，用 `[IgnoreConfig]` 跳過屬性。

環境變數 (`DOTNET_` prefix)、命令列參數也會被吃進去，優先序依 `Host.CreateDefaultBuilder` 標準行為。

## 資料庫

啟動時 Bot 會呼叫 `DbContext.Database.EnsureCreatedAsync()`（見 [Main.cs:17](SummaryAndCheck/Main.cs#L17)），第一次會建表。Log sink 也會自動建立 `SummaryAndCheck` 這張 log table（見 [Program.cs:54-69](SummaryAndCheck/Program.cs#L54-L69)）。

EF Core Migrations 目前存在 [SummaryAndCheck.Models/Migrations/](SummaryAndCheck.Models/Migrations/)。若要改 schema，走正常 migration 流程：

```bash
dotnet ef migrations add <Name> -p SummaryAndCheck.Models -s SummaryAndCheck
dotnet ef database update       -p SummaryAndCheck.Models -s SummaryAndCheck
```

注意 `EnsureCreated` 和 Migrations 不能混用，目前實際生效的是 `EnsureCreated`。

## 執行

### Bot 主程式

```bash
dotnet run --project SummaryAndCheck
```

### 後台 (WIP)

```bash
dotnet run --project AdminInterface
```

預設走 Cookie 驗證，登入頁 `/LoginPage`，計畫用 Passkey (WebAuthn)。與 Bot 共用同一個 Postgres。

目前還在施工中 — 頁面結構、登入流程、SystemConfig 編輯頁面都只有雛形，不要當成完成品。

### AntiBot 除錯模式

[SummaryAndCheck.csproj](SummaryAndCheck/SummaryAndCheck.csproj) 有一個 `AntiBot` Configuration。用這個組建會跑 [Program.cs:19-30](SummaryAndCheck/Program.cs#L19-L30) 的 code path，直接把 `WebCaptureService` 對 `bot.sannysoft.com` 的擷取結果印出來，用來驗證 Playwright 繞過偵測的設定是否有效。正常執行不會用到。

```bash
dotnet run --project SummaryAndCheck -c AntiBot
```

## Docker

`Dockerfile` 在 repo 根目錄。會比一般 .NET 的 Dockerfile 長，是為了處理 Playwright 的依賴，結構大致是：

1. **Build stage**：`mcr.microsoft.com/dotnet/sdk:10.0` 編譯 & `dotnet publish`。
2. **Runtime stage**：`mcr.microsoft.com/playwright/dotnet:v1.58.0-noble`。這個 base 已經包含 Chromium 和所有系統依賴 (fonts、X libs 等)，省掉自己裝 browser 的麻煩。
3. 但 Playwright 官方 image 只附 .NET runtime 9（截至 `v1.58.0-noble`），本專案目標是 .NET 10，所以用 `dotnet-install.sh` 把 .NET 10 runtime 裝到 `/usr/share/dotnet`。
4. 用 root 執行上述安裝後，把 `/app` 的 owner 改回 base image 內建的 `pwuser` 再切過去跑。

如果之後升級到 Playwright 官方支援 .NET 10 的 image tag，就可以把第 3 步整段拿掉。

手動建：

```bash
docker build -t summary-and-check:dev .
```

或用提供的 PowerShell script，它會從 csproj 讀 `<ProductVersion>` 打 tag 並 `docker save` 成 tar：

```powershell
./build_save_docker.ps1
```

產出檔案格式為 `summary-and-check_<version>.tar`（repo 內已有幾個歷史版本的 tar）。

執行時記得把組態掛進去：

```bash
docker run --rm \
  -v $(pwd)/local.json:/app/local.json:ro \
  -e ConnectionStrings__Postgres="..." \
  -e ConnectionStrings__PostgresLog="..." \
  summary-and-check:dev
```

## 目錄速查

- 指令實作：[SummaryAndCheck/DiscordCommand/](SummaryAndCheck/DiscordCommand/)
- Gemini / 排程 / 佇列：[SummaryAndCheck/Services/](SummaryAndCheck/Services/)
- 網頁擷取（Playwright + Readability.js）：[WebCaptureService.cs](SummaryAndCheck/Services/WebCaptureService.cs)、[assets/Readability.js](SummaryAndCheck/assets/Readability.js)
- Entity：[SummaryAndCheck.Models/](SummaryAndCheck.Models/)

## 測試

目前只有 LightI18n 有單元測試：

```bash
dotnet test Hcs.LightI18n.Tests
```

主程式與後台沒有自動化測試。
