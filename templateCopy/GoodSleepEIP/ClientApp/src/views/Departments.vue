<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 部門與部門主管設定頁面
// ModuleName: Departments
// Backend: DepartmentsController.cs
// Database: Departments, DepartmentManagers
// Author: Keng-hua Ku
// Version: 20250205, 1.0
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
import { computed, onMounted, ref } from 'vue';
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
export interface FormCompositeData extends models.DepartmentsDTO { }

// ✅ 2. 定義表單的[初始值](比如新增資料)，使用 initialValues 來初始化表單
// 📑元素初始值 (比如新增資料)，使用 initialValues 來初始化表單，
// 📑表單內[有的務必設定]，否則不會依此值初始化，會造成之前資料還在
const initialValues: FormCompositeData = {
    DepartmentId: 0,
    DepartmentName: '',
    DepartmentDescription: '',
    ParentDepartmentId: 0,
    DepartmentLevel: '',
    CreationTime: new Date().toISOString(),
    UpdateTime: new Date().toISOString(),
    Managers: []
}

// ✅ 3. 定義表單[驗證]規則，用 yup
const validationSchema = yup.object({
    DepartmentName: yup.string().max(20, '打太長啦').min(1, '部門代號太短').required('尚未輸入部門名稱'),
    DepartmentDescription: yup.string().max(20, '打太長啦').min(1, '部門名稱太短').required('尚未輸入部門名稱'),
    ParentDepartmentId: yup.number().min(0, '尚未選擇上層部門').required('尚未選擇上層部門'),
    DepartmentLevel: yup.string().required('尚未選擇部門層級')
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
    initialValues: initialValues
});

// ✅ 5. <template>元素值綁定，使用 defineField 來綁定表單元素的值
// 📑 defineField() 的第二個返回值是表單元素的事件與屬性，比如: onInput/onChange...value, checked, disabled, readonly...目前用不到，可以自行參閱 VeeValidate 文件
const [DepartmentName] = defineField('DepartmentName');
const [DepartmentDescription] = defineField('DepartmentDescription');
const [ParentDepartmentId] = defineField('ParentDepartmentId');
const [DepartmentLevel] = defineField('DepartmentLevel');
const [Managers] = defineField('Managers');

// ✨✨✨✨✨✨✨✨✨✨✨✨ Grid(AG-Grid) 定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ Grid Column 定義
const gridColumnDefs = ref([
    { headerName: '部門代號', field: 'DepartmentName', sortable: true, filter: 'agTextColumnFilter', sort: 'asc', sortIndex: 1 },
    { headerName: '部門名稱(說明)', field: 'DepartmentDescription', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '層級', field: 'DepartmentLevel', sortable: true, filter: 'agTextColumnFilter', sort: 'asc', sortIndex: 0 },
    { headerName: '上層部門', field: 'ParentDepartmentName', sortable: true, filter: 'agTextColumnFilter' }
]);

// ✅ 解構賦值(Destructuring Assignment) 來自 Composables 封裝好的邏輯與定義
const {
    gridApi: gridApi,
    gridSelectedRows: gridSelectedRows,
    gridOptions: gridOptions,
    onGridReady: onGridReady,
    onSelectionChanged: onSelectionChanged
} = useGridOptions(apiService.postApiWebDepartmentsList, gridColumnDefs.value, 'DepartmentId', toast);

// ✅ 上述 gridOptions 如需調整，在此複寫
//gridOptions.value.rowSelection.mode = 'multiRow'; // 多選
//gridOptions.value.rowSelection.checkboxes = true; // 顯示 checkbox
//gridOptions.value.multiSortKey = ''; // 清空為關閉多重排序，'ctrl'為按下 Ctrl 鍵或 Cmd 鍵才觸發，alwaysMultiSort = true 為不按鍵即可觸發多重排序
//gridOptionsRoles.value.autoSizeStrategy.type = ''; // 不自動調整欄寬
// 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚 END. Grid(AG-Grid) 定義區 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚

// ✨✨✨✨✨✨✨✨✨✨✨✨ 變數(資料等)區 ✨✨✨✨✨✨✨✨✨✨✨✨
const parameterList = ref<models.Parameter[]>([]);
const departmentsList = ref<models.Departments[]>([]);
const usersList = ref<models.Users[]>([]);

// ✨✨✨✨✨✨✨✨✨✨✨✨ [監聽](watch)變數方法區 ✨✨✨✨✨✨✨✨✨✨✨✨

// ✨✨✨✨✨✨✨✨✨✨✨✨ [計算](computed)方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
const departmentLevelOptions = computed(() => {
    return parameterList.value.filter((p) => p.Category === 'DepartmentLevel');
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ [參數取得]區與功能呼叫區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 各類參數一次取得
async function fetchCollection(): Promise<void> {
    try {
        parameterList.value = await apiService.callApi(apiService.getApiWebGetPrefixParameters, { CategoryPrefix: 'Department' });
        departmentsList.value = await apiService.callApi(apiService.getApiWebFetchDepartmentsList);
        usersList.value = await apiService.callApi(apiService.getApiWebFetchUsersList);
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
const onButtonClickAction = async (buttonType: string) => {
    editMode.value = buttonType;

    // 修改
    if (buttonType == 'edit') {
        if (!(gridSelectedRows.value.length > 0)) return;
        try {
            const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得 Grid 選中的行資料
            if (currentGridRecord.DepartmentId == 0) throw new Error('無法取得選擇資料');

            const fetchRecordData: models.Departments = await apiService.callApi(apiService.getApiWebFetchDepartmentRecord, { DepartmentId: currentGridRecord.DepartmentId });
            // 取得此部門所關聯的全部 DepartmentManagers(部門主管們)
            const departmentManagers: models.DepartmentManagers[] = await apiService.callApi(apiService.getApiWebFetchDepartmentManagersRecord, { DepartmentId: currentGridRecord.DepartmentId });
            await resetForm({ values: { ...fetchRecordData, Managers: departmentManagers.map(manager => manager.UserId) } });

            editDialogHeader.value = `修改部門: ${currentGridRecord.DepartmentName}`;
            editDialog.value = true;
        } catch (error) {
            toast.add({ severity: 'error', summary: '載入表單發生錯誤', detail: error, life: 3000 });
            return;
        }
    } else if (buttonType == 'add') {
        editDialogHeader.value = '新增部門';

        await resetForm({ values: initialValues }); // 重置表單值並給予初始值初始化表單

        editDialog.value = true;
    } else if (buttonType == 'del') {
        if (!(gridSelectedRows.value.length > 0)) return;
        const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得 Grid 選中的行資料
        deleteDescription.value = `確定要刪除部門: ${currentGridRecord.DepartmentName} 嗎?`;
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
            const submitData = ref<FormCompositeData>({ ...values });
            await apiService.callApi(apiService.postApiWebDepartmentsAdd, submitData.value);

            // 更新列表
            gridApi.value.refreshServerSide({ route: [], purge: true }); // 當 purge: true 時，會清除現有資料並顯示 loading 狀態，直到新的資料從伺服器載入進來， false 僅更新當頁資料
            toast.add({ severity: 'success', summary: '新增成功', life: 3000 });
            // 修改 //////////////////////////////////////////////////////////////////////////////////////////////
        } else if (editMode.value == 'edit' && values.DepartmentId > 0) {
            const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得選中的行資料
            const submitData = ref<FormCompositeData>({ ...values });
            await apiService.callApi(apiService.postApiWebDepartmentsEdit, submitData.value);

            // 編輯成功後，從後端取得最新的資料，使用 gridApi: getRowNode->setData 刷新或更新當前行的資料，而不是重新載入整個列表
            const updatedRowData = await gridClient.getOneRowFromApi(apiService.postApiWebDepartmentsList, currentGridRecord.DepartmentId.toString(), 'DepartmentId');
            const rowNode = gridApi.value.getRowNode(currentGridRecord.DepartmentId.toString());
            if (rowNode) rowNode.setData(updatedRowData);

            toast.add({ severity: 'success', summary: '修改成功', life: 3000 });
        } else {
            throw new Error('選擇模式錯誤' + editMode.value);
        }

        editDialog.value = false; // 關閉編輯對話框
        gridApi.value.deselectAll(); // 取消選中行，避免取表格內的資料時取到舊資料
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
});

// 刪除 API，不過 yup 驗證
const deleteDialogSubmit = async () => {
    try {
        const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得選中的行資料
        await apiService.callApi(apiService.getApiWebDepartmentsDel, { DepartmentId: currentGridRecord.DepartmentId });

        // 刪除成功後，從後端取得最新的資料，使用 gridApi: refreshServerSide 刷新或更新整個列表
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
                    <Fieldset legend="部門設定">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">部門代號</span>
                                <InputText id="DepartmentName" v-model="DepartmentName"
                                    :invalid="!!errors?.['DepartmentName']" fluid />
                                <small class="text-red-500">{{ errors?.['DepartmentName'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">部門名(描述)</span>
                                <InputText id="DepartmentDescription" v-model="DepartmentDescription"
                                    :invalid="!!errors?.['DepartmentDescription']" fluid />
                                <small class="text-red-500">{{ errors?.['DepartmentDescription'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">部門層級</span>
                                <Select id="DepartmentLevel" v-model="DepartmentLevel" :options="departmentLevelOptions"
                                    :invalid="!!errors?.['DepartmentLevel']" optionLabel="Description"
                                    optionValue="Code" placeholder="請選擇" class="w-full" filter />
                                <small class="text-red-500">{{ errors?.['DepartmentLevel'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">上級部門</span>
                                <Select id="ParentDepartmentId" v-model="ParentDepartmentId"
                                    :options="[{ DepartmentId: 0, DepartmentName: '無上層', DepartmentDescription: '最頂層' }, ...departmentsList]"
                                    :invalid="!!errors?.['ParentDepartmentId']"
                                    :optionLabel="(item) => `${item.DepartmentName} (${item.DepartmentDescription})`"
                                    optionValue="DepartmentId" placeholder="請選擇" class="w-full" filter />
                                <small class="text-red-500">{{ errors?.['ParentDepartmentId'] }}</small>
                            </div>
                        </div>
                    </Fieldset>
                    <Fieldset legend="部門主管">
                        <div>
                            <span class="block font-bold mb-3">選擇主管，請注意先後順序，如第一位為正主管，第二位為副主管...</span>
                            <MultiSelect v-model="Managers" display="chip" :options="usersList"
                                :optionLabel="(item) => `${item.Username} (${item.UserDescription})`"
                                optionValue="UserId" filter placeholder="選擇主管" class="w-full" />
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
