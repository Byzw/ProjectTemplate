// ✨✨✨✨✨✨✨✨✨✨✨✨ AgGrid Composables ✨✨✨✨✨✨✨✨✨✨✨✨
// 🚀 這是一個 AgGrid 的 Composables，用來封裝 AgGrid 的相關邏輯與狀態，以快速宣告覆用
import * as gridClient from '@/service/gridClient';
import { inject, ref } from 'vue';

export function useGridOptions(
    apiMethod: (params: any) => Promise<any>,
    gridColumnDefsInput: any[],
    pkColumeName: string,
    toast: any,
    autoLoad = true, // 控制是否自動載入（預設為 true）
    initialExtraParams: object = {} // 可選的 extraParams
) {
    // 以下參數可暴露，讓所有組件都可以使用
    const gridApi = ref<null | any>(null);
    const gridSelectedRows = ref<any[]>([]);
    const isDataLoaded = ref(false); // 紀錄是否已載入資料
    const extraParams = ref<object>(initialExtraParams); // extraParams 設為 ref，可動態變更

    const datasource = {
        getRows: async (params: any) => {
            try {
                await gridClient.getRowsFromApi(apiMethod, params, extraParams.value);
                isDataLoaded.value = true;
            } catch (error) {
                params.fail();
                console.error(error);
                toast.add({ severity: 'error', summary: '載入列表發生錯誤', detail: error, life: 5000 });
            }
        }
    };

    // ✅ 呼叫後端 API 載入資料
    const loadData = (newExtraParams?: object) => {
        if (!gridApi.value || isDataLoaded.value) return;
        if (gridApi.value.isDestroyed && gridApi.value.isDestroyed()) return; // 新增這行，避免 destroyed 時呼叫
        if (newExtraParams) extraParams.value = newExtraParams; // 更新 extraParams
        gridApi.value.setGridOption('serverSideDatasource', datasource);
    };
    // ✅ 呼叫後端 API 重新載入(必須要呼叫過loadData才行)資料
    const reloadData = (newExtraParams?: object) => {
        if (!gridApi.value) return;
        if (!isDataLoaded.value) {
            loadData(newExtraParams ?? extraParams.value); // 如果還沒載入過，先載入
            return;
        }

        if (newExtraParams) extraParams.value = newExtraParams; // 更新 extraParams
        gridApi.value.refreshServerSide({ purge: true });   // 會帶新的 extraParams 到後端
    };
    // ✅ 強制重置載入狀態並重新載入資料
    const forceLoadData = (newExtraParams?: object) => {
        if (!gridApi.value) return;
        isDataLoaded.value = false;
        loadData(newExtraParams ?? extraParams.value);
        return;
    };

    // ✅ Grid 事件處理
    function onGridReady(params: any) {
        gridApi.value = params.api; // 🚀 存到全域，方便後續存取
        if (autoLoad) loadData();
    }

    // 舊版是直接用 getSelectedRows() 取得選取的資料，但這樣只能取得當前頁面的選取狀態，而且 ServerSide 模式會報警
    // 目前畫面能看到的就算全選，所以不用 getServerSideSelectionState
    // 如果需求是 取得「所有伺服器端的選取狀態」，包括未載入的頁面，那 getServerSideSelectionState() 仍然有它的用途
    const onSelectionChanged = () => {
        gridSelectedRows.value = [];
        gridApi.value?.forEachNode((node: any) => {
            if (node.isSelected() && node.data) {
                gridSelectedRows.value.push(node.data);
            }
        });
    };

    // ✅ 欄位定義，允許動態傳入
    const gridColumnDefs = ref(gridColumnDefsInput || []);

    // ✅ 預設欄位設定 `defaultColDef` 允許動態傳入(no)
    const gridDefaultColDef = ref({
        minWidth: 100,
        filter: true,
        floatingFilter: true,
        sortable: true,
        resizable: true,
        tooltipValueGetter: (params: any) => {
            return params.value == null || params.value === '' ? '' : typeof params.value === 'string' ? params.value.replace(/<[^>]+>/g, ' ') : params.value;
        }
    });

    // ✅ AG-Grid 主要設定
    const gridOptions = ref({
        rowSelection: { mode: 'singleRow', checkboxes: false, enableClickSelection: true },
        localeText: inject('agGridLocale'),
        pagination: true,
        paginationPageSize: inject('paginationPageSize'),
        paginationPageSizeSelector: inject('paginationPageSizeSelector'),
        columnDefs: gridColumnDefs.value,
        defaultColDef: gridDefaultColDef.value,
        getRowStyle: (params: any) => { },
        tooltipShowDelay: 500,
        multiSortKey: 'ctrl', // 多重排序，按下 Ctrl 鍵或 Cmd 鍵才觸發，可參考 alwaysMultiSort，留空關掉
        alwaysMultiSort: false,
        rowModelType: 'serverSide',
        getRowId: (params: any) => params.data[pkColumeName]?.toString() ?? '', // 主鍵
        onGridReady: onGridReady,
        onRowClicked: (event: any) => {
            event.node.setSelected(true); // 點擊行時選擇該行
        },
        autoSizeStrategy: {
            type: 'fitGridWidth',
            defaultMinWidth: 100,
            defaultMaxWidth: 400
        },
        alwaysShowVerticalScroll: true, // 始終顯示垂直滾動條，避免影響寬度
        serverSideDatasource: null // 預設不載入，透過 loadData 控制
    });

    return {
        gridApi,
        gridSelectedRows,
        gridOptions,
        gridColumnDefs,
        gridDefaultColDef,
        onGridReady,
        onSelectionChanged,
        loadData,
        reloadData,
        forceLoadData
    };
}
