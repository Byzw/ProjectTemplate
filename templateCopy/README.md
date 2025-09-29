目錄說明:

1. GoodSleepEIP: .NET WebAPI Server. (後端)
2. GoodSleepEIP\ClientApp: Vue3 Client. (前端)
3. MigrationService: Scheduled Tasks Service. (自動化服務，主要以報表排程為主)
4. DapperHelper: Database Service. (資料庫存取與 ORM 工具套件，支援 .Net 4.8 ~ .NET8)

安裝指引:

1. vscode、vs、node.js(裝官網預先打包好的 msi 即可)記得裝。
2. git clone 程式回來。
3. 到前端主目錄: \ClientApp 下面，打: npm install 安裝套件
4. 打開 vscode: "Vue - Official"，問你 hybrid 混合模式，選預設 Auto。
5. npm run dev (執行測試環境前端伺服器)、npm run build (自動將編譯好的東西放入佈署目錄)。
6. npm run genapi (或 npx orval)，即可利用 orval 套件將後端的 data model、對應呼叫方法做自動產生(此時後端要先執行起來)。
7. npm run build 編譯並輸出正式版前端程式到 \wwwroot
8. 正式 Publish 打包:
   cd GoodSleepEIP
   dotnet publish -c Release -o ./publish

Docker 部署方法:

1. cd GoodSleepEIP
2. dotnet publish -c Release -o ./publish
3. cd ..
4. docker compose up -d --build (或是用 vscode 打開 docker-compose.yml, 直接選擇 Run All Service)

新環境要安裝 Obfuscar 全域工具
dotnet tool install --global Obfuscar.GlobalTool
