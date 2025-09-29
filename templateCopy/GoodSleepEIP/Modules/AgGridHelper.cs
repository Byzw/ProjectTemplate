//// Copyright © 2024 Keng-hua Ku (kenghua@gmail.com)
using Dapper;
using System; // 增加 System 的 using，因為 Exception 和其他基本類型會用到
using System.Collections.Generic; // 增加 Collections.Generic 的 using
using System.Diagnostics;
using GoodSleepEIP.Models; // 假設您的 Model 在此命名空間
using System.Linq; // 增加 Linq 的 using
using System.Text.Json;
using System.Text.RegularExpressions; // 用於 IsValidFieldName 的正規表示式

namespace GoodSleepEIP.Models
{
    // Request Model ////////////////////////////////////////////////////////////////////////////
    public class AgGridRequest
    {
        public int StartRow { get; set; }
        public int EndRow { get; set; }
        public List<AgGridSortModel>? SortModel { get; set; } // 可為 null
        public Dictionary<string, AgGridFilterModel>? FilterModel { get; set; } // 可為 null
        public List<AgGridColumn>? RowGroupCols { get; set; } // 可為 null
        public List<AgGridColumn>? ValueCols { get; set; } // 可為 null
        public List<AgGridColumn>? PivotCols { get; set; } // 可為 null
        public bool? PivotMode { get; set; } // 可為 null
        public string? OnlyOneRowId { get; set; }
        public object? ExtraParams { get; set; } // 可傳入額外參數
        public string? PrimaryKeyFieldName { get; set; } // 可為 null
        public Dictionary<string, string>? Context { get; set; } // 可為 null
    }

    public class AgGridSortModel
    {
        public required string ColId { get; set; } = string.Empty;  // 欄位名稱
        public string? Sort { get; set; }  // "asc" or "desc"，可為 null
    }

    public class AgGridFilterModel
    {
        // AgGrid 傳來的 filterType，例如 "set", "text", "number", "date"
        // 這個屬性主要用於區分篩選器主類型，特別是 Set Filter
        public string? FilterType { get; set; }

        // 原有的 Type，用於 Text/Number/Date 篩選器的具體比較操作，例如 "contains", "equals"
        public string? Type { get; set; }
        public string? Filter { get; set; } // 單一條件過濾值 (用於 Text/Number/Date 的主要值)
        public string? FilterTo { get; set; } // 用於 "inRange" (數字或日期範圍的第二個值)
        // public DateTime? DateFrom { get; set; } // 如果需要更精確的日期類型處理，可以啟用
        // public DateTime? DateTo { get; set; }   // 如果需要更精確的日期類型處理，可以啟用

        // 用於 Set Filter，存儲選擇的值列表
        public List<string>? Values { get; set; }

        // 用於多重條件的運算子（如 OR、AND），通常針對 Text/Number/Date 篩選器
        public string? Operator { get; set; }
        public List<AgGridFilterModel>? Conditions { get; set; } // 用於多重條件的列表
    }

    public class AgGridColumn
    {
        public string? Id { get; set; } // 欄位名稱，可為 null
        public string? DisplayName { get; set; } // 顯示名稱，可為 null
        public bool? IsPivot { get; set; } // 是否是樞軸欄位，可為 null
    }

    // Response Model ///////////////////////////////////////////////////////////////////////////
    public class AgGridDataTableLazyLoadResult<T>
    {
        public required IEnumerable<T> rows { get; set; }
        public int totalRecords { get; set; } // 您原先可能是 lastRow，通常表示總記錄數

        // 可選的擴充屬性
        public string? status { get; set; } // 用於返回查詢狀態，如 "Success" 或 "Error"，可為 null
        public string? message { get; set; } // 用於詳細描述錯誤或狀態，可為 null
        public DateTime? queryTime { get; set; } // 查詢執行的時間戳，可為 null
        public TimeSpan? executionTime { get; set; } // 查詢執行所耗費的時間，可為 null
        public Dictionary<string, object>? groupTotals { get; set; } // 用於分組總計的元數據，可為 null
        public object? additionalData { get; set; } // 用於返回其他附加的業務數據，可為 null
    }

    // 定義欄位映射類 (保留原有結構)
    public class AgGridFieldChangeMap
    {
        public required string OriginalFieldName { get; set; } = string.Empty;
        public string NewFieldName { get; set; } = string.Empty; // 如果為空，則表示資料庫欄位名與 OriginalFieldName 相同
        public List<AgGridValueChangeMap> AgGridValueChangeMap { get; set; } = []; // 初始化以避免 null
        public List<string>? StripString { get; set; } = []; // 初始化以避免 null
    }

    public class AgGridValueChangeMap
    {
        public required string OriginalValue { get; set; } = string.Empty;
        public required string NewValue { get; set; } = string.Empty;
    }
}

namespace GoodSleepEIP
{
    using GoodSleepEIP.Models; // 確保 using 指向正確的 Models 命名空間

    public static class AgGridHelper
    {
        private static string dbType = "mssql"; // 預設資料庫類型
        private static string paramPrefix = "@"; // 預設參數前綴

        // 操作符字典，對應 SQL 查詢條件
        // 為確保不區分大小寫的匹配 filter.Type，使用 StringComparer.OrdinalIgnoreCase
        private static readonly Dictionary<string, string> agGridOperandsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"equals", "{0} = {1}"},
            {"notEqual", "{0} <> {1}"},
            {"lessThan", "{0} < {1}"},
            {"lessThanOrEqual", "{0} <= {1}"},
            {"greaterThan", "{0} > {1}"},
            {"greaterThanOrEqual", "{0} >= {1}"},
            {"startsWith", "{0} LIKE {1}"},
            {"endsWith", "{0} LIKE {1}"},
            {"contains", "{0} LIKE {1}"},
            {"notContains", "{0} NOT LIKE {1}"},
            // 考慮到不同資料庫欄位類型，TRIM 前可能需要 CAST。這裡以 MSSQL VARCHAR 為例。
            // 您可能需要依據實際欄位類型調整，或在 ApplyFieldChangeMap 中更細緻地處理欄位表達式。
            {"blank", "({0} IS NULL OR LTRIM(RTRIM(CAST({0} AS NVARCHAR(MAX)))) = '')"},
            {"notBlank", "({0} IS NOT NULL AND LTRIM(RTRIM(CAST({0} AS NVARCHAR(MAX)))) <> '')"}
            // 若有 "inRange" 等其他類型，需在此添加
        };

        // 驗證欄位名稱
        private static bool IsValidFieldName(string fieldName)
        {
            // 驗證欄位名稱是否合理，避免SQL注入
            // 允許英數字元、底線、點（用於可能的物件路徑，但通常 AgGrid 的 colId 不會太複雜）
            // 根據您的需求調整正規表示式，此處較為寬鬆。若 colId 保證單純，可收緊。
            if (string.IsNullOrEmpty(fieldName)) return false;
            return Regex.IsMatch(fieldName, @"^[a-zA-Z0-9_.]+$");
        }

        // 應用欄位映射與轉換 (此方法現在主要由 BuildCondition 和 Set Filter 邏輯間接或直接使用其組件)
        // StringComparison.OrdinalIgnoreCase 用於比較原始欄位名和原始值，使其不區分大小寫
        public static (string finalSqlFieldExpression, string? finalDataValue) ApplyFieldAndValueChangeMap(
            string originalAgGridField,
            string? originalDataValue, // 原始篩選值 (例如來自 TextFilter 的 .Filter 或 SetFilter 的 .Values 陣列中的某個值)
            List<AgGridFieldChangeMap> fieldChangeMapList,
            bool applyStripStringToField = true, // 控制是否將 StripString 應用於欄位表達式
            bool applyValueMapToData = true)     // 控制是否將 AgGridValueChangeMap 應用於資料值
        {
            string effectiveDbFieldName = originalAgGridField; // 開始時，假設資料庫欄位名與 AgGrid 欄位 ID 相同
            string? processedDataValue = originalDataValue;

            var fieldMapConfig = fieldChangeMapList.FirstOrDefault(fcm => fcm.OriginalFieldName.Equals(originalAgGridField, StringComparison.OrdinalIgnoreCase));

            if (fieldMapConfig != null)
            {
                // 1. 欄位名稱映射：如果 NewFieldName 有值，則使用它作為基礎的資料庫欄位名
                if (!string.IsNullOrEmpty(fieldMapConfig.NewFieldName))
                {
                    effectiveDbFieldName = fieldMapConfig.NewFieldName;
                }

                // 2. 資料值映射：如果需要且原始資料值非 null
                if (applyValueMapToData && originalDataValue != null && fieldMapConfig.AgGridValueChangeMap.Any())
                {
                    var valueMap = fieldMapConfig.AgGridValueChangeMap.FirstOrDefault(vcm => vcm.OriginalValue.Equals(originalDataValue, StringComparison.OrdinalIgnoreCase));
                    if (valueMap != null && !string.IsNullOrEmpty(valueMap.NewValue))
                    {
                        processedDataValue = valueMap.NewValue; // 使用映射後的值
                    }
                }
            }

            // 預設情況下，SQL 中的欄位表達式就是資料庫欄位名
            string finalSqlFieldExpression = effectiveDbFieldName;

            // 3. StripString 應用於欄位表達式：如果需要且有定義
            if (applyStripStringToField && fieldMapConfig?.StripString != null && fieldMapConfig.StripString.Any())
            {
                foreach (var stripText in fieldMapConfig.StripString)
                {
                    if (!string.IsNullOrEmpty(stripText))
                    {
                        // 確保 stripText 中的單引號被正確跳脫，以防 SQL 注入或語法錯誤
                        string escapedStripText = stripText.Replace("'", "''");
                        finalSqlFieldExpression = $"LTRIM(RTRIM(REPLACE({finalSqlFieldExpression}, '{escapedStripText}', '')))";
                    }
                }
            }

            return (finalSqlFieldExpression, processedDataValue);
        }


        // 處理過濾條件邏輯
        public static (string whereClause, DynamicParameters parameters) AgGridWhereFilters(Dictionary<string, AgGridFilterModel> filterModel, List<AgGridFieldChangeMap> fieldChangeMap)
        {
            var parameters = new DynamicParameters();
            var allFilterClauses = new List<string>(); // 收集所有欄位的篩選條件
            int paramCount = 0; // 用於生成唯一的參數名

            foreach (var filterEntry in filterModel)
            {
                var agGridColId = filterEntry.Key; // 這是從 AgGrid 來的欄位 ID，例如 "SexDistinction"
                var filterOptions = filterEntry.Value; // 這是該欄位的篩選設定

                if (!IsValidFieldName(agGridColId))
                {
                    throw new ArgumentException($"發現非法欄位! 別亂搞喔! 欄位: {agGridColId}");
                }

                // 根據 FilterType 決定處理邏輯
                if (filterOptions.FilterType?.Equals("set", StringComparison.OrdinalIgnoreCase) == true && filterOptions.Values != null && filterOptions.Values.Any())
                {
                    // --- 處理 Set Filter ---
                    List<string> mappedValuesForInClause = new List<string>();
                    foreach (var rawValueFromSet in filterOptions.Values) // 例如 "1"
                    {
                        // 對 Set Filter 的每個值，應用 ValueChangeMap
                        // 欄位名映射和 StripString 是針對 SQL 中的欄位表達式，而不是針對這些值
                        (_, string? mappedValue) = ApplyFieldAndValueChangeMap(agGridColId, rawValueFromSet, fieldChangeMap, applyStripStringToField: false, applyValueMapToData: true);
                        if (mappedValue != null) // 確保映射後的值不是 null (儘管原始值可能允許)
                        {
                            mappedValuesForInClause.Add(mappedValue);
                        }
                    }

                    if (mappedValuesForInClause.Any())
                    {
                        // 獲取 Set Filter 欄位在 SQL 中的表達式 (已應用欄位名映射和 StripString)
                        (string sqlFieldExpressionForSet, _) = ApplyFieldAndValueChangeMap(agGridColId, null, fieldChangeMap, applyStripStringToField: true, applyValueMapToData: false);

                        var paramPlaceholders = new List<string>();
                        foreach (var valueToQuery in mappedValuesForInClause)
                        {
                            paramCount++;
                            string paramName = $"{paramPrefix}{agGridColId.Replace(".", "_")}_set_{paramCount}";
                            parameters.Add(paramName, valueToQuery);
                            paramPlaceholders.Add(paramName);
                        }
                        allFilterClauses.Add($"{sqlFieldExpressionForSet} IN ({string.Join(", ", paramPlaceholders)})");
                    }
                }
                else if (filterOptions.Operator != null && filterOptions.Conditions != null && filterOptions.Conditions.Any())
                {
                    // --- 處理 Text/Number/Date Filter 的複合條件 (AND/OR) ---
                    var conditionSqlParts = new List<string>();
                    foreach (var condition in filterOptions.Conditions)
                    {
                        paramCount++;
                        // 每個 condition 都應針對 agGridColId (父級欄位)
                        // BuildCondition 內部會呼叫 ApplyFieldAndValueChangeMap 處理該 condition 的欄位和值
                        string conditionSql = BuildCondition(condition, agGridColId, parameters, fieldChangeMap, paramCount);
                        if (!string.IsNullOrWhiteSpace(conditionSql))
                        {
                            conditionSqlParts.Add(conditionSql);
                        }
                    }

                    if (conditionSqlParts.Any())
                    {
                        allFilterClauses.Add($"({string.Join($" {filterOptions.Operator} ", conditionSqlParts)})");
                    }
                }
                else if (filterOptions.Type != null) // Type 有值，通常表示是 Text/Number/Date 的單一條件
                {
                    // --- 處理 Text/Number/Date Filter 的單一條件 ---
                    paramCount++;
                    // BuildCondition 內部會呼叫 ApplyFieldAndValueChangeMap
                    string singleConditionSql = BuildCondition(filterOptions, agGridColId, parameters, fieldChangeMap, paramCount);
                    if (!string.IsNullOrWhiteSpace(singleConditionSql))
                    {
                        allFilterClauses.Add(singleConditionSql);
                    }
                }
                // 可以考慮加入對 filterOptions.FilterType == "date" 或 "number" 的更細緻處理（如果 AgGrid 傳來的模型有差異）
            }

            string finalWhereClause = string.Join(" AND ", allFilterClauses.Where(s => !string.IsNullOrWhiteSpace(s)));
            return (finalWhereClause, parameters);
        }

        // 單條件處理邏輯 (主要用於 Text/Number/Date 篩選)
        // agGridFieldId: AgGrid 的欄位 ID
        private static string BuildCondition(AgGridFilterModel filterRule, string agGridFieldId, DynamicParameters parameters, List<AgGridFieldChangeMap> fieldChangeMap, int paramCountSuffix)
        {
            // 獲取 SQL 欄位表達式 (已映射欄位名並應用 StripString) 和映射後的資料值
            // filterRule.Filter 是 Text/Number/Date 篩選器的主要值
            (string sqlFieldExpression, string? mappedFilterData) = ApplyFieldAndValueChangeMap(agGridFieldId, filterRule.Filter, fieldChangeMap, applyStripStringToField: true, applyValueMapToData: true);

            // 對於 'blank' 和 'notBlank'，它們不依賴 mappedFilterData
            if (filterRule.Type?.Equals("blank", StringComparison.OrdinalIgnoreCase) == true ||
                filterRule.Type?.Equals("notBlank", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (agGridOperandsDict.TryGetValue(filterRule.Type, out var formatString))
                {
                    return string.Format(formatString, sqlFieldExpression); // sqlFieldExpression 已包含 LTRIM 等
                }
                else
                {
                    // 理論上不應該發生，因為 "blank"/"notBlank" 在字典中
                    throw new Exception($"未知的空白/非空白過濾類型: {filterRule.Type} (欄位: {agGridFieldId})");
                }
            }

            // 對於其他類型，需要 mappedFilterData
            if (string.IsNullOrEmpty(mappedFilterData))
            {
                // 如果 Type 不是 blank/notBlank，但 mappedFilterData 為空，則此條件無效
                // 可以選擇記錄警告或忽略此條件
                Console.WriteLine($"警告：欄位 '{agGridFieldId}' (SQL表達式: '{sqlFieldExpression}') 的過濾類型 '{filterRule.Type}' 其過濾值為空，將略過此條件。");
                return ""; // 返回空字串，使此條件被忽略
            }

            string paramValueForQuery;
            switch (filterRule.Type?.ToLowerInvariant()) // 確保比較時不區分大小寫
            {
                case "startswith":
                    paramValueForQuery = mappedFilterData + "%";
                    break;
                case "endswith":
                    paramValueForQuery = "%" + mappedFilterData;
                    break;
                case "contains":
                case "notcontains":
                    paramValueForQuery = "%" + mappedFilterData + "%";
                    break;
                default:
                    paramValueForQuery = mappedFilterData;
                    break;
            }

            if (filterRule.Type != null && agGridOperandsDict.TryGetValue(filterRule.Type, out var operandFormat))
            {
                // 參數名稱應基於 AgGrid 原始欄位 ID 以增加可讀性，並確保唯一性
                string paramName = $"{paramPrefix}{agGridFieldId.Replace(".", "_")}_cond_{paramCountSuffix}";
                parameters.Add(paramName, paramValueForQuery);
                return string.Format(operandFormat, sqlFieldExpression, paramName);
            }
            else
            {
                throw new Exception($"未知的過濾類型: {filterRule.Type} (欄位: {agGridFieldId})");
            }
        }


        public static string AgGridAddOrder(string org_str, List<AgGridSortModel>? sortModel, string defaultOrderFieldName = "")
        {
            // org_str 參數似乎未使用，直接構建 ORDER BY 子句
            string orderByClause = "";
            if (sortModel != null && sortModel.Count > 0)
            {
                // 當 sortModel 存在時，使用 sortModel 排序
                var orderClauses = sortModel
                    .Where(sort => !string.IsNullOrEmpty(sort.ColId) && IsValidFieldName(sort.ColId) && (string.IsNullOrEmpty(sort.Sort) || sort.Sort.Equals("asc", StringComparison.OrdinalIgnoreCase) || sort.Sort.Equals("desc", StringComparison.OrdinalIgnoreCase)))
                    .Select(sort => $"{sort.ColId.Replace("'", "''")} {(string.IsNullOrEmpty(sort.Sort) ? "ASC" : sort.Sort.Replace("'", "''"))}"); // 防範簡單的引號注入並提供預設排序方向

                if (orderClauses.Any())
                {
                    orderByClause = " ORDER BY " + string.Join(", ", orderClauses);
                }
            }

            if (string.IsNullOrEmpty(orderByClause)) // 如果 sortModel 無效或為空
            {
                if (!string.IsNullOrEmpty(defaultOrderFieldName) && IsValidFieldName(defaultOrderFieldName))
                {
                    orderByClause = $" ORDER BY {defaultOrderFieldName.Replace("'", "''")}";
                }
                else
                {
                    // 防止 SQL Server OFFSET 需求未提供 ORDER BY，避免 SQL 錯誤，分頁必須要有 ORDER 否則無法分
                    // 可以考慮拋出更明確的錯誤，或有一個全局預設排序欄位
                    throw new ArgumentException("分頁查詢必須提供至少一個有效的排序列，或者設定一個有效的預設排序列。");
                }
            }
            return orderByClause; // 直接返回 ORDER BY 子句
        }

        // 分頁方法
        public static string AgGridAddPagination(string org_str, int startRow, int endRow, string dbType)
        {
            // org_str 參數似乎未使用，直接構建分頁子句
            string paginationClause = "";
            int pageSize = endRow - startRow;
            if (pageSize <= 0) pageSize = 10; // 提供一個合理的預設頁面大小，防止計算錯誤

            switch (dbType.ToLower())
            {
                case "mssql":
                case "oracle": // Oracle 12c+ 的語法與 SQL Server 類似
                    paginationClause = $" OFFSET {startRow} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                    break;
                case "mysql":
                    paginationClause = $" LIMIT {startRow}, {pageSize}";
                    break;
                // 增加其他資料庫的分頁處理方法
                default:
                    throw new NotSupportedException($"不支援的資料庫類型進行分頁: {dbType}");
            }
            return paginationClause; // 直接返回分頁子句
        }

        // 整合進入呼叫點
        public static AgGridDataTableLazyLoadResult<T> HandleAgGridRequest<T>(
            IDapperHelper dapper, // 假設您有一個 IDapperHelper 介面用於資料庫操作
            AgGridRequest request,
            string sql_select,     // SELECT 子句 (例如 "SELECT col1, col2")
            string sql_from,       // FROM 子句 (例如 "FROM YourTable")
            string default_where,  // 預設的 WHERE 條件 (例如 "IsActive = 1")，若無則傳入 "1=1" 或空字串
            List<AgGridFieldChangeMap> fieldChangeMap,
            string defaultOrderFieldName, // 當 request.SortModel 為空時使用的預設排序列
            string sql_select_count = "SELECT COUNT(*)", // 計算總數的 SELECT，可被覆寫
            string _dbType = "")   // 可選，若不提供則使用靜態變數 dbType
        {
            try
            {
                if (!string.IsNullOrEmpty(_dbType)) dbType = _dbType.ToLowerInvariant();
                paramPrefix = (dbType == "oracle" ? ":" : "@");

                Stopwatch stopwatch = Stopwatch.StartNew();
                string effective_default_where = string.IsNullOrWhiteSpace(default_where) ? "1=1" : default_where;

                // OnlyOneRowId 為特殊需求: 僅給予 rowId，並只取這筆資料
                if (!string.IsNullOrEmpty(request.OnlyOneRowId) && !string.IsNullOrEmpty(request.PrimaryKeyFieldName) && IsValidFieldName(request.PrimaryKeyFieldName))
                {
                    // 對 PrimaryKeyFieldName 應用 ApplyFieldChangeMap 進行映射
                    (string mappedPkFieldExpression, string? mappedPkValue) = ApplyFieldAndValueChangeMap(
                        request.PrimaryKeyFieldName,
                        request.OnlyOneRowId,
                        fieldChangeMap,
                        applyStripStringToField: false, // 主鍵欄位通常不需要 StripString
                        applyValueMapToData: true       // 主鍵值可能需要映射
                    );

                    string onlyOneRowSql = $"{sql_select} {sql_from} WHERE {effective_default_where} AND {mappedPkFieldExpression} = {paramPrefix}PKEYID";
                    var onlyOneRowParams = new DynamicParameters();
                    onlyOneRowParams.Add($"{paramPrefix}PKEYID", mappedPkValue ?? request.OnlyOneRowId);

                    var onlyOneRow = dapper.Query<T>(onlyOneRowSql, onlyOneRowParams);
                    stopwatch.Stop();   // 停止 Stopwatch 並計算執行時間
                    return new AgGridDataTableLazyLoadResult<T>
                    {
                        rows = onlyOneRow,
                        totalRecords = onlyOneRow.Count(), // 如果只取一筆，總數就是1或0
                        status = "Success (OnlyOneRowId)",
                        queryTime = DateTime.UtcNow,
                        executionTime = stopwatch.Elapsed
                    };
                }

                // 處理 SQL 的過濾條件
                var (generatedFilters, filterParameters) = AgGridWhereFilters(request.FilterModel ?? new Dictionary<string, AgGridFilterModel>(), fieldChangeMap);

                string finalWhereClause = effective_default_where;
                if (!string.IsNullOrWhiteSpace(generatedFilters))
                {
                    finalWhereClause = $"{effective_default_where} AND {generatedFilters}";
                }

                // 排序條件 (AgGridAddOrder 現在直接返回 " ORDER BY ..." 或空字串)
                string orderClause = AgGridAddOrder("", request.SortModel, defaultOrderFieldName); // 第一個參數 "" 不再使用

                // 分頁條件 (AgGridAddPagination 現在直接返回 OFFSET...LIMIT... 或空字串)
                string paginationClause = AgGridAddPagination("", request.StartRow, request.EndRow, dbType); // 第一個參數 "" 不再使用

                // 最終的 SQL 語句
                string finalSql = $"{sql_select} {sql_from} WHERE {finalWhereClause}{orderClause}{paginationClause}";
                string finalCountSql = $"{sql_select_count} {sql_from} WHERE {finalWhereClause}";

                // 執行查詢並獲取結果
                var rows = dapper.Query<T>(finalSql, filterParameters);
                int totalRecords = Convert.ToInt32(dapper.QueryScalar(finalCountSql, filterParameters));

                stopwatch.Stop();
                return new AgGridDataTableLazyLoadResult<T>
                {
                    rows = rows,
                    totalRecords = totalRecords,
                    status = "Success",
                    queryTime = DateTime.UtcNow,
                    executionTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                // 實際應用中，應使用更完善的日誌記錄機制
                Console.Error.WriteLine($"[AgGridHelper Error] {ex.ToString()}");
                // 向上拋出，或返回包含錯誤訊息的 AgGridDataTableLazyLoadResult
                throw new Exception($"AgGridHelper 處理請求時發生錯誤: {ex.Message}", ex);
            }
        }

        // 解析 AgGridRequest 的 ExtraParams，為了可以動態判別要查詢的資料
        public static bool TryParseExtraParams<T>(object? extraParams, out T? parsedObject) where T : class
        {
            parsedObject = null;

            if (extraParams is not JsonElement jsonElement || jsonElement.ValueKind != JsonValueKind.Object)
                return false;

            try
            {
                // 設定 JsonSerializerOptions 以更好地處理大小寫不敏感等情況 (可選)
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                parsedObject = jsonElement.Deserialize<T>(options);
                return parsedObject != null;
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[安全性警告] ExtraParams 解析失敗: {ex.Message}");
                return false;
            }
        }
    }
}