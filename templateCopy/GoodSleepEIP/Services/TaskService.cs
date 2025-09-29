using Dapper;
using GoodSleepEIP.Models;
using System.Collections.Concurrent;

namespace GoodSleepEIP
{
    public interface ITaskProcessor
    {
        string TaskType { get; }
        Task<object?> ProcessAsync(Guid taskId, object parameters, CancellationToken token);
    }

    public class TaskService : BackgroundService
    {
        private readonly ILogger<TaskService> logger;
        private readonly IDapperHelper dapper;

        // 任務佇列
        private static readonly ConcurrentQueue<(Guid TaskId, string TaskType, object Parameters, TaskCompletionSource<object?> TaskSource)> taskQueue = new();

        // 任務類型對應表
        private readonly Dictionary<string, ITaskProcessor> taskProcessors;

        // 已完成的任務結果（避免查詢時找不到已完成的任務）
        private static readonly ConcurrentDictionary<Guid, object?> completedTasks = new();

        public TaskService(ILogger<TaskService> _logger, IDapperHelper _dapper, IEnumerable<ITaskProcessor> processors)
        {
            logger = _logger;
            dapper = _dapper;

            // 建立任務類型對應表
            taskProcessors = processors.ToDictionary(p => p.TaskType, p => p);
        }

        /// <summary>
        /// 佇列新的任務，回傳 TaskId
        /// </summary>
        public Guid QueueTask(string taskType, object? parameters = null)
        {
            if (!taskProcessors.ContainsKey(taskType))
                throw new ArgumentException($"無效的任務類型: {taskType}");

            var taskId = Guid.NewGuid();
            var taskSource = new TaskCompletionSource<object?>();

            taskQueue.Enqueue((taskId, taskType, parameters ?? new object(), taskSource));
            logger.LogInformation($"[TaskService] 新增任務請求: {taskId}, 類型: {taskType}");

            return taskId;
        }

        /// <summary>
        /// 讓前端查詢任務結果（可能是報表結果、下載連結等）
        /// </summary>
        public async Task<object?> GetTaskResult(Guid taskId)
        {
            // 先從已完成的任務中查找
            if (completedTasks.TryGetValue(taskId, out var result))
                return result;

            // 再從佇列中找
            var task = taskQueue.FirstOrDefault(t => t.TaskId == taskId).TaskSource;
            return task?.Task.IsCompletedSuccessfully == true ? await task.Task : null;
        }

        /// <summary>
        /// 背景執行任務
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (taskQueue.TryDequeue(out var task))
                {
                    try
                    {
                        logger.LogInformation($"[TaskService] 開始執行任務: {task.TaskId}, 類型: {task.TaskType}");

                        if (taskProcessors.TryGetValue(task.TaskType, out var processor))
                        {
                            // 執行任務（補上 `task.Parameters`）
                            object? result = await processor.ProcessAsync(task.TaskId, task.Parameters, stoppingToken);

                            // 儲存結果，避免查詢時找不到
                            completedTasks[task.TaskId] = result;

                            // 設定成功回傳
                            task.TaskSource.TrySetResult(result);
                            logger.LogInformation($"[TaskService] 任務完成: {task.TaskId}");
                        }
                        else
                        {
                            task.TaskSource.TrySetResult("任務類型未實作");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "[TaskService] 任務執行失敗");
                        task.TaskSource.TrySetResult($"任務執行失敗: {ex.Message}");
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
