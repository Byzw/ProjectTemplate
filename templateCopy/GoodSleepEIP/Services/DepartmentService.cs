using Dapper;

namespace GoodSleepEIP
{
    public class DepartmentService
    {
        private IDapperHelper dapper;

        public DepartmentService(IDapperHelper _dapper)
        {
            dapper = _dapper;
        }

        //判斷是否為該部門主管（正、副主管們）
        public bool IsDepartmentManager(string DepartmentName, int UserId)
        {
            try
            {
                string sqlstr = @$"SELECT 1 
                                    FROM {DBName.Main}.DepartmentManagers dm
                                    JOIN {DBName.Main}.Departments d ON dm.DepartmentId = d.DepartmentId
                                    WHERE d.DepartmentName = @DepartmentName
                                    AND dm.UserId = @UserId";
                int hasRecord = Convert.ToInt32(dapper.QueryScalar(sqlstr, new { DepartmentName, UserId }) ?? 0);

                if (hasRecord == 1) return true;
                return false;
            }
            catch { return false; }
        }

        //判斷是否為該部門或上級主管
        public bool IsDepartmentHierarchyManager(string DepartmentName, int UserId)
        {
            try
            {
                string sqlstr = @$"WITH RecursiveDept AS (
                                -- 取得目標部門
                                SELECT DepartmentId, ParentDepartmentId 
                                FROM {DBName.Main}.Departments 
                                WHERE DepartmentName = @DepartmentName
                                UNION ALL
                                -- 遞迴查找上級部門
                                SELECT d.DepartmentId, d.ParentDepartmentId 
                                FROM {DBName.Main}.Departments d
                                INNER JOIN RecursiveDept rd ON d.DepartmentId = rd.ParentDepartmentId
                            )
                            SELECT 1 
                            FROM {DBName.Main}.DepartmentManagers dm 
                            JOIN RecursiveDept r ON dm.DepartmentId = r.DepartmentId
                            WHERE dm.UserId = @UserId ";
                int hasRecord = Convert.ToInt32(dapper.QueryScalar(sqlstr, new { DepartmentName, UserId }) ?? 0);

                if (hasRecord == 1) return true;
                return false;
            } catch { return false; }
        }

        //查詢使用者可管轄的所有部門
        public List<string> GetManagedDepartments(int UserId)
        {
            try
            {
                string sqlstr = @$"WITH RecursiveDept AS (
                                    -- 取得該使用者直接管理的部門
                                    SELECT DepartmentId, ParentDepartmentId, DepartmentName
                                    FROM {DBName.Main}.Departments 
                                    WHERE DepartmentId IN (SELECT DepartmentId FROM {DBName.Main}.DepartmentManagers WHERE UserId = @UserId)
                                    UNION ALL
                                    -- 遞迴查找下級部門
                                    SELECT d.DepartmentId, d.ParentDepartmentId, d.DepartmentName
                                    FROM {DBName.Main}.Departments d
                                    INNER JOIN RecursiveDept rd ON d.ParentDepartmentId = rd.DepartmentId
                                )
                                SELECT DepartmentId, DepartmentName FROM RecursiveDept ";

                var Record_list = (List<Object>)dapper.Query<Object>(sqlstr, new { UserId });
                List<string> departmentNames = new List<string>();
                foreach (var record in Record_list)
                {
                    var department = (dynamic)record;
                    departmentNames.Add(department.DepartmentName);
                }
                return departmentNames;
                
            }
            catch { return new List<string> (); }
        }
    }
}

