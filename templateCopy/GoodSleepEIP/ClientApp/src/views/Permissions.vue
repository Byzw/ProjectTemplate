<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 權限控管設定頁面
// ModuleName: Permissions
// Backend: PermissionsController.cs
// Database: Permissions
// Author: Keng-hua Ku
// Version: 20250205, 1.0
// Memo: demo 類型 - 標準 grid crud
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️

import { useGridOptions } from '@/composables/useGrid';
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import * as gridClient from '@/service/gridClient';
import { useAuthStore } from '@/stores/auth';
import * as gridFormatter from '@/utils/gridFormatter';
import { useToast } from 'primevue/usetoast';
import { useForm } from 'vee-validate';
import { computed, onMounted, ref, watch } from 'vue';
import * as yup from 'yup';

const toast = useToast();
const authStore = useAuthStore();

onMounted(async () => {
    try {
        await fetchCollection();
    } catch (error) {
        toast.add({ severity: 'error', summary: '初始化參數錯誤!', detail: error, life: 5000 });
    }
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ 各類(寫死)參數定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
//const myTestV1 = ref('t1');

// ✨✨✨✨✨✨✨✨✨✨✨✨ 資料模型定義、初始值、驗證、表單元素值綁定 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ 1. 定義表單 `Composite [Model]`，要用到的資料模型都先組合在這邊，組合方法有很多可問 ChatGPT，比如 extends Pick/Omit
export interface FormCompositeData extends models.Permissions {
    ttest?: string;
}

// ✅ 2. 定義表單的[初始值](比如新增資料)，使用 initialValues 來初始化表單
// 📑元素初始值 (比如新增資料)，使用 initialValues 來初始化表單，
// 📑表單內[有的務必設定]，否則不會依此值初始化，會造成之前資料還在
const initialValues: FormCompositeData = {
    PermissionId: 0,
    ModuleName: '',
    PermissionModuleSubType: '',
    PermissionDescription: '',
    IsPublic: false,
    CanCreate: false,
    CanDelete: false,
    CanManage: false,
    CanRead: false,
    CanReadAll: false,
    CanUpdate: false,
    IsSystemReserved: false,
    CreationTime: new Date().toISOString(),
    ttest: 'test'
};

// ✅ 3. 定義表單[驗證]規則，用 yup
const validationSchema = yup.object({
    ModuleName: yup.string().max(90, '打太長啦').min(3, '模組名稱太短').required('尚未輸入模組名稱'),
    PermissionModuleSubType: yup.string().max(90, '太長').required('尚未輸入次類別名稱')
});

// ✅ 4. 用上述初始值與驗證規則，初始化表單，解構賦值(Destructuring Assignment) handleSubmit, errors...等屬性
const { handleSubmit, errors, values, resetForm, defineField } = useForm<FormCompositeData>({
    validationSchema,
    initialValues
});

// ✅ 5. <template>元素值綁定，使用 defineField 來綁定表單元素的值
// 📑 defineField() 的第二個返回值是表單元素的事件與屬性，比如: onInput/onChange...value, checked, disabled, readonly...目前用不到，可以自行參閱 VeeValidate 文件
const [ModuleName] = defineField('ModuleName');
const [PermissionModuleSubType] = defineField('PermissionModuleSubType');
const [PermissionDescription] = defineField('PermissionDescription');
const [IsPublic] = defineField('IsPublic');
const [CanCreate] = defineField('CanCreate');
const [CanDelete] = defineField('CanDelete');
const [CanManage] = defineField('CanManage');
const [CanRead] = defineField('CanRead');
const [CanReadAll] = defineField('CanReadAll');
const [CanUpdate] = defineField('CanUpdate');
const [ttest] = defineField('ttest');

// ✨✨✨✨✨✨✨✨✨✨✨✨ Grid(AG-Grid) 定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ Grid Column 定義
const gridColumnDefs = ref([
    { headerName: '模組名稱', field: 'ModuleName', width: 230, sortable: true, filter: 'agTextColumnFilter', sort: 'asc', cellRenderer: (params) => gridFormatter.lockIconRenderer(params, params.data?.IsSystemReserved) },
    { headerName: '模組次類別', field: 'PermissionModuleSubType', width: 250, sortable: true, filter: 'agTextColumnFilter', sort: 'asc' },
    { headerName: '權限描述說明', field: 'PermissionDescription', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '公用模組', field: 'IsPublic', sortable: true, filter: 'agTextColumnFilter', cellRenderer: gridFormatter.booleanCheckIconRenderer, cellStyle: { 'text-align': 'center' } },
    { headerName: '可讀', field: 'CanRead', sortable: true, filter: 'agTextColumnFilter', cellRenderer: gridFormatter.booleanCheckIconRenderer, cellStyle: { 'text-align': 'center' } },
    { headerName: '可讀全部', field: 'CanReadAll', sortable: true, filter: 'agTextColumnFilter', cellRenderer: gridFormatter.booleanCheckIconRenderer, cellStyle: { 'text-align': 'center' } },
    { headerName: '可建立', field: 'CanCreate', sortable: true, filter: 'agTextColumnFilter', cellRenderer: gridFormatter.booleanCheckIconRenderer, cellStyle: { 'text-align': 'center' } },
    { headerName: '可修改', field: 'CanUpdate', sortable: true, filter: 'agTextColumnFilter', cellRenderer: gridFormatter.booleanCheckIconRenderer, cellStyle: { 'text-align': 'center' } },
    { headerName: '可刪除', field: 'CanDelete', sortable: true, filter: 'agTextColumnFilter', cellRenderer: gridFormatter.booleanCheckIconRenderer, cellStyle: { 'text-align': 'center' } },
    { headerName: '可管理', field: 'CanManage', sortable: true, filter: 'agTextColumnFilter', cellRenderer: gridFormatter.booleanCheckIconRenderer, cellStyle: { 'text-align': 'center' } },
    { headerName: '建立時間', field: 'CreationTime', sortable: true, filter: 'agTextColumnFilter', width: 230, valueFormatter: gridFormatter.dateTimeFormatter }
]);
// ✅ 解構賦值(Destructuring Assignment) 來自 Composables 封裝好的邏輯與定義
const { gridApi, gridSelectedRows, gridOptions, onGridReady, onSelectionChanged } = useGridOptions(apiService.postApiWebPermissionsList, gridColumnDefs.value, 'PermissionId', toast);

// ✅ 上述 gridOptions 如需調整，在此複寫
//gridOptions.value.rowSelection.mode = 'multiRow'; // 多選
//gridOptions.value.rowSelection.checkboxes = true; // 顯示 checkbox
//gridOptions.value.multiSortKey = ''; // 清空為關閉多重排序，'ctrl'為按下 Ctrl 鍵或 Cmd 鍵才觸發，alwaysMultiSort = true 為不按鍵即可觸發多重排序
//gridOptionsRoles.value.autoSizeStrategy.type = ''; // 自動調整欄寬
// 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚 END. Grid(AG-Grid) 定義區 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚

// ✨✨✨✨✨✨✨✨✨✨✨✨ 變數(資料等)區 ✨✨✨✨✨✨✨✨✨✨✨✨
const parameterList = ref<models.Parameter[]>([]);

// ✨✨✨✨✨✨✨✨✨✨✨✨ [監聽](watch)變數方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
watch(IsPublic, (newValue) => {
    // IsPublic 為 true，則權限為 false
    if (newValue) {
        CanRead.value = false;
        CanReadAll.value = false;
        CanCreate.value = false;
        CanUpdate.value = false;
        CanDelete.value = false;
        CanManage.value = false;
    }
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ [計算](computed)方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
const permissionModuleSubTypeOptions = computed(() => {
    return parameterList.value.filter((p) => p.Category === 'PermissionModuleSubType');
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ [參數取得]區與功能呼叫區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 各類參數一次取得
async function fetchCollection(): Promise<void> {
    try {
        parameterList.value = await apiService.callApi(apiService.getApiWebGetPrefixParameters, { CategoryPrefix: 'Permission' });
    } catch (error) {
        throw error;
    }
}

// ✨✨✨✨✨✨✨✨✨✨✨✨ 各類[對話框]操作相關區 ✨✨✨✨✨✨✨✨✨✨✨✨
const editDialog = ref(false); // 編輯對話框開關
const editDialogHeader = ref(''); // 編輯對話框的標題: 新增 或 修改
const deleteDialog = ref(false); // 刪除對話框開關
const deleteDescription = ref(''); // 刪除對話框的描述
const editMode = ref(''); // 編輯模式: 新增(add) 或 修改(edit)

// 打開對話框、綁定資料
const onButtonClickAction = async (buttonType: string) => {
    editMode.value = buttonType;

    // 修改
    if (buttonType == 'edit') {
        if (!(gridSelectedRows.value.length > 0)) return;
        try {
            const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得 Grid 選中的行資料
            if (currentGridRecord.IsSystemReserved) {
                toast.add({ severity: 'warn', summary: '不行喔', detail: '此筆權限為系統保留，無法修改', life: 3000 });
                return;
            }
            //const fetchRecordData: models.Permissions = await apiService.callApi(apiService.getApiWebFetchPermissionsRecord, { PermissionId: currentGridRecord.PermissionId });
            await resetForm({ values: { ...currentGridRecord } });

            editDialogHeader.value = `修改模組權限: ${currentGridRecord.ModuleName}-${currentGridRecord.PermissionModuleSubType} (${currentGridRecord.PermissionDescription}`;
            editDialog.value = true;
        } catch (error) {
            toast.add({ severity: 'error', summary: '載入表單發生錯誤', detail: error, life: 3000 });
            return;
        }
    } else if (buttonType == 'add') {
        editDialogHeader.value = '新增績效目標';

        await resetForm({ values: initialValues }); // 重置表單值並給予初始值初始化表單

        editDialog.value = true;
    } else if (buttonType == 'del') {
        if (!(gridSelectedRows.value.length > 0)) return;
        const currentGridRecord: FormCompositeData = gridSelectedRows.value[0];
        if (currentGridRecord.IsSystemReserved) {
            toast.add({ severity: 'warn', summary: '不行喔', detail: '此筆權限為系統保留，無法刪除', life: 3000 });
            return;
        }
        deleteDescription.value = `確定要刪除模組權限: ${currentGridRecord.ModuleName}-${currentGridRecord.PermissionModuleSubType} (${currentGridRecord.PermissionDescription}) 嗎?`;
        deleteDialog.value = true;
    } else {
        console.warn(`無法辨識的 Action 類型，buttonType: ${buttonType}`, '資料:', gridSelectedRows.value);
    }
};

// 表單提交區 ////////////////////////////////////////////////////////////////////////////////////////////////
const dialogSubmit = handleSubmit(async (values: FormCompositeData) => {
    try {
        // 新增 //////////////////////////////////////////////////////////////////////////////////////////////
        if (editMode.value == 'add') {
            const submitData = ref<FormCompositeData>({
                ...values
            });
            await apiService.callApi(apiService.postApiWebPermissionsAdd, submitData.value);

            // 更新列表
            gridApi.value.refreshServerSide({ route: [], purge: true }); // 當 purge: true 時，會清除現有資料並顯示 loading 狀態，直到新的資料從伺服器載入進來， false 僅更新當頁資料
            toast.add({ severity: 'success', summary: '新增成功', life: 3000 });
            // 修改 //////////////////////////////////////////////////////////////////////////////////////////////
        } else if (editMode.value == 'edit' && values.PermissionId > 0) {
            const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得選中的行資料

            const submitData = ref<FormCompositeData>({
                ...values,
                PermissionId: currentGridRecord.PermissionId
            });

            await apiService.callApi(apiService.postApiWebPermissionsEdit, submitData.value);

            // 編輯成功後，從後端取得最新的資料，使用 gridApi: getRowNode->setData 刷新或更新當前行的資料，而不是重新載入整個列表
            const updatedRowData = await gridClient.getOneRowFromApi(apiService.postApiWebPermissionsList, currentGridRecord.PermissionId.toString(), 'PermissionId');
            const rowNode = gridApi.value.getRowNode(currentGridRecord.PermissionId.toString());
            if (rowNode) rowNode.setData(updatedRowData);

            toast.add({ severity: 'success', summary: '修改成功', life: 3000 });
        } else {
            throw new Error('編輯模式錯誤' + editMode.value);
        }

        gridApi.value.deselectAll(); // 取消選中行，避免取表格內的資料時取到舊資料
        editDialog.value = false; // 關閉編輯對話框
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
});

// 刪除 API，不過 yup 驗證
const deleteDialogSubmit = async () => {
    try {
        const currentGridRecord: FormCompositeData = gridSelectedRows.value[0];
        await apiService.callApi(apiService.getApiWebPermissionsDel, { PermissionId: currentGridRecord.PermissionId });

        gridApi.value.refreshServerSide({ route: [], purge: false }); // purge false 只更新當頁資料

        toast.add({ severity: 'success', summary: '刪除成功', life: 3000 });
        deleteDialog.value = false;
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
};
</script>

<template>
    <div>
        <div class="card">
            <Toolbar class="mb-6 overflow-x-auto whitespace-nowrap">
                <template #start>
                    <Button severity="secondary" class="mr-2" @click="onButtonClickAction('add')">
                        <span class="fa-solid fa-plus" data-pc-section="icon"></span>
                        <span class="hidden sm:inline" data-pc-section="label">新增</span>
                    </Button>
                    <Button severity="secondary" class="mr-2" @click="onButtonClickAction('edit')">
                        <span class="fa-solid fa-pen-to-square" data-pc-section="icon" />
                        <span class="hidden sm:inline" data-pc-section="label">修改</span>
                    </Button>
                    <Button severity="secondary" class="mr-2" @click="onButtonClickAction('del')">
                        <span class="fa-regular fa-trash-can" data-pc-section="icon" />
                        <span class="hidden sm:inline" data-pc-section="label">刪除</span>
                    </Button>
                </template>
                <template #end></template>
            </Toolbar>

            <ag-grid-vue class="ag-theme-quartz" style="width: 100%; height: calc(100vh - 17.6rem); min-height: 400px"
                :gridOptions="gridOptions" @gridReady="onGridReady" @rowDoubleClicked="onButtonClickAction('edit')"
                @selection-changed="onSelectionChanged">
            </ag-grid-vue>
        </div>

        <Dialog v-model:visible="editDialog" class="w-full max-w-[95%] md:max-w-[80%] lg:max-w-[70%]"
            :header="editDialogHeader" :modal="true">
            <form @submit.prevent="dialogSubmit">
                <div class="space-y-4">
                    <Fieldset legend="模組辨識">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">模組名稱</span>
                                <InputText id="ModuleName" v-model="ModuleName" :invalid="!!errors?.['ModuleName']"
                                    fluid />
                                <small class="text-red-500">{{ errors?.['ModuleName'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">模組次類別</span>
                                <Select id="PermissionModuleSubType" v-model="PermissionModuleSubType"
                                    :options="permissionModuleSubTypeOptions"
                                    :invalid="!!errors?.['PermissionModuleSubType']" optionLabel="Description"
                                    optionValue="Description" placeholder="請選擇" class="w-full" filter />
                                <small class="text-red-500">{{ errors?.['PermissionModuleSubType'] }}</small>
                            </div>
                            <div class="col-span-2">
                                <span class="block font-bold mb-3">模組描述</span>
                                <InputText id="PermissionDescription" v-model="PermissionDescription"
                                    :invalid="!!errors?.['PermissionDescription']" fluid />
                                <small class="text-red-500">{{ errors?.['PermissionDescription'] }}</small>
                            </div>
                        </div>
                    </Fieldset>
                    <Fieldset legend="公用模組">
                        <div class="grid mt-4">
                            <div class="flex items-center gap-2">
                                <ToggleSwitch inputId="IsPublic" v-model="IsPublic" />
                                <span v-if="IsPublic">是，對所有人開放</span>
                                <span v-if="!IsPublic">否，一般模組</span>
                            </div>
                        </div>
                    </Fieldset>
                    <Fieldset legend="權限設定">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-2">
                            <div class="flex items-center gap-2">
                                <Checkbox v-model="CanRead" name="CanRead" inputId="CanRead" binary
                                    :disabled="IsPublic" />
                                <label for="CanRead">可讀</label>
                            </div>
                            <div class="flex items-center gap-2">
                                <Checkbox v-model="CanReadAll" name="CanReadAll" inputId="CanReadAll" binary
                                    :disabled="IsPublic" />
                                <label for="CanReadAll">可讀全部</label>
                            </div>
                            <div class="flex items-center gap-2">
                                <Checkbox v-model="CanCreate" name="CanCreate" inputId="CanCreate" binary
                                    :disabled="IsPublic" />
                                <label for="CanCreate">可建立</label>
                            </div>
                            <div class="flex items-center gap-2">
                                <Checkbox v-model="CanUpdate" name="CanUpdate" inputId="CanUpdate" binary
                                    :disabled="IsPublic" />
                                <label for="CanUpdate">可修改</label>
                            </div>
                            <div class="flex items-center gap-2">
                                <Checkbox v-model="CanDelete" name="CanDelete" inputId="CanDelete" binary
                                    :disabled="IsPublic" />
                                <label for="CanDelete">可刪除</label>
                            </div>
                            <div class="flex items-center gap-2">
                                <Checkbox v-model="CanManage" name="CanManage" inputId="CanManage" binary
                                    :disabled="IsPublic" />
                                <label for="CanManage">可管理(等於全部)</label>
                            </div>
                        </div>
                    </Fieldset>
                    <div class="flex justify-end items-center gap-4 pt-6">
                        <i v-if="errors && Object.keys(errors).length > 0"
                            class="pi pi-exclamation-triangle !text-lg text-red-500">
                            請檢查輸入
                        </i>
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
                <span v-if="deleteDescription"><b>{{ deleteDescription }}</b></span>
            </div>
            <template #footer>
                <Button label="否" icon="pi pi-times" text @click="deleteDialog = false" />
                <Button label="是" icon="pi pi-check" @click="deleteDialogSubmit" />
            </template>
        </Dialog>
    </div>
</template>
