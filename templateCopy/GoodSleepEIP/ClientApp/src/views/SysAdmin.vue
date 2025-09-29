<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 底層系統管理，請勿隨意開放，包括參數設定、資料庫備份等
// ModuleName: SysAdmin 建議不要單獨指定此權限，透過管理員群組或角色來取得他
// Backend: SysAdmin.cs
// Database: Parameter
// Author: Keng-hua Ku
// Version: 20250225, 1.0
// Memo: 
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️

import { useGridOptions } from '@/composables/useGrid';
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import * as gridClient from '@/service/gridClient';
import { useAuthStore } from '@/stores/auth';
import * as gridFormatter from '@/utils/gridFormatter';
import { useToast } from 'primevue/usetoast';
import { useForm } from 'vee-validate';
import { watch, computed, onMounted, ref } from 'vue';
import dayjs from 'dayjs';
import * as yup from 'yup';
import * as uuid from 'uuid';

const toast = useToast();
const authStore = useAuthStore();

// ✨✨✨✨✨✨✨✨✨✨✨✨ 各類(寫死)參數定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
//const myTestV1 = ref('t1');

// ✨✨✨✨✨✨✨✨✨✨✨✨ 資料模型定義、初始值、驗證、表單元素值綁定 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ 1. 定義表單 `Composite [Model]`，要用到的資料模型都先組合在這邊，組合方法有很多可問 ChatGPT，比如 extends Pick/Omit
export interface FormCompositeDataParameter extends models.Parameter { }

// ✅ 2. 定義表單的[初始值](比如新增資料)，使用 initialValues 來初始化表單
// 📑元素初始值 (比如新增資料)，使用 initialValues 來初始化表單，
// 📑表單內[有的務必設定]，否則不會依此值初始化，會造成之前資料還在
const initialValuesParameter: FormCompositeDataParameter = {
    ParameterId: '00000000-0000-0000-0000-000000000000',
    Category: '',
    Code: '',
    Description: '',
    Memo: null,
    IsSystemReserved: false
};

// ✅ 3. 定義表單[驗證]規則，用 yup
const validationSchemaParameter = yup.object({
    Category: yup.string().max(40, '打太長啦').min(2, '類別名稱太短').required('尚未輸入類別名稱'),
    Code: yup.string().max(8, '打太長啦').min(1, '編碼值太短').required('尚未輸入編碼值'),
    Description: yup.string().max(100, '打太長啦').min(1, '參數描述太短').required('尚未輸入參數描述'),
    Memo: yup.string().max(80, '打太長啦').nullable()
});

// ✅ 4. 用上述初始值與驗證規則，初始化表單，解構賦值(Destructuring Assignment) handleSubmit, errors...等屬性
const {
    handleSubmit: handleSubmitParameter,
    errors: errorsParameter,
    values: valuesParameter,
    resetForm: resetFormParameter,
    defineField: defineFieldParameter
} = useForm<FormCompositeDataParameter>({
    validationSchema: validationSchemaParameter,
    initialValues: initialValuesParameter
});

// ✅ 5. <template>元素值綁定，使用 defineField 來綁定表單元素的值
// 📑 defineField() 的第二個返回值是表單元素的事件與屬性，比如: onInput/onChange...value, checked, disabled, readonly...目前用不到，可以自行參閱 VeeValidate 文件
const [ParameterID] = defineFieldParameter('ParameterId');
const [Category] = defineFieldParameter('Category');
const [Code] = defineFieldParameter('Code');
const [Description] = defineFieldParameter('Description');
const [Memo] = defineFieldParameter('Memo');
const [IsSystemReserved] = defineFieldParameter('IsSystemReserved');

// ✨✨✨✨✨✨✨✨✨✨✨✨ Grid(AG-Grid) 定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ Grid Column 定義
const gridColumnDefsParameter = ref([
    { headerName: '類別', field: 'Category', sortable: true, width: 300, filter: 'agTextColumnFilter', cellRenderer: (params) => gridFormatter.lockIconRenderer(params, params.data.IsSystemReserved), sort: 'asc', sortIndex: 0 },
    { headerName: '編碼值', field: 'Code', sortable: true, filter: 'agTextColumnFilter', sort: 'asc', sortIndex: 1 },
    { headerName: '描述', field: 'Description', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '備註', field: 'Memo', sortable: true, filter: 'agTextColumnFilter' }
]);

// ✅ 解構賦值(Destructuring Assignment) 來自 Composables 封裝好的邏輯與定義
const {
    gridApi: gridApiParameter,
    gridSelectedRows: gridSelectedRowsParameter,
    gridOptions: gridOptionsParameter,
    onGridReady: onGridReadyParameter,
    onSelectionChanged: onSelectionChangedParameter,
    reloadData: reloadDataParameter
} = useGridOptions(apiService.postApiWebParameterList, gridColumnDefsParameter.value, 'ParameterId', toast, true);

// ✅ 上述 gridOptions 如需調整，在此複寫
//gridOptions.value.rowSelection.mode = 'multiRow'; // 多選
//gridOptions.value.rowSelection.checkboxes = true; // 顯示 checkbox
//gridOptions.value.multiSortKey = ''; // 清空為關閉多重排序，'ctrl'為按下 Ctrl 鍵或 Cmd 鍵才觸發，alwaysMultiSort = true 為不按鍵即可觸發多重排序
gridOptionsParameter.value.autoSizeStrategy.type = ''; // 不自動調整欄寬，要能自動調整，要 grid 能被看到的情況下才行喔
// 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚 END. Grid(AG-Grid) 定義區 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚

// ✨✨✨✨✨✨✨✨✨✨✨✨ 變數(資料等)區 ✨✨✨✨✨✨✨✨✨✨✨✨
const parameterList = ref<models.Parameter[]>([]);

// ✨✨✨✨✨✨✨✨✨✨✨✨ [監聽](watch)變數方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
const activeTab = ref('Parameter'); // 頁籤切換值
watch(activeTab, async (newValue) => {
    if (newValue === 'Parameter') await reloadDataParameter();
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ [計算](computed)方法區 ✨✨✨✨✨✨✨✨✨✨✨✨


// ✨✨✨✨✨✨✨✨✨✨✨✨ [參數取得]區與功能呼叫區 ✨✨✨✨✨✨✨✨✨✨✨✨


// ✨✨✨✨✨✨✨✨✨✨✨✨ 各類[對話框]操作相關區 ✨✨✨✨✨✨✨✨✨✨✨✨
const editDialogParameter = ref(false); // 角色編輯對話框開關
const editDialogParameterHeader = ref(''); // 編輯對話框的標題: 新增 或 修改

const deleteDialog = ref(false); // 刪除對話框開關
const deleteDescription = ref(''); // 刪除對話框的描述

const editMode = ref(''); // 編輯模式: 新增(add) 或 修改(edit) 或 刪除(del)

// 打開對話框、綁定資料
const onButtonClickAction = async (buttonType: string) => {
    editMode.value = buttonType;

    // Parameter 修改
    if (buttonType == 'editParameter') {
        if (!(gridSelectedRowsParameter.value.length > 0)) return;
        try {
            const currentGridRecord: FormCompositeDataParameter = gridSelectedRowsParameter.value[0]; // 取得 Grid 選中的行資料
            if (uuid.validate(currentGridRecord.ParameterId) == false) throw new Error('無法取得選擇資料');
            if (currentGridRecord.IsSystemReserved) {
                toast.add({ severity: 'warn', summary: '不行喔', detail: '此筆參數為系統保留，無法修改', life: 3000 });
                return;
            }

            const fetchRecordData: models.Parameter = await apiService.callApi(apiService.getApiWebFetchParameterRecord, { ParameterId: currentGridRecord.ParameterId });
            await resetFormParameter({ values: { ...fetchRecordData } });

            editDialogParameterHeader.value = `修改參數: ${currentGridRecord.Category}-${currentGridRecord.Code}`;
            editDialogParameter.value = true;
        } catch (error) {
            toast.add({ severity: 'error', summary: '載入表單發生錯誤', detail: error, life: 3000 });
            return;
        }
        // Parameter 新增
    } else if (buttonType == 'addParameter') {
        editDialogParameterHeader.value = '新增參數';
        await resetFormParameter({ values: initialValuesParameter }); // 重置表單值並給予初始值初始化表單
        editDialogParameter.value = true;
        // Parameter 刪除
    } else if (buttonType == 'delParameter') {
        if (!(gridSelectedRowsParameter.value.length > 0)) return;
        const currentGridRecord: FormCompositeDataParameter = gridSelectedRowsParameter.value[0]; // 取得 Grid 選中的行資料
        if (currentGridRecord.IsSystemReserved) {
            toast.add({ severity: 'warn', summary: '不行喔', detail: '此筆參數為系統保留，無法刪除', life: 3000 });
            return;
        }
        deleteDescription.value = `確定要刪除參數: ${currentGridRecord.Category}-${currentGridRecord.Code} 嗎?`;
        deleteDialog.value = true;
    } else {
        console.warn(`無法辨識的 Action 類型，buttonType: ${buttonType}`);
    }
};

// 表單提交區 ////////////////////////////////////////////////////////////////////////////////////////////////
const dialogSubmitParameter = handleSubmitParameter(async (values: FormCompositeDataParameter) => {
    try {
        // 新增 //////////////////////////////////////////////////////////////////////////////////////////////
        if (editMode.value == 'addParameter') {
            const submitData = ref<FormCompositeDataParameter>({ ...values });
            await apiService.callApi(apiService.postApiWebParameterAdd, submitData.value);

            // 更新列表
            gridApiParameter.value.refreshServerSide({ route: [], purge: true }); // 當 purge: true 時，會清除現有資料並顯示 loading 狀態，直到新的資料從伺服器載入進來， false 僅更新當頁資料
            toast.add({ severity: 'success', summary: '新增成功', life: 3000 });
            // 修改 //////////////////////////////////////////////////////////////////////////////////////////////
        } else if (editMode.value == 'editParameter' && uuid.validate(values.ParameterId)) {
            const currentGridRecord: FormCompositeDataParameter = gridSelectedRowsParameter.value[0]; // 取得選中的行資料
            const submitData = ref<FormCompositeDataParameter>({ ...values });
            await apiService.callApi(apiService.postApiWebParameterEdit, submitData.value);

            // 編輯成功後，從後端取得最新的資料，使用 gridApi: getRowNode->setData 刷新或更新當前行的資料，而不是重新載入整個列表
            if (currentGridRecord.ParameterId) {
                const updatedRowData = await gridClient.getOneRowFromApi(apiService.postApiWebParameterList, currentGridRecord.ParameterId, 'ParameterId');
                const rowNode = gridApiParameter.value.getRowNode(currentGridRecord.ParameterId);
                if (rowNode) rowNode.setData(updatedRowData);
            }
            toast.add({ severity: 'success', summary: '修改成功', life: 3000 });
        } else {
            throw new Error('選擇模式錯誤' + editMode.value);
        }

        editDialogParameter.value = false; // 關閉編輯對話框
        gridApiParameter.value.deselectAll(); // 取消選中行，避免取表格內的資料時取到舊資料
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
});

const deleteDialogSubmitParameter = async () => {
    try {
        const currentGridRecord: FormCompositeDataParameter = gridSelectedRowsParameter.value[0]; // 取得選中的行資料
        await apiService.callApi(apiService.getApiWebParameterDel, { ParameterId: currentGridRecord.ParameterId });

        // 刪除成功後，從後端取得最新的資料，使用 gridApi: refreshServerSide 刷新或更新整個列表
        gridApiParameter.value.refreshServerSide({ route: [], purge: false }); // purge false 只更新當頁資料

        toast.add({ severity: 'success', summary: '刪除成功', life: 3000 });
        deleteDialog.value = false;
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
};

const executeTaskBackupDatabaseBtnIsDisabled = ref(false);
const executeTaskBackupDatabase = async () => {
    executeTaskBackupDatabaseBtnIsDisabled.value = true; // 禁用按鈕
    try {
        await apiService.callApi(apiService.postApiTaskBackupDatabase);
        toast.add({ severity: 'success', summary: '已請系統開始產生備份檔，請耐心等候', life: 3000 });
    } finally {
        executeTaskBackupDatabaseBtnIsDisabled.value = false;
    }
};
</script>

<template>
    <div>
        <div class="card">
            <Tabs v-model:value="activeTab" scrollable>
                <TabList>
                    <Tab value="Parameter" as="div" class="flex items-center gap-2">
                        <span class="text-lg font-bold whitespace-nowrap">
                            <i class="fa-solid fa-list-check"></i> 參數設定
                        </span>
                    </Tab>
                    <Tab value="DatabaseBackup" as="div" class="flex items-center gap-2">
                        <span class="text-lg font-bold whitespace-nowrap">
                            <i class="fa-solid fa-database"></i> 資料庫備份
                        </span>
                    </Tab>
                </TabList>
                <TabPanels>
                    <TabPanel value="Parameter" as="div" class="m-0 p-2">
                        <Toolbar class="mt-4 mb-6 overflow-x-auto whitespace-nowrap">
                            <template #start>
                                <Button severity="secondary" class="mr-2" @click="onButtonClickAction('addParameter')">
                                    <span class="fa-solid fa-plus" data-pc-section="icon"></span>
                                    <span class="hidden sm:inline" data-pc-section="label">新增</span>
                                </Button>
                                <Button severity="secondary" class="mr-2" @click="onButtonClickAction('editParameter')">
                                    <span class="fa-solid fa-pen-to-square" data-pc-section="icon" />
                                    <span class="hidden sm:inline" data-pc-section="label">修改</span>
                                </Button>
                                <Button severity="secondary" class="mr-2" @click="onButtonClickAction('delParameter')">
                                    <span class="fa-regular fa-trash-can" data-pc-section="icon" />
                                    <span class="hidden sm:inline" data-pc-section="label">刪除</span>
                                </Button>
                            </template>
                        </Toolbar>

                        <ag-grid-vue class="ag-theme-quartz"
                            style="width: 100%; height: calc(100vh - 25.4rem); min-height: 400px"
                            :gridOptions="gridOptionsParameter" @gridReady="onGridReadyParameter"
                            @rowDoubleClicked="onButtonClickAction('editParameter')"
                            @selection-changed="onSelectionChangedParameter">
                        </ag-grid-vue>
                    </TabPanel>
                    <TabPanel value="DatabaseBackup" as="div" class="m-0 p-2">
                        <Fieldset legend="Microsoft SQL Server 資料庫備份">
                            <span class="block mb-3">本備份為目前 EIP 系統資料庫備份，不包含 ERP
                                系統資料庫，按下『開始執行』後系統即開始進行工作，依資料大小耗時不同，請耐心等候；您可隨時至『通知中心』(上方的小鈴鐺)查閱工作進度。</span>
                            <Button severity="secondary" class="mr-2" :disabled="executeTaskBackupDatabaseBtnIsDisabled"
                                @click="executeTaskBackupDatabase">
                                <span class="fa-solid fa-square-binary" data-pc-section="icon"></span>
                                <span data-pc-section="label">開始執行</span>
                            </Button>
                        </Fieldset>
                    </TabPanel>
                </TabPanels>
            </Tabs>
        </div>

        <Dialog v-model:visible="editDialogParameter" class="w-full max-w-[95%] md:max-w-[80%] lg:max-w-[70%]"
            :header="editDialogParameterHeader" :modal="true">
            <form @submit.prevent="dialogSubmitParameter">
                <div class="space-y-4">
                    <span class="text-lg block font-bold mb-3 text-red-500">若自行更動，可能會造成系統異常崩潰，請謹慎操作，您真的想清楚了嗎?</span>
                    <Panel>
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">參數類別</span>
                                <InputText inputId="Category" v-model="Category"
                                    :invalid="!!errorsParameter?.['Category']" fluid />
                                <small class="text-red-500">{{ errorsParameter?.['Category'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">編碼值</span>
                                <InputText inputId="Code" v-model="Code" :invalid="!!errorsParameter?.['Code']" fluid />
                                <small class="text-red-500">{{ errorsParameter?.['Code'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">參數描述</span>
                                <InputText inputId="Description" v-model="Description"
                                    :invalid="!!errorsParameter?.['Description']" fluid />
                                <small class="text-red-500">{{ errorsParameter?.['Description'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">備註</span>
                                <InputText inputId="Memo" v-model="Memo" :invalid="!!errorsParameter?.['Memo']" fluid />
                                <small class="text-red-500">{{ errorsParameter?.['Memo'] }}</small>
                            </div>
                        </div>
                    </Panel>
                    <div class="flex justify-end items-center gap-4 pt-6">
                        <i v-if="errorsParameter && Object.keys(errorsParameter).length > 0"
                            class="pi pi-exclamation-triangle !text-lg text-red-500">
                            請檢查輸入
                        </i>
                        <i v-if="isLoading" class="loading-spinner-inline"></i>
                        <Button label="取消" icon="pi pi-times" text @click="editDialogParameter = false" />
                        <Button label="存檔" icon="pi pi-check" outlined @click="dialogSubmitParameter" />
                    </div>
                </div>
            </form>
        </Dialog>

        <Dialog v-model:visible="deleteDialog" :style="{ width: '450px' }" header="請小心，世上沒有後悔藥可吃" :modal="true">
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span v-if="deleteDescription"><b>{{ deleteDescription }}</b></span>
            </div>
            <template #footer>
                <Button label="否" icon="pi pi-times" text @click="deleteDialog = false" />
                <Button label="是" icon="pi pi-check" @click="deleteDialogSubmitParameter();" />
            </template>
        </Dialog>
    </div>
</template>
