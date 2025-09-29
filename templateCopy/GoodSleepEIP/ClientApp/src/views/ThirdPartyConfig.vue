<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 第三方服務設定
// ModuleName: ThirdPartyConfig
// Backend: ThirdPartyConfigController.cs
// Database: Company, CompanyServiceIntegration, CompanyServiceParam, Parameter
// Author: Neil Lin
// Version: 20250618, 1.0
// Memo:
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️

import { useGridOptions } from '@/composables/useGrid';
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import * as gridClient from '@/service/gridClient';
import * as gridFormatter from '@/utils/gridFormatter';
import { useToast } from 'primevue/usetoast';
import { useForm } from 'vee-validate';
import { computed, onMounted, ref } from 'vue';
import * as yup from 'yup';
import ServiceParamInput from '@/components/service/ServiceParamInput.vue';

const toast = useToast();

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
export interface FormCompositeData extends models.CompanyServiceIntegrationDto { }

// ✅ 2. 定義表單的[初始值](比如新增資料)，使用 initialValues 來初始化表單
// 📑元素初始值 (比如新增資料)，使用 initialValues 來初始化表單，
// 📑表單內[有的務必設定]，否則不會依此值初始化，會造成之前資料還在
const initialValues: FormCompositeData = {
    Id: '00000000-0000-0000-0000-000000000000',
    CompanyId: '',
    ServiceType: '',
    EndpointName: '',
    EnvType: '',
    ApiBaseUrl: '',
    IsActive: true,
    CreationTime: new Date().toISOString(),
    UpdateTime: new Date().toISOString(),
    Params: []
};

// ✅ 3. 定義表單[驗證]規則，用 yup
const validationSchema = yup.object({
    CompanyId: yup.string().required('公司為必填'),
    EndpointName: yup.string().required('服務點名稱為必填'),
    ServiceType: yup.string().required('服務類型為必填'),
    EnvType: yup.string().required('環境別為必填'),
    Params: yup.array().of(
        yup.object().shape({
            ParamKey: yup.string().required('鍵為必填'),
            ParamValue: yup.string().required('值為必填')
        })
    ).min(1, '至少需要輸入一筆資料')
});

// ✅ 4. 用上述初始值與驗證規則，初始化表單，解構賦值(Destructuring Assignment) handleSubmit, errors...等屬性
const {
    handleSubmit,
    errors,
    values,
    resetForm,
    defineField
} = useForm<FormCompositeData>({
    validationSchema: validationSchema,
    initialValues: initialValues
});

// ✅ 5. <template>元素值綁定，使用 defineField 來綁定表單元素的值
// 📑 defineField() 的第二個返回值是表單元素的事件與屬性，比如: onInput/onChange...value, checked, disabled, readonly...目前用不到，可以自行參閱 VeeValidate 文件
const [Id] = defineField('Id');
const [CompanyId] = defineField('CompanyId');
const [ServiceType] = defineField('ServiceType');
const [EndpointName] = defineField('EndpointName');
const [EnvType] = defineField('EnvType');
const [ApiBaseUrl] = defineField('ApiBaseUrl');
const [IsActive] = defineField('IsActive');
const [Params] = defineField('Params');

// ✨✨✨✨✨✨✨✨✨✨✨✨ Grid(AG-Grid) 定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ Grid Column 定義
const gridColumnDefs = ref([
    { headerName: '公司', field: 'CompanyName', filter: 'agTextColumnFilter', sort: 'asc' },
    { headerName: '服務點名稱', field: 'EndpointName', filter: 'agTextColumnFilter' },
    { headerName: '服務類型', field: 'ServiceTypeDescription', filter: 'agTextColumnFilter' },
    { headerName: '環境', field: 'EnvTypeDescription', filter: 'agTextColumnFilter' },
    // { headerName: '啟用', field: 'IsActive', cellRenderer: gridFormatter.booleanCheckIconRenderer, filter: 'agSetColumnFilter', filterParams: { values: ['是', '否'] }, width: 100 },
    {
        headerName: "啟用", field: "IsActive", width: 120, filter: 'agSetColumnFilter', filterParams: {
            values: ["是", "否"],
        }, 
        cellRenderer: gridFormatter.booleanFormatter
    },
    { headerName: '更新時間', field: 'UpdateTime', filter: 'agTextColumnFilter', valueFormatter: gridFormatter.dateTimeFormatter },
]);

// ✅ 解構賦值(Destructuring Assignment) 來自 Composables 封裝好的邏輯與定義
const {
    gridApi,
    gridSelectedRows,
    gridOptions,
    onGridReady,
    onSelectionChanged
} = useGridOptions(apiService.postApiWebCompanyServiceIntegrationList, gridColumnDefs.value, 'Id', toast, true);


// ✅ 上述 gridOptions 如需調整，在此複寫
//gridOptions.value.rowSelection.mode = 'multiRow'; // 多選
//gridOptions.value.rowSelection.checkboxes = true; // 顯示 checkbox
//gridOptions.value.multiSortKey = ''; // 清空為關閉多重排序，'ctrl'為按下 Ctrl 鍵或 Cmd 鍵才觸發，alwaysMultiSort = true 為不按鍵即可觸發多重排序
//gridOptions.value.autoSizeStrategy.type = ''; // 不自動調整欄寬
// 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚 END. Grid(AG-Grid) 定義區 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚

// ✨✨✨✨✨✨✨✨✨✨✨✨ 變數(資料等)區 ✨✨✨✨✨✨✨✨✨✨✨✨
const parameterList = ref<models.Parameter[]>([]);
const companyOptions = ref<models.Company[]>([]);

// ✨✨✨✨✨✨✨✨✨✨✨✨ [計算](computed)方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
const serviceTypeOptions = computed(() => parameterList.value.filter(p => p.Category === 'ServiceType'));
const envTypeOptions = computed(() => parameterList.value.filter(p => p.Category === 'EnvType'));

// ✨✨✨✨✨✨✨✨✨✨✨✨ [參數取得]區與功能呼叫區 ✨✨✨✨✨✨✨✨✨✨✨✨
async function fetchCollection(): Promise<void> {
    try {
        parameterList.value = await apiService.callApi(apiService.postApiWebGetListParameters, ['ServiceType', 'EnvType']);
        companyOptions.value = await apiService.callApi(apiService.getApiWebGetCompanyList, {});
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

    if (buttonType === 'edit') {
        if (!(gridSelectedRows.value.length > 0)) return;

        try {
            const recordId = gridSelectedRows.value[0].Id;
            if (!recordId) throw new Error('無法獲取紀錄ID');
            const data = await apiService.callApi<models.CompanyServiceIntegrationDto>(apiService.getApiWebFetchCompanyServiceIntegration, { integrationId: recordId });
            resetForm({ values: data });
            if (!values.Params) {
                values.Params = [];
            }
            editDialogHeader.value = `修改服務整合設定: ${data.EndpointName}`;
            editDialog.value = true;
        } catch (error) {
            toast.add({ severity: 'error', summary: '載入失敗', detail: error, life: 3000 });
        }
    } else if (buttonType === 'add') {
        resetForm({ values: initialValues });
        editDialogHeader.value = '新增服務整合設定';
        editDialog.value = true;
    } else if (buttonType === 'del') {
        if (!(gridSelectedRows.value.length > 0)) return;
        const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得 Grid 選中的行資料
        deleteDescription.value = `確定要刪除服務整合設定: ${currentGridRecord.EndpointName} 嗎?`;
        deleteDialog.value = true;
    }
};

// 表單提交區 ////////////////////////////////////////////////////////////////////////////////////////////////
const dialogSubmit = handleSubmit(async (values: FormCompositeData) => {
    try {
        // 新增 //////////////////////////////////////////////////////////////////////////////////////////////
        if (editMode.value === 'add') {
            const submitData = ref<FormCompositeData>({ ...values });
            await apiService.callApi(apiService.postApiWebCompanyServiceIntegrationAdd, submitData.value);

            // 更新列表
            gridApi.value.refreshServerSide({ route: [], purge: true }); // 當 purge: true 時，會清除現有資料並顯示 loading 狀態，直到新的資料從伺服器載入進來， false 僅更新當頁資料
            toast.add({ severity: 'success', summary: '新增成功', life: 3000 });

        // 修改 //////////////////////////////////////////////////////////////////////////////////////////////
        } else if (editMode.value === 'edit') {
            const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得選中的行資料
            const submitData = ref<FormCompositeData>({ ...values });
            await apiService.callApi(apiService.postApiWebCompanyServiceIntegrationEdit, submitData.value);
            
            // 編輯成功後，從後端取得最新的資料，使用 gridApi: getRowNode->setData 刷新或更新當前行的資料，而不是重新載入整個列表
            if (currentGridRecord.Id) {
                const updatedRowData = await gridClient.getOneRowFromApi(apiService.postApiWebCompanyServiceIntegrationList, currentGridRecord.Id, 'Id');
                const rowNode = gridApi.value.getRowNode(currentGridRecord.Id);
                if (rowNode) rowNode.setData(updatedRowData);
            }

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
        await apiService.callApi(apiService.getApiWebCompanyServiceIntegrationDel, { integrationId: currentGridRecord.Id });

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
        </Toolbar>

        <ag-grid-vue
            class="ag-theme-quartz"
            style="width: 100%; height: calc(100vh - 17.6rem); min-height: 400px"
            :gridOptions="gridOptions"
            @gridReady="onGridReady"
            @rowDoubleClicked="onButtonClickAction('edit')"
            @selection-changed="onSelectionChanged"
        >
        </ag-grid-vue>
    </div>

    <Dialog v-model:visible="editDialog" class="w-full max-w-[95%] md:max-w-[80%] lg:max-w-[70%]" :header="editDialogHeader" :modal="true">
        <form @submit.prevent="dialogSubmit">
            <div class="space-y-4">
                <Fieldset legend="主要設定">
                    <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                        
                        <div class="col-span-1">
                            <span class="block font-bold mb-3">公司</span>
                            <Select id="CompanyId" v-model="CompanyId" :options="companyOptions"
                                :invalid="!!errors?.['CompanyId']" optionLabel="CompanyName"
                                optionValue="CompanyId" placeholder="請選擇" class="w-full" filter />
                            <small class="text-red-500">{{ errors?.['CompanyId'] }}</small>
                        </div>
                         <div class="col-span-1">
                            <span class="block font-bold mb-3">服務點名稱</span>
                            <InputText v-model="EndpointName" :invalid="!!errors?.['EndpointName']" class="w-full" />
                            <small class="text-red-500">{{ errors?.['EndpointName'] }}</small>
                        </div>
                        <div class="col-span-1">
                            <span class="block font-bold mb-3">服務類型</span>
                            <Select id="ServiceType" v-model="ServiceType" :options="serviceTypeOptions"
                                :invalid="!!errors?.['ServiceType']" optionLabel="Description"
                                optionValue="Code" placeholder="請選擇" class="w-full" filter />
                            <small class="text-red-500">{{ errors?.['ServiceType'] }}</small>
                        </div>
                        <div class="col-span-1">
                            <span class="block font-bold mb-3">環境別</span>
                            <Select id="EnvType" v-model="EnvType" :options="envTypeOptions"
                                :invalid="!!errors?.['EnvType']" optionLabel="Description"
                                optionValue="Code" placeholder="請選擇" class="w-full" filter />
                            <small class="text-red-500">{{ errors?.['EnvType'] }}</small>
                        </div>
                        <div class="col-span-2">
                            <span class="block font-bold mb-3">API Base URL</span>
                            <InputText v-model="ApiBaseUrl" class="w-full" />
                            <small class="text-red-500">{{ errors?.['ApiBaseUrl'] }}</small>
                        </div>
                        <div class="col-span-1">
                            <span class="block font-bold mb-3">是否啟用</span>
                            <div class="flex gap-4">
                                <div class="flex align-items-center">
                                    <RadioButton v-model="IsActive" :value="true" inputId="true" />
                                    <label for="true" class="ml-2">啟用</label>
                                </div>
                                <div class="flex align-items-center">
                                    <RadioButton v-model="IsActive" :value="false" inputId="false" />
                                    <label for="false" class="ml-2">停用</label>
                                </div>
                            </div>
                            <small class="text-red-500">{{ errors?.['IsActive'] }}</small>
                        </div>
                    </div>
                </Fieldset>
                <Fieldset legend="服務參數">
                    <div class="grid grid-cols-1 gap-4">
                        <div class="col-span-1">
                            <ServiceParamInput v-model="Params" v-if="Params" :toast="toast" />
                            <small class="text-red-500">{{ errors?.['Params'] }}</small>
                        </div>
                    </div>
                    
                </Fieldset>

                <div class="flex justify-end items-center gap-4 pt-6">
                    <i v-if="errors && Object.keys(errors).length > 0" class="pi pi-exclamation-triangle !text-lg text-red-500"> 請檢查輸入 </i>
                    <i v-if="isLoading" class="loading-spinner-inline"></i>
                    <Button label="取消" icon="pi pi-times" text @click="editDialog = false" />
                    <Button label="存檔" icon="pi pi-check" outlined type="submit" />
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
</template> 