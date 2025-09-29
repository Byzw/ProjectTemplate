using Quartz;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GoodSleepEIP
{
    public class DataCleanupJob : IJob
    {
        private readonly TaskService _taskService;
        private readonly ILogger<DataCleanupJob> _logger;

        // 排程時間定義在這裡比較好管理，但是實際定義還是在 program.cs 將本 IJob 加入時(外部讀此值)
        // 秒 分 時 日 月(* 不限制) 星期(1 SUN ~ 7 SAT, ? 不限制)、每 10 分鐘執行一次: "0 */10 * * * ?"
        public static string CronSchedule => "0 0 4 * * ?"; // 每天凌晨 4 點執行

        // 透過 DI 取得 TaskService
        public DataCleanupJob(TaskService taskService, ILogger<DataCleanupJob> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("[Quartz] 觸發 DataCleanup 任務");

            // 使用 TaskService 觸發 "DataCleanup" 任務
            _taskService.QueueTask("DataCleanup");

            return Task.CompletedTask;
        }
    }
}
