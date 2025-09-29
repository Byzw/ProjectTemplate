<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 客戶提示
// ModuleName: CustomerBlacklist
// Backend: CustomerBlacklistController.cs
// Database: CustomerBlacklist, T8:comCustomer,comBusinessPartner
// Author: Neil Lin
// Version: 20250703, 1.0
// Memo:
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️

import { useGridOptions } from '@/composables/useGrid';
import { apiService } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import { useToast } from 'primevue/usetoast';
import { useForm } from 'vee-validate';
import { computed, onMounted, ref } from 'vue';
import * as yup from 'yup';

const toast = useToast();

onMounted(async () => {
    try {
        await fetchCollection();
    } catch (error) {
        toast.add({ severity: 'error', summary: '初始化參數錯誤!', detail: error, life: 5000 });
    }
});

const parameterList = ref<models.Parameter[]>([]);

// ✨✨✨✨✨✨✨✨✨✨✨✨ 各類(寫死)參數定義區 ✨✨✨✨✨✨✨✨✨✨✨✨

// ✨✨✨✨✨✨✨✨✨✨✨✨ 資料模型定義、初始值、驗證、表單元素值綁定 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ 1. 定義表單 `Composite [Model]`，要用到的資料模型都先組合在這邊，組合方法有很多可問 ChatGPT，比如 extends Pick/Omit
export interface FormCompositeData extends models.SalePerformanceDto {}

// ✅ 2. 定義表單的[初始值](比如新增資料)，使用 initialValues 來初始化表單
// 📑元素初始值 (比如新增資料)，使用 initialValues 來初始化表單，
// 📑表單內[有的務必設定]，否則不會依此值初始化，會造成之前資料還在
const initialValues: FormCompositeData = {};

// ✅ 3. 定義表單[驗證]規則，用 yup
const validationSchema = yup.object({
    // Customer: yup
    //     .object({
    //         BizPartnerId: yup.string().required('客戶姓名必填')
    //     })
    //     .required('客戶必填'),
    // BlacklistLevel: yup.string().required('提示等級必填'),
    // BlacklistEffectiveDate: yup.string().required('生效時間必填'),
    // BlacklistRemark: yup.string().max(255, '備註不能超過 255 個字符'),
    // BlacklistDescription: yup.string().max(255, '説明描述不能超過 255 個字符')
});

// ✅ 4. 用上述初始值與驗證規則，初始化表單，解構賦值(Destructuring Assignment) handleSubmit, errors...等屬性
const {
    handleSubmit: handleSubmit,
    errors: errors,
    values: values,
    resetForm: resetForm,
    defineField: defineField
} = useForm<FormCompositeData>({
    validationSchema: validationSchema,
    initialValues: { ...initialValues }
});

// ✅ 5. <template>元素值綁定，使用 defineField 來綁定表單元素的值
// 📑 defineField() 的第二個返回值是表單元素的事件與屬性，比如: onInput/onChange...value, checked, disabled, readonly...目前用不到，可以自行參閱 VeeValidate 文件
// const [BizPartnerId] = defineField('BizPartnerId');
// const [BlacklistLevel] = defineField('BlacklistLevel');
// const [BlacklistEffectiveDate] = defineField('BlacklistEffectiveDate');
// const [BlacklistExpiryDate] = defineField('BlacklistExpiryDate');
// const [BlacklistDescription] = defineField('BlacklistDescription');
// const [BlacklistRemark] = defineField('BlacklistRemark');
// const [BizPartnerName] = defineField('BizPartnerName');
// const [Customer] = defineField('Customer');
const isDataLoaded = ref(false);

// ✨✨✨✨✨✨✨✨✨✨✨✨ Grid(AG-Grid) 定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ Grid Column 定義
const gridColumnDefs = ref([
    { headerName: '訂購日期', field: 'OrderDate', sortable: true, filter: 'agTextColumnFilter', sort: 'asc' },
    { headerName: '訂單編號', field: 'OrderNo', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '客戶編號', field: 'CustomerID', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '客戶名稱', field: 'FullName', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '業務人員', field: 'PersonName', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '業績', field: 'Sale', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '異動金額', field: 'OrderMoney', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '訂金%數', field: 'DepositPercent', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '訂金不足', field: 'DepositLack', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '訂金70%', field: 'Deposit70Percent', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '訂金30%', field: 'Deposit30Percent', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '實際出貨時間', field: 'ActShipmentDate', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '預計交貨日', field: 'ExpectedDeliveryDate', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '配送司機', field: 'DeliveryDriver', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '是否已有業績比例', field: 'IsExistSale', sortable: true, filter: 'agTextColumnFilter' }
]);

// ✅ 解構賦值(Destructuring Assignment) 來自 Composables 封裝好的邏輯與定義
const {
    gridApi: gridApi,
    gridSelectedRows: gridSelectedRows,
    gridOptions: gridOptions,
    onGridReady: onGridReady,
    onSelectionChanged: onSelectionChanged
} = useGridOptions(apiService.postApiWebSalePerformanceList, gridColumnDefs.value, 'OrderNo', toast, true);

// ✅ 上述 gridOptions 如需調整，在此複寫
//gridOptions.value.rowSelection.mode = 'multiRow'; // 多選
//gridOptions.value.rowSelection.checkboxes = true; // 顯示 checkbox
//gridOptions.value.multiSortKey = ''; // 清空為關閉多重排序，'ctrl'為按下 Ctrl 鍵或 Cmd 鍵才觸發，alwaysMultiSort = true 為不按鍵即可觸發多重排序
gridOptions.value.autoSizeStrategy.type = ''; // 不自動調整欄寬
// 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚 END. Grid(AG-Grid) 定義區 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚

// ✨✨✨✨✨✨✨✨✨✨✨✨ 變數(資料等)區 ✨✨✨✨✨✨✨✨✨✨✨✨

// const isViewOnly = ref(true);

// ✨✨✨✨✨✨✨✨✨✨✨✨ [計算](computed)方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
const BlacklistLevelOptions = computed(() => {
    return parameterList.value.filter((p) => p.Category === 'BlacklistLevel');
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ [監聽](watch)變數方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
// watch(
//     parameterList,
//     async (newVal, oldVal) => {
//         if (newVal && newVal.length > 0) {
//             // 確保資料加載完成後，更新 AG-Grid 的篩選器選項
//             const filterParams = gridColumnDefs.value.find((col) => col.field === 'BlacklistLevelDescription')?.filterParams;
//             if (filterParams) {
//                 filterParams.values = newVal.filter((p) => p.Category === 'BlacklistLevel').map((item) => item.Code);
//             }
//         }
//     },
//     { deep: true }
// );

// ✨✨✨✨✨✨✨✨✨✨✨✨ [參數取得]區與功能呼叫區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 各類參數一次取得
async function fetchCollection(): Promise<void> {
    try {
        parameterList.value = await apiService.callApi(apiService.postApiWebGetListParameters, ['BlacklistLevel']);
        isDataLoaded.value = true;
    } catch (error) {
        throw error;
    }
}

// ✨✨✨✨✨✨✨✨✨✨✨✨ 各類[對話框]操作相關區 ✨✨✨✨✨✨✨✨✨✨✨✨
const editDialog = ref(false); // 編輯對話框開關
const editDialogHeader = ref(''); // 編輯對話框的標題: 新增 或 修改
const deleteDialog = ref(false); // 刪除對話框開關
const deleteDescription = ref(''); // 刪除對話框的描述
const editMode = ref(''); // 編輯模式: 新增(add) 或 修改(edit) 或 刪除(del)

// 打開對話框、綁定資料
const onButtonClickAction = async (buttonType: string) => {};

// 表單提交區 ////////////////////////////////////////////////////////////////////////////////////////////////
const dialogSubmit = handleSubmit(async (values: FormCompositeData) => {
    try {
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
});

// 刪除 API，不過 yup 驗證
const deleteDialogSubmit = async () => {
    try {
        toast.add({ severity: 'success', summary: '刪除成功', life: 3000 });
        deleteDialog.value = false;
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
};
</script>

<template>
    <div class="card">
        <!-- 響應式深淺色工具列 -->
        <div class="mb-6 bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4">
            <div class="flex flex-col gap-4 lg:flex-row lg:justify-between lg:items-end">
                <!-- 左側：基本操作 -->
                <div class="bg-gradient-to-b from-blue-50 to-blue-100 dark:from-blue-900/30 dark:to-blue-800/30 rounded-lg border border-blue-200 dark:border-blue-700/50 p-3 flex-1 max-w-md">
                    <div class="flex items-center gap-2 mb-3 pb-2 border-b border-blue-200 dark:border-blue-700/50">
                        <i class="fa-solid fa-cogs text-blue-600 dark:text-blue-400 text-sm"></i>
                        <span class="text-sm font-semibold text-blue-700 dark:text-blue-300">基本操作</span>
                    </div>
                    <div class="grid grid-cols-3 gap-2">
                        <Button severity="success" @click="onButtonClickAction('add')" v-tooltip.bottom="'新增客戶提示'">
                            <i class="fa-solid fa-plus mr-1"></i>
                            <span class="hidden sm:inline">新增</span>
                        </Button>
                        <Button severity="info" @click="onButtonClickAction('edit')" v-tooltip.bottom="'修改客戶提示'">
                            <i class="fa-solid fa-pen-to-square mr-1"></i>
                            <span class="hidden sm:inline">修改</span>
                        </Button>
                        <Button severity="danger" @click="onButtonClickAction('del')" v-tooltip.bottom="'刪除客戶提示'">
                            <i class="fa-regular fa-trash-can mr-1"></i>
                            <span class="hidden sm:inline">刪除</span>
                        </Button>
                    </div>
                </div>

                <!-- 右下角：頁面信息（桌面版）/ 底部信息（手機版） -->
                <div class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 lg:self-end">
                    <i class="fa-solid fa-info-circle text-blue-500"></i>
                    <span>僅作警示用途，不影響業務流程</span>
                </div>
            </div>
        </div>

        <ag-grid-vue
            v-if="isDataLoaded"
            class="ag-theme-quartz"
            style="width: 100%; height: calc(100vh - 17.6rem); min-height: 400px"
            :gridOptions="gridOptions"
            @gridReady="onGridReady"
            @rowDoubleClicked="onButtonClickAction('edit')"
            @selection-changed="onSelectionChanged"
        >
        </ag-grid-vue>
    </div>
    <!-- 
    <Dialog v-model:visible="editDialog" class="w-full max-w-[95%] md:max-w-[50%] lg:max-w-[40%]" :header="editDialogHeader" :modal="true">
        <form @submit.prevent="dialogSubmit">
            <div class="space-y-4">
                <Fieldset legend="客戶提示">
                    <div class="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-2 gap-4">
                        <div class="col-span-1">
                            <span class="block font-bold mb-3">客戶編號</span>
                            <div class="px-3 py-2 bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-md text-gray-700 dark:text-gray-300 min-h-[2.5rem] flex items-center">
                                {{ BizPartnerId || '' }}
                            </div>
                        </div>
                        <div class="col-span-1">
                            <span class="block font-bold mb-3">客戶姓名</span>
                            
                            <template v-if="!isViewOnly">
                                <CustomerOnlySelector v-model="Customer" @change="onCustomerChanged" :invalid="!!errors?.['Customer']" :toast="toast" />
                                <small class="text-red-500">{{ errors?.['Customer.BizPartnerId'] || errors?.['Customer'] }}</small>
                            </template>

                            <template v-else>
                                <div class="px-3 py-2 bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-md text-gray-700 dark:text-gray-300 min-h-[2.5rem] flex items-center">
                                    {{ BizPartnerName || '' }}
                                </div>
                            </template>
                        </div>
                        <div class="col-span-1">
                            <span class="block font-bold mb-3">提示等級</span>
                            <Select
                                inputId="BlacklistLevel"
                                v-model="BlacklistLevel"
                                :options="BlacklistLevelOptions"
                                :invalid="!!errors?.['BlacklistLevel']"
                                optionLabel="Description"
                                optionValue="Code"
                                placeholder="請選擇"
                                class="w-full"
                                filter
                                fluid
                            />
                            <small class="text-blue-600 text-sm mt-1 block">※ 此設定僅作為提示用途，不會影響業務流程進行</small>
                            <small class="text-red-500">{{ errors?.['BlacklistLevel'] }}</small>
                        </div>
                        <div class="col-span-1">
                            <span class="block font-bold mb-3">生效日期</span>
                            <DatePicker
                                :model-value="BlacklistEffectiveDate ? dayjs(BlacklistEffectiveDate.toString()).toDate() : null"
                                @update:model-value="BlacklistEffectiveDate = $event instanceof Date ? dayjs($event).format('YYYY-MM-DD') : null"
                                :invalid="!!errors?.['BlacklistEffectiveDate']"
                                dateFormat="yy-mm-dd"
                                showIcon
                                fluid
                            />
                            <small class="text-red-500">{{ errors?.['BlacklistEffectiveDate'] }}</small>
                        </div>
                        <div class="col-span-2">
                            <span class="block font-bold mb-3">説明描述</span>
                            <InputText id="BlacklistDescription" v-model="BlacklistDescription" :invalid="!!errors?.['BlacklistDescription']" fluid />
                            <small class="text-red-500">{{ errors?.['BlacklistDescription'] }}</small>
                        </div>
                        <div class="col-span-2">
                            <span class="block font-bold mb-3">備註</span>
                            <InputText id="BlacklistRemark" v-model="BlacklistRemark" :invalid="!!errors?.['BlacklistRemark']" fluid />
                            <small class="text-red-500">{{ errors?.['BlacklistRemark'] }}</small>
                        </div>
                    </div>
                </Fieldset>

                <div class="flex justify-end items-center gap-4 pt-6">
                    <i v-if="errors && Object.keys(errors).length > 0" class="pi pi-exclamation-triangle !text-lg text-red-500"> 請檢查輸入 </i>
                    <i v-if="isLoading" class="loading-spinner-inline"></i>
                    <Button label="取消" icon="pi pi-times" text @click="editDialog = false" />
                    <Button label="存檔" icon="pi pi-check" outlined @click="dialogSubmit" />
                </div>
            </div>
        </form>
    </Dialog>

    <Dialog v-model:visible="deleteDialog" :style="{ width: '450px' }" header="請小心" :modal="true">
        <div class="flex items-center gap-4">
            <i class="pi pi-exclamation-triangle !text-3xl" />
            <span v-if="deleteDescription"
                ><b>{{ deleteDescription }}</b></span
            >
        </div>
        <template #footer>
            <Button label="否" icon="pi pi-times" text @click="deleteDialog = false" />
            <Button label="是" icon="pi pi-check" @click="deleteDialogSubmit" />
        </template>
    </Dialog> -->
</template>
