# **Hcs.LightI18n**

Hcs.LightI18n is a lightweight and high-performance .NET Internationalization (i18n) library, designed for modern C# applications, supporting .NET 10.0 and higher. It provides a concise API for handling multi-language translations, dynamic parameter formatting, and scoped language management, integrated with ASP.NET Core's configuration and dependency injection mechanisms.

## **Features**

* **Concise API Design**: Provides intuitive translation methods via the static L class, and can be used with string extension methods.  
* **Dynamic Parameter Replacement and Formatting**: Supports {{ParameterName}} or {{ParameterName:FormatString}} syntax in translation strings, automatically replacing object properties with formatted strings (e.g., currency, dates). To output literal `{{ParameterName}}` without replacement, use `\{{ParameterName}}`.  
* **Scoped Language Management**: Utilizes AsyncLocal to implement a language and scope prefix stack, automatically passing the current translation context in asynchronous operations.  
* **IConfiguration-Based Translation Data**: Translation content can be managed through .NET's configuration system (e.g., appsettings.json), supporting hot reloading.  
* **High-Performance Caching**: Built-in memory cache (MemoryCache) for caching parsed string segments, reducing redundant parsing overhead.  
* **Flexible Language Fallback Mechanism**: Automatically handles language ID fallback logic (e.g., en-US can fall back to en).  
* **Integration with Microsoft.Extensions.DependencyInjection**: Provides extension methods to simplify setup and startup in ASP.NET Core applications.

## **Installation**

1. Install the NuGet package in your project:  
```dotnet add package Hcs.LightI18n```

## **Usage**

### **1. Service Registration**

In your Program.cs or Startup.cs, add the LightI18n service to the dependency injection container:
```csharp
using Hcs.LightI18n;  
using Microsoft.Extensions.Hosting; // Requires this namespace for IHostBuilder

public class Program  
{  
    public static void Main(string[] args)  
    {  
        CreateHostBuilder(args).Build().Run();  
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>  
        Host.CreateDefaultBuilder(args)  
            .ConfigureServices((hostContext, services) =>  
            {  
                // Register LightI18n service  
                // "Localization" is the root path prefix for translation data in the configuration file  
                services.AddLightI18n("Localization");  
                  
                // Other service registrations...  
            });  
}
```
### **2. Configure Translation Content**

Hcs.LightI18n expects translation content to be stored in appsettings.json or other IConfiguration sources. For example:

**appsettings.json**
```json
{  
  "Localization": {  
    "en-US": {  
      "WelcomeMessage": "Hello, {{Name}}! Today is {{CurrentDate:D}}.",  
      "Greeting": "Welcome",  
      "Dashboard": {  
        "WidgetTitle": "Dashboard Widget (Count: {{Count}})"  
      }  
    },  
    "zh-TW": {  
      "WelcomeMessage": "哈囉，{{Name}}！今天是 {{CurrentDate:D}}。",  
      "Greeting": "歡迎",  
      "Dashboard": {  
        "WidgetTitle": "儀表板小工具 (數量: {{Count}})"  
      }  
    }  
  }  
}
```
In the example above, "Localization" is the pathPrefix you configured in AddLightI18n. "en-US" and "zh-TW" are language IDs, under which are the actual translation key-value pairs.

**Using additional JSON files for translation content:**
You can also use additional JSON files for translation content, which can be useful for separating translations or managing different environments. For example, in your `Program.cs` or `Startup.cs`, you can add:
```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("local.json", optional: false, reloadOnChange: true);
        config.AddJsonFile("local.zh-TW.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLightI18n("Localization");
    });
```

### **3. Performing Translations**

You can perform translations via the static class L.

#### **Basic Translation**
```csharp
using Hcs.LightI18n;  
using Hcs.LightI18n.Core; // For ILocalizationService  
using System.Globalization;

// When using Host.CreateDefaultBuilder().ConfigureServices(services => services.AddLightI18n()),  
// L.Initialize is automatically called by an internal service, usually no manual initialization is needed.  
// If you are using it without an IHostBuilder environment, you will need to call it manually:  
// L.Initialize(serviceProvider.GetRequiredService<ILocalizationService>());

// Translate a simple string  
string greeting = L.T("zh-TW", "Greeting"); // "歡迎"  
Console.WriteLine(greeting);

// When no language is set in the current scope, the first language from the configuration file is used as default  
string defaultGreeting = L.T("Greeting"); // Could be "Hello, {{Name}}! Today is {{CurrentDate:D}}." or "歡迎" depending on config order  
Console.WriteLine(defaultGreeting);
```
#### **Translation with Parameters and Formatting**

Translation strings can include parameters and support standard .NET format strings.  
For example, if appsettings.json has "WelcomeMessage": "Hello, {{Name}}! Today is {{CurrentDate:D}}."  
```csharp
using Hcs.LightI18n;  
using System;  
using System.Globalization;

var args = new { Name = "John Doe", CurrentDate = DateTime.Now };

// Translate using en-US culture  
string welcomeEn = L.T("en-US", "WelcomeMessage", args);  
Console.WriteLine(welcomeEn); // Example output: "Hello, John Doe! Today is Tuesday, July 13, 2025."

// Translate using zh-TW culture  
string welcomeZh = L.T("zh-TW", "WelcomeMessage", args);  
Console.WriteLine(welcomeZh); // Example output: "哈囉，John Doe！今天是 2025年7月13日 星期二。"

// Handle currency formatting for numbers  
var priceArgs = new { amount = 1234.56m };  
string priceEn = L.T("en-US", "Price: {{amount:C2}}", priceArgs);  
Console.WriteLine(priceEn); // Example output: "Price: $1,234.56"

string priceZh = L.T("zh-TW", "Price: {{amount:C2}}", priceArgs);  
Console.WriteLine(priceZh); // Example output: "Price: NT$1,234.56" (depends on system default currency symbol)
```
#### **Using String Extension Methods**

For convenience, you can call the .T() extension method directly on a string. This will automatically use the language from the current AsyncLocal scope.
```csharp
using Hcs.LightI18n;

// Assuming a scope has been set up  
// using (L.GetScope("en-US")) { ... }

string message = "WelcomeMessage".T(new { Name = "Alice", CurrentDate = DateTime.Now });  
Console.WriteLine(message); // Output based on the current scope's language
```
#### **Scope Management**

Hcs.LightI18n supports managing language scopes via GetScope and GetChildScope, which is very useful when dealing with code blocks that require specific translation contexts, especially in asynchronous programming.
```csharp
using Hcs.LightI18n;  
using System;  
using System.Threading.Tasks;

// Global or default language (assuming zh-TW is the first in appsettings.json for this example)  
Console.WriteLine($"Default: {L.T("Greeting")}"); // Output: 歡迎

// Create an English scope  
using (L.GetScope("en-US"))  
{  
    Console.WriteLine($"In en-US scope: {L.T("Greeting")}"); // Output: Welcome

    // Create a child scope within the English scope, and set a scope prefix  
    // This will look for keys like "Localization:en-US:Dashboard:WidgetTitle"  
    using (L.GetChildScope("Dashboard"))  
    {  
        Console.WriteLine($"In en-US child scope (Dashboard): {L.T("WidgetTitle", new { Count = 5 })}");   
        // Output: In en-US child scope (Dashboard): Dashboard Widget (Count: 5)

        // Asynchronous operations will inherit the current scope  
        await Task.Run(() =>  
        {  
            Console.WriteLine($"In async task (en-US, Dashboard): {L.T("Greeting")}"); // Output: Welcome  
        });  
    }  
    Console.WriteLine($"Back in en-US scope: {L.T("Greeting")}"); // Output: Welcome  
}

// After the scope ends, it reverts to the outer language  
Console.WriteLine($"After en-US scope: {L.T("Greeting")}"); // Output: 歡迎
```
#### **Get Translations Across All Locales**

The GetMap method can retrieve translations for a specified path across all supported languages:
```csharp
using Hcs.LightI18n;  
using System.Collections.Generic;

IDictionary<string, string> allGreetings = L.GetMap("Greeting");  
foreach (var entry in allGreetings)  
{  
    Console.WriteLine($"Language: {entry.Key}, Translation: {entry.Value}");  
}  
// Example output:  
// Language: en-US, Translation: Welcome  
// Language: zh-TW, Translation: 歡迎
```
## **ASP.NET Core Integration**

Hcs.LightI18n.AspNetCore provides an ASP.NET Core middleware for automatically resolving the language from HTTP Requests and setting the LightI18n scope. **Note that Hcs.LightI18n.AspNetCore is a separate NuGet package and needs to be installed independently.**

### **Using the UseLightI18nLocalization Middleware**

In your Program.cs or Startup.cs, add the UseLightI18nLocalization middleware to your application's request pipeline:
```csharp
using Hcs.LightI18n.AspNetCore; // Import the namespace

public class Program  
{  
    public static void Main(string[] args)  
    {  
        var builder = WebApplication.CreateBuilder(args);

        // ... Other service registrations (including services.AddLightI18n("Localization");)

        var app = builder.Build();

        // Add LightI18n's localization middleware to the application's Request Pipeline.  
        // This middleware automatically resolves the language from Request Headers (Accept-Language) or Query String (lang).  
        app.UseLightI18nLocalization();

        // ... Other middleware and routing configurations

        app.Run();  
    }  
}
```
#### **Middleware Behavior**

The UseLightI18nLocalization middleware checks the following sources in order to determine the current request's language:

1. **Query String**: It first checks for the lang key in the URL query parameters (e.g., ?lang=en-US).  
2. **Accept-Language Header**: If lang is not found in the query parameters, it then checks the Accept-Language HTTP Request Header (e.g., Accept-Language: en-US,en;q=0.9,zh-TW;q=0.8) and takes the first language.

If a language is successfully resolved, it creates an L.GetScope(currentLang) scope, ensuring that all L.T() calls within that request's lifecycle will use the resolved language. If no language is found, no specific scope is set, and L.T() will use its default fallback logic.
