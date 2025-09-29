# 使用官方 .NET 8 ASP.NET Core Runtime (支援 arm64)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 複製 arial.ttf 到系統字型目錄
COPY ./Fonts/arial.ttf /usr/share/fonts/truetype/arial.ttf

# 複製 publish 好的所有檔案（包含 wwwroot、Templates、runtimes 等）
COPY ./publish ./

# 啟動服務
ENTRYPOINT ["dotnet", "GoodSleepEIP.dll"]
