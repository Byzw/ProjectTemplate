# DapperHelper

DapperHelper 是一個基於 Dapper 的資料庫存取輔助套件，提供更簡便的資料庫操作介面，僅為 Keng-hua Ku 個人專案使用。

## 功能特點

- 簡化 Dapper 的資料庫操作
- 提供常用的資料庫查詢擴充方法
- 支援多資料庫連線
- 提供斷路器和重試機制

## 安裝方式

```bash
dotnet add package DapperHelper
```

## 使用範例

```csharp
// 基本查詢
var result = dapper.Query<YourModel>("SELECT * FROM YourTable");

// 參數化查詢
var result = dapper.Query<YourModel>("SELECT * FROM YourTable WHERE Id = @Id", new { Id = 1 });

// 執行命令
dapper.Execute("INSERT INTO YourTable (Name) VALUES (@Name)", new { Name = "Test" });
```

## 授權

MIT License
