import ExcelJS from 'exceljs';
import { saveAs } from 'file-saver';
import DOMPurify from 'dompurify';

/**
 * 安全匯出 Excel（自動清理 XSS）
 * @param data 資料陣列
 * @param headers 欄位標題陣列
 * @param filename 檔名，預設 'export.xlsx'
 */
export async function exportCompanyExcel(data: any[], headers: string[], filename = 'export.xlsx') {
    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet('Sheet1');

    // 加入標題
    worksheet.addRow(headers);

    // 加入資料，並做 sanitize
    data.forEach(row => {
        worksheet.addRow(headers.map(h => sanitizeCell(row[h])));
    });

    // 自動欄寬
    worksheet.columns.forEach(column => {
        let maxLength = 10;
        if (column && typeof column.eachCell === 'function') {
            column.eachCell({ includeEmpty: true }, cell => {
                const cellValue = cell.value ? cell.value.toString() : '';
                maxLength = Math.max(maxLength, cellValue.length);
            });
        }
        column.width = maxLength + 2;
    });

    const buffer = await workbook.xlsx.writeBuffer();
    saveAs(new Blob([buffer], { type: 'application/octet-stream' }), filename);
}

function sanitizeCell(value: any) {
    if (typeof value !== 'string') return value;
    // 使用 DOMPurify 清理 HTML
    return DOMPurify.sanitize(value, { ALLOWED_TAGS: [], ALLOWED_ATTR: [] });
}

/**
 * 安全匯出多工作表 Excel（自動清理 XSS）
 * @param sheetsData 工作表資料陣列 [{name: '工作表名稱', data: 資料陣列, headers: 欄位標題陣列}]
 * @param filename 檔名，預設 'export.xlsx'
 */
export async function exportCompanyExcelMultiSheet(
    sheetsData: Array<{
        name: string;
        data: any[];
        headers: string[];
    }>,
    filename = 'export.xlsx'
) {
    const workbook = new ExcelJS.Workbook();

    // 處理每個工作表
    sheetsData.forEach(sheetInfo => {
        const { name, data, headers } = sheetInfo;

        // 建立工作表
        const worksheet = workbook.addWorksheet(name);

        // 加入標題
        worksheet.addRow(headers);

        // 加入資料，並做 sanitize
        data.forEach(row => {
            worksheet.addRow(headers.map(h => sanitizeCell(row[h])));
        });

        // 自動欄寬
        worksheet.columns.forEach(column => {
            let maxLength = 10;
            if (column && typeof column.eachCell === 'function') {
                column.eachCell({ includeEmpty: true }, cell => {
                    const cellValue = cell.value ? cell.value.toString() : '';
                    maxLength = Math.max(maxLength, cellValue.length);
                });
            }
            column.width = maxLength + 2;
        });
    });

    const buffer = await workbook.xlsx.writeBuffer();
    saveAs(new Blob([buffer], { type: 'application/octet-stream' }), filename);
} 