<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 客戶在途單據
// ModuleName: Sale
// Backend:
// Database:
// Author:
// Version: 20250720, 1.0
// Memo:
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️

import SalesDetailInputType01ShowOnly from '@/components/Sale/SalesDetailInputType01ShowOnly.vue';
import { useGridOptions } from '@/composables/useGrid';
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import * as gridFormatter from '@/utils/gridFormatter';
import { useToast } from 'primevue/usetoast';
import { computed, onMounted, ref, watch } from 'vue';

const toast = useToast();

const props = defineProps<{
    BizPartnerId: string | null | undefined;
}>();

// 頁面載入時初始化參數
onMounted(async () => {
    try {
        await fetchCollection();
    } catch (error) {
        toast.add({ severity: 'error', summary: '初始化參數錯誤!', detail: error, life: 5000 });
    }
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ 各類(寫死)參數定義區 ✨✨✨✨✨✨✨✨✨✨✨✨

// ✨✨✨✨✨✨✨✨✨✨✨✨ 資料模型定義、初始值、驗證、表單元素值綁定 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ 1. 定義表單 `Composite [Model]`，要用到的資料模型都先組合在這邊，組合方法有很多可問 ChatGPT，比如 extends Pick/Omit
export interface FormCompositeDataInvoice extends models.InvoiceDto {}

// 定義擴展的部門資料模型，加上顯示文字
export interface DepartmentWithDisplayText extends models.ComDepartmentDto {
    DisplayText: string;
}

// ✅ 2. 定義表單的[初始值](比如新增資料)，使用 initialValues 來初始化表單
// 📑元素初始值 (比如新增資料)，使用 initialValues 來初始化表單，
// 📑表單內[有的務必設定]，否則不會依此值初始化，會造成之前資料還在

// ✅ 3. 定義表單[驗證]規則，用 yup

// 人工作廢表單驗證規則

// ✅ 4. 用上述初始值與驗證規則，初始化表單，解構賦值(Destructuring Assignment) handleSubmit, errors...等屬性

// 人工作廢表單

// ✅ 5. <template>元素值綁定，使用 defineField 來綁定表單元素的值
// 📑 defineField() 的第二個返回值是表單元素的事件與屬性，比如: onInput/onChange...value, checked, disabled, readonly...目前用不到，可以自行參閱 VeeValidate 文件

// ✨✨✨✨✨✨✨✨✨✨✨✨ Grid(AG-Grid) 定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ Grid Column 定義 - Master Grid (發貨單列表)
const gridColumnDefsDispatch = ref([
    { headerName: '節目名稱', field: 'SalesPersonName', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '單據編號', field: 'BillNo', sortable: true, filter: 'agTextColumnFilter', sort: 'desc', sortIndex: 0 },
    { headerName: '發貨日期', field: 'ConsignmentDate', sortable: true, filter: 'agTextColumnFilter', valueFormatter: gridFormatter.numericDateFormatter },
    { headerName: '送貨地址', field: 'Address', sortable: true, filter: 'agTextColumnFilter' },
    {
        headerName: '發貨轉檔狀態',
        field: 'SalesTransferStatusDescription',
        width: 150,
        sortable: true,
        filter: 'agSetColumnFilter',
        filterParams: {
            values: (params) => {
                params.success(salesTransferStatusOptions.value.map((item) => item.Description));
            },
            cellRenderer: gridFormatter.salesTransferStatusFormatter
        },
        cellRenderer: gridFormatter.salesTransferStatusFormatter
    },
    { headerName: '建立時間', field: 'CreationTime', sortable: true, filter: 'agTextColumnFilter', valueFormatter: gridFormatter.dateTimeFormatter },
    { headerName: '最後異動時間', field: 'UpdateTime', sortable: true, filter: 'agTextColumnFilter', valueFormatter: gridFormatter.dateTimeFormatter }
]);

const SalesIdTemp = ref();

// ✅ 解構賦值(Destructuring Assignment) 來自 Composables 封裝好的邏輯與定義
// 未完成發貨單grid
const {
    gridApi: gridApiDispatch,
    gridSelectedRows: gridSelectedRowsDispatch,
    gridOptions: gridOptionsDispatch,
    onGridReady: onGridReadyDispatch,
    onSelectionChanged: onSelectionChangedDispatch
} = useGridOptions(apiService.postApiWebSalesPendingOrderList, gridColumnDefsDispatch.value, 'BillNo', toast, true, { BizPartnerId: props.BizPartnerId });

// ✅ 上述 gridOptions 如需調整，在此複寫
//gridOptions.value.rowSelection.mode = 'multiRow'; // 多選
//gridOptions.value.rowSelection.checkboxes = true; // 顯示 checkbox
//gridOptions.value.multiSortKey = ''; // 清空為關閉多重排序，'ctrl'為按下 Ctrl 鍵或 Cmd 鍵才觸發，alwaysMultiSort = true 為不按鍵即可觸發多重排序
gridOptionsDispatch.value.autoSizeStrategy.type = ''; // 不自動調整欄寬

// 雙格線連動設定
const selectedDispatchBillNo = ref<string>(''); // 選中的發貨單號

// 監聽發貨單選擇變化，更新對應的商品清單列表
const onDispatchSelectionChanged = async () => {
    const selectedRows = gridSelectedRowsDispatch.value;

    if (selectedRows && selectedRows.length > 0) {
        selectedDispatchBillNo.value = selectedRows[0].BillNo;
        saleOrderInfo.value = undefined;
        saleOrderInfo.value = (await apiService.callApi(apiService.postApiWebFetchSalesOrderRecord, { SalesId: selectedRows[0].SalesId })) as models.SalesOrderDto;
    } else {
        saleOrderInfo.value = undefined;
    }
};

// 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚 END. Grid(AG-Grid) 定義區 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚

// ✨✨✨✨✨✨✨✨✨✨✨✨ 變數(資料等)區 ✨✨✨✨✨✨✨✨✨✨✨✨
const parameterList = ref<models.Parameter[]>([]);
const saleOrderInfo = ref<models.SalesOrderDto>();

// ✨✨✨✨✨✨✨✨✨✨✨✨ [計算](computed)方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 發貨單轉檔狀態
const salesTransferStatusOptions = computed(() => {
    return parameterList.value.filter((p) => p.Category === 'SalesTransferStatus');
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ [監聽](watch)變數方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 監聽發貨單選擇變化
watch(gridSelectedRowsDispatch, onDispatchSelectionChanged, { deep: true });

// ✨✨✨✨✨✨✨✨✨✨✨✨ [參數取得]區與功能呼叫區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 各類參數一次取得
async function fetchCollection(): Promise<void> {
    try {
        parameterList.value = await apiService.callApi(apiService.postApiWebGetListParameters, ['CustomerTransferStatus', 'SalesTransferStatus']);
    } catch (error) {
        throw error;
    }
}

// ✨✨✨✨✨✨✨✨✨✨✨✨ 各類[對話框]操作相關區 ✨✨✨✨✨✨✨✨✨✨✨✨
</script>

<template>
    <div class="card">
        <div class="space-y-6">
            <!-- 在途訂單列表 - 清爽卡片設計 -->
            <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-md border border-gray-200/60 dark:border-gray-700/60 overflow-hidden">
                <div class="bg-blue-100/90 dark:bg-blue-900/70 px-6 py-4 border-b border-blue-200 dark:border-blue-800">
                    <div class="flex items-center justify-between">
                        <div class="flex items-center gap-3">
                            <div>
                                <h3 class="text-lg font-semibold text-blue-900 dark:text-blue-100">在途訂單列表</h3>
                                <!-- <p class="text-blue-700/90 dark:text-blue-200/80 text-sm">選擇發貨單進行發票開立</p> -->
                            </div>
                        </div>
                    </div>
                </div>
                <div class="p-4">
                    <ag-grid-vue class="ag-theme-quartz" style="width: 100%; height: calc(100vh - 41.6rem)" :gridOptions="gridOptionsDispatch" @gridReady="onGridReadyDispatch" @selection-changed="onSelectionChangedDispatch"> </ag-grid-vue>
                </div>
            </div>

            <!-- 對應商品清單列表 -->
            <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-md border border-gray-200/60 dark:border-gray-700/60 overflow-hidden">
                <!-- 標題列 -->
                <div class="bg-emerald-100/90 dark:bg-emerald-900/70 px-6 py-4 border-b border-emerald-200 dark:border-emerald-800">
                    <div class="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-3">
                        <div class="flex items-center gap-3">
                            <div>
                                <h3 class="text-lg font-semibold text-emerald-900 dark:text-emerald-100">對應商品清單列表</h3>
                            </div>
                            <span v-if="selectedDispatchBillNo" class="px-3 py-1 bg-emerald-200 dark:bg-emerald-800/60 text-emerald-800 dark:text-emerald-200 text-sm font-medium rounded-full border border-emerald-300 dark:border-emerald-700">
                                {{ selectedDispatchBillNo }}
                            </span>
                            <i v-if="isLoading" class="fa-solid fa-spinner fa-spin text-emerald-500 text-sm ml-2"></i>
                        </div>
                    </div>
                </div>

                <!-- 空狀態顯示 -->
                <div v-if="!selectedDispatchBillNo" class="flex flex-col items-center justify-center py-16">
                    <div class="w-16 h-16 bg-slate-100 dark:bg-slate-700 rounded-full flex items-center justify-center mb-4">
                        <i class="fa-solid fa-arrow-up text-slate-400 text-xl"></i>
                    </div>
                    <h4 class="text-slate-700 dark:text-slate-300 text-lg font-semibold mb-2">請先選擇訂單</h4>
                    <p class="text-slate-500 dark:text-slate-400 text-center">選擇上方訂單列表中的記錄<br />即可在此檢視對應的商品清單</p>
                </div>

                <!-- 商品清單內容區域 -->
                <div v-else>
                    <div class="h-[250px] overflow-y-auto">
                        <SalesDetailInputType01ShowOnly :modelValue="saleOrderInfo?.SalesOrderDetails" :toast="toast" />
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>
