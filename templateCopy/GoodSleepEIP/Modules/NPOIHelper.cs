using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Text;
using HorizontalAlignment = NPOI.SS.UserModel.HorizontalAlignment;

namespace GoodSleepEIP
{
    public class ExcelCellData
    {
        public required string SheetName { get; set; }
        public required string CellReference { get; set; }
        public required string CellValue { get; set; }
    }

    public class NPOIHelper
    {
        #region 屬性

        private readonly int _perSheetCount = 50000; //每個sheet要保存的條數

        public NPOIHelper()
        {
        }

        /// <summary>
        ///     最大接收5萬條每頁，大於5萬時，使用系統預設的值(4萬)
        /// </summary>
        /// <param name="perSheetCounts"></param>
        public NPOIHelper(int perSheetCounts)
        {
            _perSheetCount = perSheetCounts;
        }

        #endregion

        #region IExcelProvider 成員

        public DataTable? Import(Stream fs, string ext, out string msg, List<string>? validates = null)
        {
            msg = string.Empty;
            var dt = new DataTable();
            try
            {
                IWorkbook workbook;
                if (ext == ".xls")
                    workbook = new HSSFWorkbook(fs);
                else if (ext == ".xlsx")
                    workbook = new XSSFWorkbook(fs);
                else
                {
                    msg = "不支援的檔案格式";
                    return null;
                }

                const int num = 0;
                var sheet = workbook.GetSheetAt(num);
                if (sheet == null) return null;

                dt.TableName = sheet.SheetName;
                var rowCount = sheet.LastRowNum;
                const int firstNum = 0;
                var headerRow = sheet.GetRow(0);
                if (headerRow == null) return null;

                int cellCount = headerRow.LastCellNum;
                if (validates != null)
                {
                    var validateCount = validates.Count;
                    if (validateCount > cellCount)
                    {
                        msg = "上傳EXCEL文件格式不正確";
                        return null;
                    }
                    for (var i = 0; i < validateCount; i++)
                    {
                        var cell = headerRow.GetCell(i);
                        if (cell == null) continue;
                        var columnName = cell.StringCellValue;
                        if (validates[i] == columnName) continue;
                        msg = "上傳EXCEL文件格式不正確";
                        return null;
                    }
                }
                for (int i = headerRow.FirstCellNum; i < cellCount; i++)
                {
                    var cell = headerRow.GetCell(i);
                    if (cell == null) continue;
                    var column = new DataColumn(cell.StringCellValue);
                    dt.Columns.Add(column);
                }
                for (var i = firstNum + 1; i <= rowCount; i++)
                {
                    var row = sheet.GetRow(i);
                    var dataRow = dt.NewRow();
                    if (row != null)
                        for (int j = row.FirstCellNum; j < cellCount; j++)
                            if (row.GetCell(j) != null)
                                dataRow[j] = GetCellValue(row.GetCell(j), ext);
                    dt.Rows.Add(dataRow);
                }
                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static IFont GetFont(IWorkbook workbook, HSSFColor color)
        {
            var font = workbook.CreateFont();
            font.Color = color.Indexed;
            font.FontHeightInPoints = 10;
            //font.FontName = "標楷體";
            //font.IsItalic = true; // 斜體
            return font;
        }

        private static IFont GetHeadFont(IWorkbook workbook, HSSFColor color)
        {
            var font = workbook.CreateFont();
            font.Color = color.Indexed;
            //font.FontHeightInPoints = 12;
            font.FontHeightInPoints = 10;
            font.IsBold = true;
            return font;
        }

        public static void SetCellValues(ICell cell, string cellType, string cellValue)
        {
            if (cellValue == null)
            {
                cell.SetCellValue("");
                return;
            }

            switch (cellType)
            {
                case "System.String": //字串類型
                    // 判斷是否為百分比格式，百分比在C#中必須為string，可是在excel中必須為『百分比格式』，否則會報警
                    if (!string.IsNullOrEmpty(cellValue) && cellValue.EndsWith("%"))
                    {
                        var percentStr = cellValue.TrimEnd('%');
                        if (double.TryParse(percentStr, out double percentValue))
                        {
                            cell.SetCellValue(percentValue / 100.0);

                            // 設定百分比格式
                            var workbook = cell.Sheet.Workbook;
                            var originalStyle = cell.CellStyle ?? workbook.CreateCellStyle();
                            var percentStyle = workbook.CreateCellStyle();
                            percentStyle.CloneStyleFrom(originalStyle);
                            percentStyle.DataFormat = workbook.CreateDataFormat().GetFormat("0%");
                            cell.CellStyle = percentStyle;
                            break;
                        }
                    }
                    cell.SetCellValue(cellValue);
                    break;
                case "System.DateTime": //日期類型
                    DateTime dateV;
                    if (DateTime.TryParse(cellValue, out dateV))
                        cell.SetCellValue(dateV);
                    else
                        cell.SetCellValue("");
                    break;
                case "System.Boolean": //布林型
                    bool boolV;
                    if (bool.TryParse(cellValue, out boolV))
                        cell.SetCellValue(boolV);
                    else
                        cell.SetCellValue("");
                    break;
                case "System.Int16": //整數型
                case "System.Int32":
                case "System.Int64":
                case "System.Byte":
                    int intV;
                    if (int.TryParse(cellValue, out intV))
                        cell.SetCellValue(intV);
                    else
                        cell.SetCellValue("");
                    break;
                case "System.Decimal": //浮點型
                case "System.Double":
                    double doubV;
                    if (double.TryParse(cellValue, out doubV))
                        cell.SetCellValue(doubV);
                    else
                        cell.SetCellValue("");
                    break;
                case "System.DBNull": //空值處理
                    cell.SetCellValue("");
                    break;
                default:
                    cell.SetCellValue("");
                    break;
            }
        }

        public byte[] ExportDownload(List<DataTable> dtInList, string ext, bool isWarp = true)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Export(dtInList, ext, isWarp).Write(ms);
                return ms.ToArray();
            }
        }

        public string ExportFile(string excelFileName, List<DataTable> dtInList, string ext, bool isWarp = true)
        {
            var fs = new FileStream(excelFileName, FileMode.CreateNew, FileAccess.Write);
            Export(dtInList, ext, isWarp).Write(fs);
            fs.Close();
            return excelFileName;
        }

        // 套表用，以後讀Excel進來、寫入資料到某格
        public void FillDataToExcel(string filePath, List<Dictionary<string, string>> input)
        {
            try
            {
                // check file exist
                if (!File.Exists(filePath)) throw new Exception("指定的檔案不存在");

                // create file stream
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // 建立工作表
                    //var workbook = new XSSFWorkbook(fileStream);
                    var workbook = new HSSFWorkbook(fileStream);

                    // foreach 資料列表
                    foreach (var data in input)
                    {
                        // get 列與行訊息
                        string cellRef = data.Keys.First();
                        string cellValue = data.Values.First();

                        // 解析列和行信息，如"A1" -> 列索引為0，行索引為0
                        string columnLetter = new string(cellRef.TakeWhile(char.IsLetter).ToArray());
                        int columnIndex = CellReference.ConvertColStringToIndex(columnLetter);
                        int rowIndex = int.Parse(cellRef.Substring(columnLetter.Length)) - 1;

                        // 獲取要填入數據的工作表和單元格
                        ISheet sheet = workbook.GetSheetAt(0);
                        IRow row = sheet.GetRow(rowIndex);
                        ICell cell = row.GetCell(columnIndex, MissingCellPolicy.CREATE_NULL_AS_BLANK);

                        // 設置單元格的值
                        cell.SetCellValue(cellValue);
                    }

                    // 保存修改後的Excel文件
                    workbook.Write(fileStream);
                }
            }
            catch (IOException ex)
            {
                throw new Exception("無法存取文件: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("錯誤: " + ex.Message);
            }
        }

        private IWorkbook Export(List<DataTable> dtInList, string ext, bool isWarp = true)
        {
            IWorkbook workbook;
            if (ext == ".xls")
                workbook = new HSSFWorkbook();
            else if (ext == ".xlsx")
                workbook = new XSSFWorkbook();
            else
                throw new ArgumentException("Unsupported file extension: " + ext);

            //var workbook = new HSSFWorkbook();
            ICell cell;

            foreach (DataTable dtIn in dtInList)
            {
                var sheetCount = 1; //目前的sheet數量
                var currentSheetCount = 0; //迴圈時目前保存的條數，每頁都會清零

                //表頭樣式
                var head_style = workbook.CreateCellStyle();
                head_style.Alignment = HorizontalAlignment.Center;          // 水平置中
                head_style.VerticalAlignment = VerticalAlignment.Center;    // 垂直置中
                head_style.WrapText = isWarp;                                 // 允許自動換行
                head_style.SetFont(GetHeadFont(workbook, new HSSFColor.Blue()));

                //內容樣式
                var style = workbook.CreateCellStyle();
                style.Alignment = HorizontalAlignment.Left;
                style.SetFont(GetFont(workbook, new HSSFColor.Black()));
                style.WrapText = isWarp;

                string sheet_base_name = dtIn.TableName;    //string sheet_base_name = "Sheet";
                                                            //var sheet = workbook.CreateSheet(sheet_base_name + sheetCount);
                var sheet = workbook.CreateSheet(sheet_base_name);

                //填充表頭
                var row = sheet.CreateRow(0);
                for (var i = 0; i < dtIn.Columns.Count; i++)
                {
                    cell = row.CreateCell(i);
                    cell.SetCellValue(dtIn.Columns[i].ColumnName);
                    cell.CellStyle = head_style;
                }
                //填充內容
                for (var i = 0; i < dtIn.Rows.Count; i++)
                {
                    if (currentSheetCount >= _perSheetCount)
                    {
                        sheetCount++;
                        currentSheetCount = 0;
                        sheet = workbook.CreateSheet(sheet_base_name + sheetCount);
                    }
                    row = sheetCount == 1 ? sheet.CreateRow(currentSheetCount + 1) : sheet.CreateRow(currentSheetCount);
                    currentSheetCount++;
                    for (var j = 0; j < dtIn.Columns.Count; j++)
                    {
                        cell = row.CreateCell(j);
                        cell.CellStyle = style;
                        string cell_data = dtIn.Rows[i][j]?.ToString() ?? "";
                        SetCellValues(cell, dtIn.Columns[j].DataType.ToString(), cell_data);

                        //sheet.AutoSizeColumn(j);  // 碰到中文會有問題且會大 loading! 以下為中文相容方案
                        int max_setWidth = 255 * 256;   // 最大寬度不可超過 255
                        int cell_width_now = (int)(sheet.GetColumnWidth(j) / 256);
                        int cell_width_data = Encoding.Default.GetBytes(cell_data).Length;
                        int setWidth = cell_width_data * 256;
                        if (cell_width_now < cell_width_data) sheet.SetColumnWidth(j, setWidth > max_setWidth ? max_setWidth : setWidth);
                    }
                }
            }
            return workbook;
        }

        public DataTable? Import(IFormFile file)
        {
            var table = new DataTable();
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var workbook = WorkbookFactory.Create(stream);  // 使用 WorkbookFactory 自動處理 xls 和 xlsx 文件

                    for (var num = 0; num < workbook.NumberOfSheets; num++)
                    {
                        var sheet = workbook.GetSheetAt(num);
                        if (sheet == null) continue;

                        table.TableName = sheet.SheetName;
                        var headerRow = sheet.GetRow(sheet.FirstRowNum);
                        if (headerRow == null) continue;

                        var cellCount = headerRow.LastCellNum;

                        // Adding column headers
                        for (int i = headerRow.FirstCellNum; i < cellCount; i++)
                        {
                            var cell = headerRow.GetCell(i);
                            if (cell != null)
                            {
                                var column = new DataColumn(cell.StringCellValue);
                                table.Columns.Add(column);
                            }
                        }

                        // Adding data rows
                        for (var i = sheet.FirstRowNum + 1; i <= sheet.LastRowNum; i++)
                        {
                            var row = sheet.GetRow(i);
                            if (row == null) continue;

                            var dataRow = table.NewRow();
                            for (int j = row.FirstCellNum; j < cellCount; j++)
                            {
                                var cell = row.GetCell(j);
                                if (cell != null)
                                    dataRow[j] = cell.ToString();
                            }
                            table.Rows.Add(dataRow);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return table;
        }


        public DataTable? Import(string filepath, string key, string sheetName, string endKey)
        {
            var table = new DataTable();
            try
            {
                using (var excelFileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    var file = Path.GetExtension(filepath);
                    if (file != null)
                    {
                        var type = file.Replace(".", "");
                        IWorkbook workbook;
                        if (type == "xls")
                            workbook = new HSSFWorkbook(excelFileStream);
                        else
                            workbook = new XSSFWorkbook(excelFileStream);

                        for (var num = 0; num < workbook.NumberOfSheets; num++)
                        {
                            var sheet = workbook.GetSheetAt(num);
                            if (sheet == null || sheet.SheetName != sheetName)
                                continue;

                            table.TableName = sheet.SheetName;
                            var rowCount = sheet.LastRowNum;
                            IRow? headerRow = null;
                            var cellCount = 0;
                            var firstNum = 0;

                            for (var i = 0; i <= rowCount; i++)
                            {
                                var currentRow = sheet.GetRow(i);
                                if (currentRow == null) continue;

                                var cell = currentRow.GetCell(0);
                                if (cell == null) continue;

                                if (cell.StringCellValue != key) continue;
                                headerRow = currentRow;
                                cellCount = headerRow.LastCellNum;
                                firstNum = i;
                                break;
                            }

                            //列名

                            //handling header.
                            if (headerRow != null)
                            {
                                for (int i = headerRow.FirstCellNum; i < cellCount; i++)
                                {
                                    var cell = headerRow.GetCell(i);
                                    if (cell != null)
                                    {
                                        var column = new DataColumn(cell.StringCellValue);
                                        table.Columns.Add(column);
                                    }
                                }
                            }

                            for (var i = firstNum + 1; i <= rowCount; i++)
                            {
                                var row = sheet.GetRow(i);
                                if (row == null) continue;

                                var dataRow = table.NewRow();
                                var isEnd = false;

                                for (int j = row.FirstCellNum; j < cellCount; j++)
                                {
                                    var cell = row.GetCell(j);
                                    if (cell != null)
                                    {
                                        dataRow[j] = GetCellValue(cell, type);
                                        if (dataRow[j]?.ToString() == endKey)
                                        {
                                            isEnd = true;
                                            break;
                                        }
                                    }
                                }

                                if (isEnd)
                                    break;

                                table.Rows.Add(dataRow);
                            }
                            return table;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return table;
        }

        private static string GetCellValue(ICell cell, string type)
        {
            if (cell == null)
                return string.Empty;
            switch (cell.CellType)
            {
                case CellType.Blank:
                    return string.Empty;
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Error:
                    return cell.ErrorCellValue.ToString();
                case CellType.Numeric:
                    var format = cell.CellStyle.DataFormat;
                    if (format == 14 || format == 31 || format == 57 || format == 58)
                    {
                        var date = cell.DateCellValue;
                        return string.Format("{0:yyy-MM-dd}", date);
                    }
                    return cell.ToString() ?? string.Empty;

                case CellType.String:
                    return cell.StringCellValue ?? string.Empty;

                case CellType.Formula:
                    try
                    {
                        if (type == "xls")
                        {
                            var e = new HSSFFormulaEvaluator(cell.Sheet.Workbook);
                            e.EvaluateInCell(cell);
                            return cell.ToString() ?? string.Empty;
                        }
                        else
                        {
                            var e = new XSSFFormulaEvaluator(cell.Sheet.Workbook);
                            e.EvaluateInCell(cell);
                            return cell.ToString() ?? string.Empty;
                        }
                    }
                    catch
                    {
                        return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                    }
                case CellType.Unknown:
                    return cell.ToString() ?? string.Empty;
                default:
                    return cell.ToString() ?? string.Empty;
            }
        }

        #endregion

        #region 輔助導入

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="datatable"></param>
        /// <returns></returns>
        public IEnumerable<T> ConvertTo<T>(DataTable datatable) where T : new()
        {
            var temp = new List<T>();
            try
            {
                var columnsNames =
                    (from DataColumn dataColumn in datatable.Columns select dataColumn.ColumnName).ToList();
                temp = datatable.AsEnumerable().ToList().ConvertAll(row => GetObject<T>(row, columnsNames));
                return temp;
            }
            catch
            {
                return temp;
            }
        }

        /// <summary>
        ///     根據DataTable生成對象，對象的屬性與列同名
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <param name="columnsName"></param>
        /// <returns></returns>
        public T GetObject<T>(DataRow row, List<string> columnsName) where T : new()
        {
            var obj = new T();
            try
            {
                var properties = typeof(T).GetProperties();
                foreach (var objProperty in properties)
                {
                    var attrs = objProperty.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                    if (!attrs.Any()) continue;
                    var displayName = ((DisplayNameAttribute)attrs.First()).DisplayName;

                    var columnname = columnsName.Find(s => s == displayName);
                    if (string.IsNullOrEmpty(columnname)) continue;

                    var value = row[columnname]?.ToString();
                    if (string.IsNullOrEmpty(value)) continue;

                    if (Nullable.GetUnderlyingType(objProperty.PropertyType) != null)
                    {
                        value = value.Replace("$", "").Replace(",", "");
                        var underlyingType = Nullable.GetUnderlyingType(objProperty.PropertyType);
                        if (underlyingType != null)
                        {
                            objProperty.SetValue(obj, Convert.ChangeType(value, underlyingType), null);
                        }
                    }
                    else
                    {
                        value = value.Replace("%", "");
                        var propertyType = objProperty.PropertyType;
                        if (propertyType != null)
                        {
                            objProperty.SetValue(obj, Convert.ChangeType(value, propertyType), null);
                        }
                    }
                }
                return obj;
            }
            catch
            {
                return obj;
            }
        }

        public static void CopyRow(int startRow, int endRow, int pPosition, ISheet sheet)
        {
            int pStartRow = startRow - 1;
            int pEndRow = endRow - 1;
            int targetRowFrom;
            int targetRowTo;
            int cloumnCount;

            if (pStartRow == -1 || pEndRow == -1)
            {
                return;
            }

            //拷貝合併的單元格
            for (int k = 0; k < sheet.NumMergedRegions; k++)
            {
                var region = sheet.GetMergedRegion(k);
                if (region.FirstRow >= pStartRow && region.LastRow <= pEndRow)
                {
                    targetRowFrom = region.FirstRow - pStartRow + pPosition;
                    targetRowTo = region.LastRow - pStartRow + pPosition;
                    CellRangeAddress newRegion = region.Copy();
                    newRegion.FirstRow = targetRowFrom;
                    newRegion.FirstColumn = region.FirstColumn;
                    newRegion.LastRow = targetRowTo;
                    newRegion.LastColumn = region.LastColumn;
                    sheet.AddMergedRegion(newRegion);
                }
            }

            //設置列寬
            for (int k = pStartRow; k <= pEndRow; k++)
            {
                IRow? sourceRow = sheet.GetRow(k);
                if (sourceRow == null) continue;

                cloumnCount = sourceRow.LastCellNum;
                IRow newRow = sheet.CreateRow(pPosition - pStartRow + k);
                newRow.Height = sourceRow.Height;

                for (int l = 0; l < cloumnCount; l++)
                {
                    ICell? templateCell = sourceRow.GetCell(l);
                    if (templateCell != null)
                    {
                        ICell newCell = newRow.CreateCell(l);
                        CopyCell(templateCell, newCell);
                    }
                }
            }
        }

        private static void CopyCell(ICell srcCell, ICell distCell)
        {
            distCell.CellStyle = srcCell.CellStyle;
            if (srcCell.CellComment != null)
            {
                distCell.CellComment = srcCell.CellComment;
            }

            CellType srcCellType = srcCell.CellType;
            distCell.SetCellType(srcCellType);

            string cellValue = GetCellValue(srcCell, "xlsx");
            SetCellValues(distCell, "System.String", cellValue);
        }

        /// <summary>
        /// 將多個 DataTable 匯出到同一個 sheet，並可指定每個表格的起始列
        /// </summary>
        /// <param name="tables">DataTable 列表</param>
        /// <param name="ext">副檔名（.xls 或 .xlsx）</param>
        /// <param name="startRows">每個表格的起始 row（例如 [0, 20, 40, ...]）</param>
        /// <param name="sheetName">sheet 名稱，預設為 "報表彙總"</param>
        /// <returns>Excel 檔案的 byte[]</returns>
        public byte[] ExportMultipleTablesToOneSheet(
            List<DataTable> tables,
            string ext,
            List<int> startRows,
            string sheetName = "報表彙總"
        )
        {
            IWorkbook workbook = ext == ".xls" ? new HSSFWorkbook() : new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 樣式設定
            var headStyle = workbook.CreateCellStyle();
            headStyle.Alignment = HorizontalAlignment.Center;
            headStyle.VerticalAlignment = VerticalAlignment.Center;
            headStyle.WrapText = true;
            headStyle.SetFont(GetHeadFont(workbook, new HSSFColor.Blue()));

            var style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Left;
            style.SetFont(GetFont(workbook, new HSSFColor.Black()));

            for (int t = 0; t < tables.Count; t++)
            {
                var dt = tables[t];
                int startRow = startRows[t];

                // 表頭
                var row = sheet.CreateRow(startRow);
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    var cell = row.CreateCell(i);
                    cell.SetCellValue(dt.Columns[i].ColumnName);
                    cell.CellStyle = headStyle;
                }

                // 內容
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    row = sheet.CreateRow(startRow + 1 + i);
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        var cell = row.CreateCell(j);
                        cell.CellStyle = style;
                        string cellData = dt.Rows[i][j]?.ToString() ?? "";
                        SetCellValues(cell, dt.Columns[j].DataType.ToString(), cellData);
                    }
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 將多個 DataTable 匯出到同一個 sheet，起始列自動計算
        /// </summary>
        /// <param name="tables">DataTable 列表</param>
        /// <param name="ext">副檔名（.xls 或 .xlsx）</param>
        /// <param name="sheetName">sheet 名稱，預設為 "報表彙總"</param>
        /// <param name="spaceBetweenTables">表格間空幾行，預設1</param>
        /// <returns>Excel 檔案的 byte[]</returns>
        public byte[] ExportMultipleTablesToOneSheetAutoRow(
            List<DataTable> tables,
            string ext,
            string sheetName = "報表彙總",
            int spaceBetweenTables = 1
        )
        {
            IWorkbook workbook = ext == ".xls" ? new HSSFWorkbook() : new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 樣式設定
            var headStyle = workbook.CreateCellStyle();
            headStyle.Alignment = HorizontalAlignment.Center;
            headStyle.VerticalAlignment = VerticalAlignment.Center;
            headStyle.WrapText = true;
            headStyle.SetFont(GetHeadFont(workbook, new HSSFColor.Blue()));

            var style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Left;
            style.SetFont(GetFont(workbook, new HSSFColor.Black()));

            int currentRow = 0;
            for (int t = 0; t < tables.Count; t++)
            {
                var dt = tables[t];

                // 表頭
                var row = sheet.CreateRow(currentRow);
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    var cell = row.CreateCell(i);
                    cell.SetCellValue(dt.Columns[i].ColumnName);
                    cell.CellStyle = headStyle;
                    sheet.SetColumnWidth(i, 8 * 256); // 可依需求調整
                }

                // 內容
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    row = sheet.CreateRow(currentRow + 1 + i);
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        var cell = row.CreateCell(j);
                        cell.CellStyle = style;
                        string cellData = dt.Rows[i][j]?.ToString() ?? "";
                        SetCellValues(cell, dt.Columns[j].DataType.ToString(), cellData);
                    }
                }

                // 計算下一個表格的起始 row
                currentRow += dt.Rows.Count + 1 + spaceBetweenTables;
            }

            // 設定列印縮放
            sheet.PrintSetup.FitWidth = 1;
            sheet.PrintSetup.FitHeight = 0;
            sheet.FitToPage = true;

            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }

        public byte[] ExportTablesMatrixToOneSheetWithTitles(
            List<DataTable> tables,
            List<string> titles,
            string ext,
            string sheetName = "報表彙總",
            int tablesPerRow = 3, // 每列幾個表格
            int spaceBetweenTables = 2, // 表格間空幾欄
            int spaceBetweenRows = 2 // 表格行間空幾行
        )
        {
            IWorkbook workbook = ext == ".xls" ? new HSSFWorkbook() : new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(sheetName);

            // 標題樣式
            var titleStyle = workbook.CreateCellStyle();
            titleStyle.Alignment = HorizontalAlignment.Center;
            titleStyle.VerticalAlignment = VerticalAlignment.Center;
            var titleFont = workbook.CreateFont();
            titleFont.IsBold = true;
            titleFont.FontHeightInPoints = 12;
            titleStyle.SetFont(titleFont);

            // 表頭樣式
            var headStyle = workbook.CreateCellStyle();
            headStyle.Alignment = HorizontalAlignment.Center;
            headStyle.VerticalAlignment = VerticalAlignment.Center;
            headStyle.WrapText = true;
            headStyle.SetFont(GetHeadFont(workbook, new HSSFColor.Blue()));

            // 內容樣式
            var style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Left;
            style.SetFont(GetFont(workbook, new HSSFColor.Black()));

            int currentRow = 0;
            int maxTableHeightInRow = 0;

            for (int t = 0; t < tables.Count; t += tablesPerRow)
            {
                int currentCol = 0;
                maxTableHeightInRow = 0;

                for (int k = 0; k < tablesPerRow && (t + k) < tables.Count; k++)
                {
                    var dt = tables[t + k];
                    var title = (titles != null && (t + k) < titles.Count) ? titles[t + k] : $"表格{t + k + 1}";

                    // 標題列（合併表格寬度）
                    var titleRow = sheet.GetRow(currentRow) ?? sheet.CreateRow(currentRow);
                    var titleCell = titleRow.CreateCell(currentCol);
                    titleCell.SetCellValue(title);
                    titleCell.CellStyle = titleStyle;
                    if (dt.Columns.Count > 1)
                        sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(currentRow, currentRow, currentCol, currentCol + dt.Columns.Count - 1));

                    // 表頭
                    var row = sheet.GetRow(currentRow + 1) ?? sheet.CreateRow(currentRow + 1);
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        var cell = row.CreateCell(currentCol + i);
                        cell.SetCellValue(dt.Columns[i].ColumnName);
                        cell.CellStyle = headStyle;
                        sheet.SetColumnWidth(currentCol + i, 8 * 256); // 可依需求調整
                    }

                    // 內容
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var dataRow = sheet.GetRow(currentRow + 2 + i) ?? sheet.CreateRow(currentRow + 2 + i);
                        for (int j = 0; j < dt.Columns.Count; j++)
                        {
                            var cell = dataRow.CreateCell(currentCol + j);
                            cell.CellStyle = style;
                            string cellData = dt.Rows[i][j]?.ToString() ?? "";
                            SetCellValues(cell, dt.Columns[j].DataType.ToString(), cellData);
                        }
                    }

                    // === 新增：加上外框 ===
                    var borderStyle = workbook.CreateCellStyle();
                    borderStyle.BorderTop = BorderStyle.Thin;
                    borderStyle.BorderBottom = BorderStyle.Thin;
                    borderStyle.BorderLeft = BorderStyle.Thin;
                    borderStyle.BorderRight = BorderStyle.Thin;

                    int tableStartRow = currentRow;
                    int tableEndRow = currentRow + dt.Rows.Count + 1; // 標題+表頭+內容
                    int tableStartCol = currentCol;
                    int tableEndCol = currentCol + dt.Columns.Count - 1;

                    for (int r = tableStartRow; r <= tableEndRow; r++)
                    {
                        var rowObj = sheet.GetRow(r) ?? sheet.CreateRow(r);
                        for (int c = tableStartCol; c <= tableEndCol; c++)
                        {
                            var cellObj = rowObj.GetCell(c) ?? rowObj.CreateCell(c);
                            // 保留原本 style 的字型、對齊等，僅加上框線
                            var originalStyle = cellObj.CellStyle ?? workbook.CreateCellStyle();
                            var newStyle = workbook.CreateCellStyle();
                            newStyle.CloneStyleFrom(originalStyle);
                            newStyle.BorderTop = BorderStyle.Thin;
                            newStyle.BorderBottom = BorderStyle.Thin;
                            newStyle.BorderLeft = BorderStyle.Thin;
                            newStyle.BorderRight = BorderStyle.Thin;
                            cellObj.CellStyle = newStyle;
                        }
                    }
                    // === 外框結束 ===

                    // 計算這一列的最大表格高度
                    int tableHeight = dt.Rows.Count + 2; // 標題+表頭+內容
                    if (tableHeight > maxTableHeightInRow)
                        maxTableHeightInRow = tableHeight;

                    // 下一個表格的起始欄
                    currentCol += dt.Columns.Count + spaceBetweenTables;
                }
                // 下一行的起始 row
                currentRow += maxTableHeightInRow + spaceBetweenRows;
            }

            // 設定列印縮放
            sheet.PrintSetup.FitWidth = 1;
            sheet.PrintSetup.FitHeight = 0;
            sheet.FitToPage = true;

            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }
        #endregion
    }
}