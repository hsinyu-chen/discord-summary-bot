# build-and-save-docker-with-csproj-version.ps1

# --- 設定區塊 ---
$csprojFilePath = ".\SummaryAndCheck\SummaryAndCheck.csproj" # <--- 請務必更改為您實際的 .csproj 檔案路徑
$imageName = "summary-and-check" # <--- Docker 映像檔的名稱
# -----------------

# 函式：從 .csproj 讀取版本號
function Get-VersionFromCsproj {
    param (
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        Write-Host "找不到指定的 .csproj 檔案：'$Path'。"  -ForegroundColor Red
        return $null
    }

    try {
        [xml]$csprojContent = Get-Content $Path
        $xpaths = @(
            '/Project/PropertyGroup/ProductVersion',
            '/Project/PropertyGroup/Version',
            '/Project/PropertyGroup/AssemblyVersion',
            '/Project/PropertyGroup/FileVersion'
        )
        foreach ($xpath in $xpaths) {
            $node = $csprojContent.SelectSingleNode($xpath)
            if ($null -ne $node) {
                $version = $node.InnerText

                if (-not ([string]::IsNullOrWhiteSpace($version))) {
                    return $version -replace '^\s+|\s+$'
                }
            }
        }
        Write-Host "在 '$Path' 中找不到任何已知的版本節點 (例如 <ProductVersion>, <Version>, <AssemblyVersion>, <FileVersion>)。"  -ForegroundColor Yellow
        return $null
    }
    catch {
        Write-Host "解析 '$Path' 檔案時發生錯誤：$($_.Exception.Message)。請檢查 .csproj 檔案格式是否正確。"  -ForegroundColor Red
        return $null
    }
}

# --- 主要執行邏輯 ---

Write-Host "--- Docker 自動建置與儲存 (使用 .csproj 版本) ---" -ForegroundColor Cyan

# 嘗試從 .csproj 讀取版本號
$version = Get-VersionFromCsproj -Path $csprojFilePath
if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Host $csprojFilePath
    Write-Host "無法從 .csproj 讀取版本。請檢查您的 .csproj 檔案是否包含版本資訊。" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "✅ 成功從 .csproj 讀取版本號: $($version)" -ForegroundColor Green
}

$tag = "${imageName}:$version"
$outputFile = "${imageName}_${version}.tar"

Write-Host "`n📁 映像檔資訊：" -ForegroundColor Blue
Write-Host "  名稱: $($imageName)"
Write-Host "  Docker Tag: $($tag)"
Write-Host "  輸出 .tar 檔案: $($outputFile)"

### 執行 Docker 建置命令 (即時輸出)

Write-Host "`n🚀 正在建置 Docker 映像檔: $($tag) ..." -ForegroundColor Blue

# 直接執行 Docker build，讓輸出即時串流到終端機
docker build -t $tag .
$buildResult = $LASTEXITCODE # 在命令執行後立即檢查 $LASTEXITCODE

if ($buildResult -ne 0) {
    Write-Host "❌ Docker 映像檔建置失敗！請檢查上述輸出訊息。" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "`n✅ Docker 映像檔建置成功！" -ForegroundColor Green
}


### 執行 Docker 儲存命令 (即時輸出)

Write-Host "`n📦 正在儲存 Docker 映像檔至：$($outputFile) ..." -ForegroundColor Blue

# 直接執行 Docker save，讓輸出即時串流到終端機
docker save -o $outputFile $tag
$saveResult = $LASTEXITCODE # 在命令執行後立即檢查 $LASTEXITCODE

if ($saveResult -ne 0) {
    Write-Host "❌ Docker 映像檔儲存失敗！請檢查上述輸出訊息。" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "`n✅ Docker 映像檔儲存成功！" -ForegroundColor Green
}

Write-Host "`n✨ Docker 自動化流程完成！`n" -ForegroundColor Cyan