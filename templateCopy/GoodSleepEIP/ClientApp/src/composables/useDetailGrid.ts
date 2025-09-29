// ✨✨✨✨✨✨✨✨✨✨✨✨ AgGrid Master/Detail Composables ✨✨✨✨✨✨✨✨✨✨✨✨
// 🚀 這是一個 AgGrid Master/Detail 的 Composables，用來封裝 Master/Detail 的相關邏輯與狀態
// 遵循 useGrid.ts 的設計模式，提供簡潔易用且高度模組化的 API
import { ref, inject } from 'vue';
import { AG_GRID_LOCALE_ZH_TW } from '@/locales/ag-grid/zh-TW';

// Row Class 條件介面
export interface RowClassCondition {
    field: string;
    value: any;
    className: string;
    operator?: 'equals' | 'notEquals' | 'contains' | 'in'; // 比較運算子
}

// Detail Grid 設定介面
export interface DetailGridOptions {
    columnDefs: any[];
    apiMethod: (params: any) => Promise<any>;
    masterRowKeyField: string; // Master 行的關鍵欄位，通用設計
    
    // 可選設定
    detailRowHeight?: number;
    detailRowAutoHeight?: boolean; // 自動調整高度
    paginationPageSize?: number;
    
    // 樣式設定
    enableRowClass?: boolean;
    rowClassConditions?: RowClassCondition[]; // 支援多個條件
    
    // API 參數映射（更靈活的參數處理）
    apiParamsMapper?: (masterRowData: any) => any;
    
    // 自訂 Grid 選項覆寫
    customGridOptions?: any;
    
    // 禁用某些預設功能
    disableFloatingFilter?: boolean;
    disableTextSelection?: boolean;
    disableAutoSizeColumn?: boolean;
}

// 檢查 Row Class 條件的函數
function checkRowClassCondition(value: any, condition: RowClassCondition): boolean {
    const { value: conditionValue, operator = 'equals' } = condition;
    
    switch (operator) {
        case 'equals':
            return value === conditionValue;
        case 'notEquals':
            return value !== conditionValue;
        case 'contains':
            return typeof value === 'string' && value.includes(conditionValue);
        case 'in':
            return Array.isArray(conditionValue) && conditionValue.includes(value);
        default:
            return false;
    }
}

export function useDetailGrid(
    masterGridOptions: any,
    config: DetailGridOptions,
    toast: any
) {
    const {
        columnDefs,
        apiMethod,
        masterRowKeyField,
        detailRowHeight = 400,
        detailRowAutoHeight = false,
        paginationPageSize,
        enableRowClass = false,
        rowClassConditions = [],
        apiParamsMapper,
        customGridOptions = {},
        disableFloatingFilter = false,
        disableTextSelection = false,
        disableAutoSizeColumn = false
    } = config;

    // 使用與 useGrid 相同的注入方式
    const defaultPaginationPageSize = paginationPageSize || inject('paginationPageSize', 10);
    const paginationPageSizeSelector = inject('paginationPageSizeSelector', [5, 10, 20, 50]);

    // Detail Grid 基礎選項配置
    const baseDetailGridOptions = {
        columnDefs: columnDefs,
        defaultColDef: {
            minWidth: 100,
            filter: true,
            floatingFilter: !disableFloatingFilter,
            sortable: true,
            resizable: true,
            tooltipValueGetter: (params: any) => {
                return params.value == null || params.value === '' ? '' : 
                       typeof params.value === 'string' ? params.value.replace(/<[^>]+>/g, ' ') : params.value;
            }
        },
        
        // 效能優化設定
        animateRows: false,
        enableCellTextSelection: !disableTextSelection,
        suppressRowClickSelection: true,
        
        // 分頁設定（與 useGrid 一致）
        pagination: true,
        paginationPageSize: defaultPaginationPageSize,
        paginationPageSizeSelector: paginationPageSizeSelector,
        
        // 使用注入的本地化設定
        localeText: inject('agGridLocale', AG_GRID_LOCALE_ZH_TW),
        
        // 多條件 Row Class 支援
        getRowClass: enableRowClass && rowClassConditions.length > 0 ? (params: any) => {
            for (const condition of rowClassConditions) {
                if (checkRowClassCondition(params.data?.[condition.field], condition)) {
                    return condition.className;
                }
            }
            return '';
        } : undefined,
        
        // 自動調整欄寬（可選）
        autoSizeStrategy: disableAutoSizeColumn ? undefined : {
            type: 'fitCellContents'
        }
    };

    // 合併自訂選項
    const detailGridOptions = ref({
        ...baseDetailGridOptions,
        ...customGridOptions
    });

    // 彈性的資料載入函數
    const getDetailRowData = (params: any) => {
        const masterRowData = params.data;
        const keyValue = masterRowData?.[masterRowKeyField];
        
        if (!keyValue) {
            console.error(`${masterRowKeyField} 為空`);
            params.failCallback();
            return;
        }
        
        setTimeout(async () => {
            try {
                console.log(`載入 ${masterRowKeyField}: ${keyValue} 的 Detail 資料`);
                
                // 使用自訂的參數映射器或預設的單一參數
                const apiParams = apiParamsMapper 
                    ? apiParamsMapper(masterRowData)
                    : { [masterRowKeyField]: keyValue };
                
                const detailList = await apiMethod(apiParams);
                const validDetailList = Array.isArray(detailList) ? detailList : [];
                
                console.log(`${masterRowKeyField}: ${keyValue} 共有 ${validDetailList.length} 筆 Detail 資料`);
                params.successCallback(validDetailList);
                
            } catch (error) {
                console.error(`載入 ${masterRowKeyField}: ${keyValue} 的 Detail 資料失敗:`, error);
                toast.add({ 
                    severity: 'error', 
                    summary: '載入 Detail 資料失敗', 
                    detail: `${masterRowKeyField}: ${keyValue} - ${error}`, 
                    life: 5000 
                });
                params.failCallback();
            }
        }, 0);
    };

    // 可自訂的 Master 行判斷邏輯
    const isRowMaster = (dataItem: any) => {
        return !!(dataItem?.[masterRowKeyField]);
    };

    // 自動應用 Master/Detail 設定
    masterGridOptions.masterDetail = true;
    masterGridOptions.detailCellRendererParams = {
        detailGridOptions: detailGridOptions.value,
        detailRowHeight: detailRowAutoHeight ? undefined : detailRowHeight,
        detailRowAutoHeight: detailRowAutoHeight,
        getDetailRowData: getDetailRowData
    };
    masterGridOptions.isRowMaster = isRowMaster;

    // 自動為 Master Grid 的關鍵欄位添加 cellRenderer
    if (masterGridOptions.columnDefs) {
        const keyColumn = masterGridOptions.columnDefs.find((col: any) => col.field === masterRowKeyField);
        if (keyColumn && !keyColumn.cellRenderer) {
            keyColumn.cellRenderer = 'agGroupCellRenderer';
        }
    }

    return {
        detailGridOptions,
        getDetailRowData,
        isRowMaster,
        // 公開內部方法以便進階使用
        checkRowClassCondition
    };
}

// 輔助函數：建立常見的 Row Class 條件
export const DetailGridHelpers = {
    // 狀態相等條件
    createStatusCondition: (field: string, value: any, className: string): RowClassCondition => ({
        field,
        value,
        className,
        operator: 'equals'
    }),
    
    // 多值條件
    createInCondition: (field: string, values: any[], className: string): RowClassCondition => ({
        field,
        value: values,
        className,
        operator: 'in'
    }),
    
    // 包含條件
    createContainsCondition: (field: string, searchText: string, className: string): RowClassCondition => ({
        field,
        value: searchText,
        className,
        operator: 'contains'
    })
}; 