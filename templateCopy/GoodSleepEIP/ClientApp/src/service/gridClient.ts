import type { AgGridRequest } from '@/service/apiServices.schemas';

// AG-Grid 專屬的 handleResponse
const handleResponse = (response: any, params?: any, successCallback?: (rows: any[], totalRecords: number) => void) => {
    const data = response.data as unknown;
    try {
        if (typeof data === 'object' && data !== null && 'rows' in data) {
            const rows = (data as { rows: any[] }).rows;
            if (Array.isArray(rows) && rows.length > 0) {
                if (successCallback) {
                    params?.success({ rowData: rows, rowCount: response.data.totalRecords });
                }
                return rows.length === 1 ? rows[0] : rows;
            } else {
                params?.success({ rowData: [], rowCount: 0 });
                return [];
            }
        } else {
            params?.fail();
            throw new Error('資料格式不正確，缺少 rows 屬性');
        }
    } catch (error: any) {
        params?.fail();
        throw error;
    }
};

// Grid 資料載入
export const getRowsFromApi = async (
    apiMethod: (request: AgGridRequest) => Promise<any>,
    params?: any,
    extraParams?: object // 可選的 extraParams
) => {
    if (!params) {
        throw new Error('getRowsFromApi 需要 `params`');
    }

    try {
        // 如果 extraParams 存在，就加入，否則不傳
        const request: AgGridRequest = {
            ...params.request,
            ...(extraParams ? { ExtraParams: extraParams } : {}) // 動態加入 ExtraParams
        };

        const response = await apiMethod(request);
        handleResponse(response, params, (rows, totalRecords) => {
            params.success({ rowData: rows, rowCount: totalRecords });
        });
    } catch (error) {
        params.fail();
        throw error;
    }
};


// 單筆查詢 - 透過 primaryKeyFieldName(主鍵欄位名)、onlyOneRowId(字串為了Guid或數字) 取得特定 ID 的資料
// 主要是為了: 更新完資料後，將此筆取出做 grid 單一筆的更新，而不用整頁重載
// 這樣後端就不用寫單獨的 Grid單筆查詢 方法
export const getOneRowFromApi = async (
    apiMethod: (request: AgGridRequest) => Promise<any>,
    onlyOneRowId: string,
    primaryKeyFieldName: string,
    extraParams?: object // 可選的 extraParams
) => {
    if (!onlyOneRowId || !primaryKeyFieldName) {
        throw new Error('getOneRowFromApi 需要 `onlyOneRowId` 和 `primaryKeyFieldName`');
    }

    try {
        // 如果 extraParams 存在，就加入
        const request: AgGridRequest = {
            StartRow: 0,
            EndRow: 0,
            PrimaryKeyFieldName: primaryKeyFieldName,
            OnlyOneRowId: onlyOneRowId,
            ...(extraParams ? { ExtraParams: extraParams } : {}) // 動態加入 ExtraParams
        };

        const response = await apiMethod(request);
        return handleResponse(response);
    } catch (error) {
        throw error;
    }
};

