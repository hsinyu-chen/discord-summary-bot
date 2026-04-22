# ==========================================
# Stage 1: Build (.NET SDK)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

# 1. 建置專案
RUN dotnet restore "SummaryAndCheck/SummaryAndCheck.csproj"
WORKDIR /src/SummaryAndCheck
RUN dotnet build SummaryAndCheck.csproj -c Release
RUN dotnet publish SummaryAndCheck.csproj -c Release -o /app/publish --no-restore

# ==========================================
# Stage 2: Runtime (Playwright 官方 Image)
# ==========================================
FROM mcr.microsoft.com/playwright/dotnet:v1.58.0-noble AS final

WORKDIR /app

# 1. 安裝 .NET 10 Runtime (使用 root 權限安裝到全域路徑)
USER root
RUN apt-get update && apt-get install -y wget && \
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && \
    chmod +x ./dotnet-install.sh && \
    ./dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet && \
    rm dotnet-install.sh

# 2. 建立使用者 -> 【移除這行】因為 Base Image 已經內建 pwuser 了
# RUN adduser --system --group pwuser

# 3. 複製編譯好的程式
COPY --from=build /app/publish .

# 4. 設定權限 (這一步很重要，因為 COPY 預設是用 root 複製的，我們要轉給 pwuser)
RUN chown -R pwuser:pwuser /app

# 5. 切換到該使用者
USER pwuser

# 6. 啟動
ENTRYPOINT ["dotnet", "SummaryAndCheck.dll"]