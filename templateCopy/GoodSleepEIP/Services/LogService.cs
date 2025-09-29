using System.Text.Json;
using Dapper;
using GoodSleepEIP.Models;

namespace GoodSleepEIP
{
    public interface ILogService
    {
        /// <summary>
        /// 新增日誌
        /// </summary>
        /// <param name="logType">日誌類型</param>
        /// <param name="actionType">操作類型</param>
        /// <param name="moduleName">模組名稱</param>
        /// <param name="logData">日誌資料（將被序列化為 JSON）</param>
        Task CreateLogAsync(string logType, string actionType, string moduleName, string username, object logData);

        /// <summary>
        /// 查詢日誌
        /// </summary>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">結束時間</param>
        /// <param name="logType">日誌類型（可選）</param>
        /// <param name="actionType">操作類型（可選）</param>
        /// <param name="moduleName">模組名稱（可選）</param>
        /// <returns>日誌列表</returns>
        Task<List<Logs>> GetLogsAsync(DateTime startTime, DateTime endTime, string? logType = null, string? actionType = null, string? moduleName = null);

        void CreateLog(string logType, string actionType, string moduleName, string username, object logData);

        List<Logs> GetLogs(DateTime startTime, DateTime endTime, string? logType = null, string? actionType = null, string? moduleName = null);
    }

    public class LogService : ILogService
    {
        private readonly IConfiguration configuration;
        private readonly IDapperHelper dapper;

        public LogService(IConfiguration _config, IDapperHelper _dapper, TokenService _tokenService)
        {
            configuration = _config;
            dapper = _dapper;
        }

        public async Task CreateLogAsync(string logType, string actionType, string moduleName, string username, object logData)
        {
            try
            {
                var log = new Logs
                {
                    Id = Guid.NewGuid(),
                    LogType = logType,
                    Timestamp = DateTime.Now,
                    LogData = JsonSerializer.Serialize(logData),
                    ActionType = actionType,
                    ModuleName = moduleName,
                    Username = username
                };

                var sql = $@"
                    INSERT INTO {DBName.Main}.Logs (Id, LogType, Timestamp, LogData, ActionType, ModuleName, Username)
                    VALUES (@Id, @LogType, @Timestamp, @LogData, @ActionType, @ModuleName, @Username)";

                await dapper.ExecuteAsync(sql, log);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating log: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Logs>> GetLogsAsync(DateTime startTime, DateTime endTime, string? logType = null, string? actionType = null, string? moduleName = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@StartTime", startTime);
                parameters.Add("@EndTime", endTime);

                var whereClause = "Timestamp BETWEEN @StartTime AND @EndTime";

                if (!string.IsNullOrEmpty(logType))
                {
                    whereClause += " AND LogType = @LogType";
                    parameters.Add("@LogType", logType);
                }

                if (!string.IsNullOrEmpty(actionType))
                {
                    whereClause += " AND ActionType = @ActionType";
                    parameters.Add("@ActionType", actionType);
                }

                if (!string.IsNullOrEmpty(moduleName))
                {
                    whereClause += " AND ModuleName = @ModuleName";
                    parameters.Add("@ModuleName", moduleName);
                }

                var sql = $@"
                    SELECT * FROM {DBName.Main}.Logs 
                    WHERE {whereClause}
                    ORDER BY Timestamp DESC";

                var logs = await dapper.QueryAsync<Logs>(sql, parameters);
                return logs.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting logs: {ex.Message}");
                throw;
            }
        }

        public void CreateLog(string logType, string actionType, string moduleName, string username, object logData)
        {
            try
            {
                var log = new Logs
                {
                    Id = Guid.NewGuid(),
                    LogType = logType,
                    Timestamp = DateTime.Now,
                    LogData = JsonSerializer.Serialize(logData),
                    ActionType = actionType,
                    ModuleName = moduleName,
                    Username = username
                };

                var sql = $@"
                    INSERT INTO {DBName.Main}.Logs (Id, LogType, Timestamp, LogData, ActionType, ModuleName, Username)
                    VALUES (@Id, @LogType, @Timestamp, @LogData, @ActionType, @ModuleName, @Username)";

                dapper.Execute(sql, log);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating log: {ex.Message}");
                throw;
            }
        }

        public List<Logs> GetLogs(DateTime startTime, DateTime endTime, string? logType = null, string? actionType = null, string? moduleName = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@StartTime", startTime);
                parameters.Add("@EndTime", endTime);

                var whereClause = "Timestamp BETWEEN @StartTime AND @EndTime";

                if (!string.IsNullOrEmpty(logType))
                {
                    whereClause += " AND LogType = @LogType";
                    parameters.Add("@LogType", logType);
                }

                if (!string.IsNullOrEmpty(actionType))
                {
                    whereClause += " AND ActionType = @ActionType";
                    parameters.Add("@ActionType", actionType);
                }

                if (!string.IsNullOrEmpty(moduleName))
                {
                    whereClause += " AND ModuleName = @ModuleName";
                    parameters.Add("@ModuleName", moduleName);
                }

                var sql = $@"
                    SELECT * FROM {DBName.Main}.Logs 
                    WHERE {whereClause}
                    ORDER BY Timestamp DESC";

                var logs = dapper.Query<Logs>(sql, parameters);
                return logs.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting logs: {ex.Message}");
                throw;
            }
        }
    }
}