<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 權限控管-角色設定頁面
// ModuleName: PermissionRelationships
// Backend: RolesController.cs
// Database: Roles, RolePermissions, Groups, GroupPermissions, Permissions
// Author: Keng-hua Ku
// Version: 20250205, 1.0
// Memo: demo類型 - 載入兩套 grid、功能按鈕在 grid rows 裡面
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️

import GridActionCell from '@/components/grid/PermissionRelationshipsActionCell.vue';
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
export interface FormCompositeDataRoles extends models.Roles {
    rolePermissions: number[];
}
export interface FormCompositeDataGroups extends models.Groups {
    groupPermissions: number[];
}

// ✅ 2. 定義表單的[初始值](比如新增資料)，使用 initialValues 來初始化表單
// 📑元素初始值 (比如新增資料)，使用 initialValues 來初始化表單，
// 📑表單內[有的務必設定]，否則不會依此值初始化，會造成之前資料還在
const initialValuesRoles: FormCompositeDataRoles = {
    RoleId: 0,
    RoleName: '',
    RoleDescription: '',
    IsAdmin: false,
    IsSystemReserved: false,
    CreationTime: new Date().toISOString(),
    rolePermissions: []
};
const initialValuesGroups: FormCompositeDataGroups = {
    GroupId: 0,
    GroupName: '',
    GroupDescription: '',
    PermissionGroupType: '',
    IsAdmin: false,
    IsSystemReserved: false,
    CreationTime: new Date().toISOString(),
    groupPermissions: []
};

// ✅ 3. 定義表單[驗證]規則，用 yup
const validationSchemaRoles = yup.object({
    RoleName: yup.string().max(40, '打太長啦').min(2, '角色名稱太短').required('尚未輸入角色名稱'),
    RoleDescription: yup.string().max(200, '打太長啦').nullable(),
});
const validationSchemaGroups = yup.object({
    GroupName: yup.string().max(40, '打太長啦').min(2, '群組名稱太短').required('尚未輸入角色名稱'),
    GroupDescription: yup.string().max(200, '打太長啦').nullable(),
    PermissionGroupType: yup.string().required('請選擇')
});

// ✅ 4. 用上述初始值與驗證規則，初始化表單，解構賦值(Destructuring Assignment) handleSubmit, errors...等屬性
const {
    handleSubmit: handleSubmitRoles,
    errors: errorsRoles,
    values: valuesRoles,
    resetForm: resetFormRoles,
    defineField: defineFieldRoles
} = useForm<FormCompositeDataRoles>({
    validationSchema: validationSchemaRoles,
    initialValues: initialValuesRoles
});
const {
    handleSubmit: handleSubmitGroups,
    errors: errorsGroups,
    values: valuesGroups,
    resetForm: resetFormGroups,
    defineField: defineFieldGroups
} = useForm<FormCompositeDataGroups>({
    validationSchema: validationSchemaGroups,
    initialValues: initialValuesGroups
});

// ✅ 5. <template>元素值綁定，使用 defineField 來綁定表單元素的值
// 📑 defineField() 的第二個返回值是表單元素的事件與屬性，比如: onInput/onChange...value, checked, disabled, readonly...目前用不到，可以自行參閱 VeeValidate 文件
const [RoleName] = defineFieldRoles('RoleName');
const [RoleDescription] = defineFieldRoles('RoleDescription');
const [IsAdminRoles] = defineFieldRoles('IsAdmin');
const [rolePermissions] = defineFieldRoles('rolePermissions');

const [GroupName] = defineFieldGroups('GroupName');
const [GroupDescription] = defineFieldGroups('GroupDescription');
const [PermissionGroupType] = defineFieldGroups('PermissionGroupType');
const [IsAdminGroups] = defineFieldGroups('IsAdmin');
const [groupPermissions] = defineFieldGroups('groupPermissions');

// ✨✨✨✨✨✨✨✨✨✨✨✨ Grid(AG-Grid) 定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ cellRendererParams 呼叫，必須先定義在前面
const onGridRowButtonClickCallBack = async (buttonType: string, sourceAction: string, rowData: FormCompositeDataRoles | FormCompositeDataGroups) => {
    onGridRowButtonClickAction(buttonType, sourceAction, rowData);
};

// ✅ Grid Column 定義
const gridColumnDefsRoles = ref([
    { headerName: '角色名稱', field: 'RoleName', sortable: true, filter: 'agTextColumnFilter', sort: 'asc', cellRenderer: (params) => gridFormatter.lockIconRenderer(params, params.data?.IsSystemReserved) },
    { headerName: '角色描述說明', field: 'RoleDescription', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '管理權限', field: 'IsAdmin', sortable: true, filter: 'agTextColumnFilter', cellRenderer: gridFormatter.booleanCheckIconRenderer, cellStyle: { 'text-align': 'center' } },
    { headerName: '功能', filter: false, width: 160, cellRenderer: GridActionCell, cellRendererParams: { sourceAction: 'roles', onGridRowButtonClickCallBack } }
]);
const gridColumnDefsGroups = ref([
    { headerName: '群組名稱', field: 'GroupName', sortable: true, filter: 'agTextColumnFilter', sort: 'asc', sortIndex: 1, cellRenderer: (params) => gridFormatter.lockIconRenderer(params, params.data?.IsSystemReserved) },
    { headerName: '群組類型', field: 'PermissionGroupType', sortable: true, filter: 'agTextColumnFilter', sort: 'asc', sortIndex: 0 },
    { headerName: '群組描述說明', field: 'GroupDescription', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '管理權限', field: 'IsAdmin', sortable: true, filter: 'agTextColumnFilter', cellRenderer: gridFormatter.booleanCheckIconRenderer, cellStyle: { 'text-align': 'center' } },
    { headerName: '功能', filter: false, width: 160, cellRenderer: GridActionCell, cellRendererParams: { sourceAction: 'groups', onGridRowButtonClickCallBack } }
]);

// ✅ 解構賦值(Destructuring Assignment) 來自 Composables 封裝好的邏輯與定義
const {
    gridApi: gridApiRoles,
    gridSelectedRows: gridSelectedRowsRoles,
    gridOptions: gridOptionsRoles,
    onGridReady: onGridReadyRoles,
    onSelectionChanged: onSelectionChangedRoles
} = useGridOptions(apiService.postApiWebRolesList, gridColumnDefsRoles.value, 'RoleId', toast);
const {
    gridApi: gridApiGroups,
    gridSelectedRows: gridSelectedRowsGroups,
    gridOptions: gridOptionsGroups,
    onGridReady: onGridReadyGroups,
    onSelectionChanged: onSelectionChangedGroups
} = useGridOptions(apiService.postApiWebGroupsList, gridColumnDefsGroups.value, 'GroupId', toast);

// ✅ 上述 gridOptions 如需調整，在此複寫
//gridOptions.value.rowSelection.mode = 'multiRow'; // 多選
//gridOptions.value.rowSelection.checkboxes = true; // 顯示 checkbox
//gridOptions.value.multiSortKey = ''; // 清空為關閉多重排序，'ctrl'為按下 Ctrl 鍵或 Cmd 鍵才觸發，alwaysMultiSort = true 為不按鍵即可觸發多重排序
//gridOptionsRoles.value.autoSizeStrategy.type = ''; // 自動調整欄寬
// 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚 END. Grid(AG-Grid) 定義區 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚

// ✨✨✨✨✨✨✨✨✨✨✨✨ 變數(資料等)區 ✨✨✨✨✨✨✨✨✨✨✨✨
const parameterList = ref<models.Parameter[]>([]);
const permissionsList = ref<models.Permissions[]>([]);

// ✨✨✨✨✨✨✨✨✨✨✨✨ [監聽](watch)變數方法區 ✨✨✨✨✨✨✨✨✨✨✨✨

// ✨✨✨✨✨✨✨✨✨✨✨✨ [計算](computed)方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
const permissionGroupTypeOptions = computed(() => {
    return parameterList.value.filter((p) => p.Category === 'PermissionGroupType');
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ [參數取得]區與功能呼叫區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 各類參數一次取得
async function fetchCollection(): Promise<void> {
    try {
        parameterList.value = await apiService.callApi(apiService.getApiWebGetPrefixParameters, { CategoryPrefix: 'Permission' });
        permissionsList.value = await apiService.callApi(apiService.getApiWebFetchPermissionsList, {});
    } catch (error) {
        throw error;
    }
}

// ✨✨✨✨✨✨✨✨✨✨✨✨ 各類[對話框]操作相關區 ✨✨✨✨✨✨✨✨✨✨✨✨
const editDialogRoles = ref(false); // 角色編輯對話框開關
const editDialogRolesHeader = ref(''); // 編輯對話框的標題: 新增 或 修改
const editDialogGroups = ref(false); // 群組編輯對話框開關
const editDialogGroupsHeader = ref(''); // 編輯對話框的標題: 新增 或 修改

const deleteDialogRoles = ref(false); // 角色刪除對話框開關
const deleteDialogGroups = ref(false); // 群組刪除對話框開關
const deleteDescription = ref(''); // 刪除對話框的描述，角色/群組 內容差不多就共用吧

const editMode = ref(''); // 編輯模式: 新增(add) 或 修改(edit) 或 刪除(del)

// 打開對話框、綁定資料
const onGridRowButtonClickAction = async (buttonType: string, sourceAction: string, rowData: FormCompositeDataRoles | FormCompositeDataGroups) => {
    editMode.value = buttonType;

    // 角色修改
    if (buttonType == 'edit' && sourceAction == 'roles' && "RoleId" in rowData && rowData.RoleId > 0) {
        try {
            // 取得此角色所關聯的全部 PermissionId
            rolePermissions.value = await apiService.callApi(apiService.getApiWebFetchRolePermissions, { RoleId: rowData.RoleId });
            await resetFormRoles({ values: { ...rowData, rolePermissions: rolePermissions.value } });
        } catch (error) {
            toast.add({ severity: 'error', summary: '載入表單發生錯誤', detail: error, life: 3000 });
            return;
        }
        editDialogRolesHeader.value = `修改角色: ${rowData.RoleName}`;
        editDialogRoles.value = true;
        // 群組修改
    } else if (buttonType == 'edit' && sourceAction == 'groups' && "GroupId" in rowData && rowData.GroupId > 0) {
        try {
            // 取得此群組所關聯的全部 PermissionId
            groupPermissions.value = await apiService.callApi(apiService.getApiWebFetchGroupPermissions, { GroupId: rowData.GroupId });
            // 因為 grid 裡面 PermissionGroupType 原始資料已經被轉換成參數的 Description，格式已被改變，所以要重新取得原始資料才能對應
            const fetchRecordData: models.Groups = await apiService.callApi(apiService.getApiWebFetchGroupsRecord, { GroupId: rowData.GroupId });
            await resetFormGroups({ values: { ...fetchRecordData, groupPermissions: groupPermissions.value } });
        } catch (error) {
            toast.add({ severity: 'error', summary: '載入表單發生錯誤', detail: error, life: 3000 });
            return;
        }
        editDialogGroupsHeader.value = `修改群組: ${rowData.GroupName}`;
        editDialogGroups.value = true;
        // 角色刪除
    } else if (buttonType == 'del' && sourceAction == 'roles' && "RoleId" in rowData && rowData.RoleId > 0) {
        if (rowData.IsSystemReserved) {
            toast.add({ severity: 'warn', summary: '不行喔', detail: '此筆角色為系統保留，無法刪除', life: 3000 });
            return;
        }
        deleteDescription.value = `確定要刪除角色: ${rowData.RoleName} 嗎?`;
        deleteDialogRoles.value = true;
        // 群組刪除
    } else if (buttonType == 'del' && sourceAction == 'groups' && "GroupId" in rowData && rowData.GroupId > 0) {
        if (rowData.IsSystemReserved) {
            toast.add({ severity: 'warn', summary: '不行喔', detail: '此筆群組為系統保留，無法刪除', life: 3000 });
            return;
        }
        deleteDescription.value = `確定要刪除群組: ${rowData.GroupName} 嗎?`;
        deleteDialogGroups.value = true;
    } else {
        console.warn(`無法辨識的 Action 類型，buttonType: ${buttonType}, sourceAction: ${sourceAction}`, '資料:', rowData);
    }
};
const onAddClickRoles = async () => {
    editMode.value = 'add';
    editDialogRolesHeader.value = '新增角色';

    await resetFormRoles({ values: initialValuesRoles }); // 重置表單值並給予初始值初始化表單

    editDialogRoles.value = true;
};
const onAddClickGroups = async () => {
    editMode.value = 'add';
    editDialogGroupsHeader.value = '新增群組';

    await resetFormGroups({ values: initialValuesGroups }); // 重置表單值並給予初始值初始化表單

    editDialogGroups.value = true;
}

// 表單提交區 ////////////////////////////////////////////////////////////////////////////////////////////////
const dialogSubmitRoles = handleSubmitRoles(async (values: FormCompositeDataRoles) => {
    try {
        // 新增 //////////////////////////////////////////////////////////////////////////////////////////////
        if (editMode.value == 'add') {
            const submitData = ref<FormCompositeDataRoles>({ ...values });
            await apiService.callApi(apiService.postApiWebRolesAdd, submitData.value);

            // 更新列表
            gridApiRoles.value.refreshServerSide({ route: [], purge: true }); // 當 purge: true 時，會清除現有資料並顯示 loading 狀態，直到新的資料從伺服器載入進來， false 僅更新當頁資料
            toast.add({ severity: 'success', summary: '新增成功', life: 3000 });
            // 修改 //////////////////////////////////////////////////////////////////////////////////////////////
        } else if (editMode.value == 'edit' && values.RoleId > 0) {
            const currentGridRecord: FormCompositeDataRoles = gridSelectedRowsRoles.value[0]; // 取得選中的行資料
            const submitData = ref<FormCompositeDataRoles>({ ...values });
            await apiService.callApi(apiService.postApiWebRolesEdit, submitData.value);

            // 編輯成功後，從後端取得最新的資料，使用 gridApi: getRowNode->setData 刷新或更新當前行的資料，而不是重新載入整個列表
            const updatedRowData = await gridClient.getOneRowFromApi(apiService.postApiWebRolesList, currentGridRecord.RoleId.toString(), 'RoleId');
            const rowNode = gridApiRoles.value.getRowNode(currentGridRecord.RoleId.toString());
            if (rowNode) rowNode.setData(updatedRowData);

            toast.add({ severity: 'success', summary: '修改成功', life: 3000 });
        } else {
            throw new Error('編輯模式錯誤' + editMode.value);
        }

        editDialogRoles.value = false; // 關閉編輯對話框
        gridApiRoles.value.deselectAll(); // 取消選中行，避免取表格內的資料時取到舊資料
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
});

const dialogSubmitGroups = handleSubmitGroups(async (values: FormCompositeDataGroups) => {
    try {
        // 新增 //////////////////////////////////////////////////////////////////////////////////////////////
        if (editMode.value == 'add') {
            const submitData = ref<FormCompositeDataGroups>({ ...values });
            await apiService.callApi(apiService.postApiWebGroupsAdd, submitData.value);

            // 更新列表
            gridApiGroups.value.refreshServerSide({ route: [], purge: true }); // 當 purge: true 時，會清除現有資料並顯示 loading 狀態，直到新的資料從伺服器載入進來， false 僅更新當頁資料
            toast.add({ severity: 'success', summary: '新增成功', life: 3000 });
            // 修改 //////////////////////////////////////////////////////////////////////////////////////////////
        } else if (editMode.value == 'edit' && values.GroupId > 0) {
            const currentGridRecord: FormCompositeDataGroups = gridSelectedRowsGroups.value[0]; // 取得選中的行資料
            const submitData = ref<FormCompositeDataGroups>({ ...values });
            await apiService.callApi(apiService.postApiWebGroupsEdit, submitData.value);

            // 編輯成功後，從後端取得最新的資料，使用 gridApi: getRowNode->setData 刷新或更新當前行的資料，而不是重新載入整個列表
            const updatedRowData = await gridClient.getOneRowFromApi(apiService.postApiWebGroupsList, currentGridRecord.GroupId.toString(), 'GroupId');
            const rowNode = gridApiGroups.value.getRowNode(currentGridRecord.GroupId.toString());
            if (rowNode) rowNode.setData(updatedRowData);

            toast.add({ severity: 'success', summary: '修改成功', life: 3000 });
        } else {
            throw new Error('編輯模式錯誤' + editMode.value);
        }

        editDialogGroups.value = false; // 關閉編輯對話框
        gridApiGroups.value.deselectAll(); // 取消選中行，避免取表格內的資料時取到舊資料
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
});

const deleteSubmitRoles = async () => {
    try {
        const currentGridRecord: FormCompositeDataRoles = gridSelectedRowsRoles.value[0];
        await apiService.callApi(apiService.getApiWebRolesDel, { RoleId: currentGridRecord.RoleId });

        gridApiRoles.value.refreshServerSide({ route: [], purge: false }); // purge false 只更新當頁資料

        toast.add({ severity: 'success', summary: '刪除成功', life: 3000 });
        deleteDialogRoles.value = false;
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
};

const deleteSubmitGroups = async () => {
    try {
        const currentGridRecord: FormCompositeDataGroups = gridSelectedRowsGroups.value[0];
        await apiService.callApi(apiService.getApiWebGroupsDel, { GroupId: currentGridRecord.GroupId });

        gridApiGroups.value.refreshServerSide({ route: [], purge: false }); // purge false 只更新當頁資料

        toast.add({ severity: 'success', summary: '刪除成功', life: 3000 });
        deleteDialogGroups.value = false;
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
};
</script>

<template>
    <div>
        <div class="card">

            <div class="grid grid-cols-1 2xl:grid-cols-2 gap-8 h-full">
                <!-- 左側 Roles Grid -->
                <div class="flex flex-col">
                    <h2 class="text-lg font-bold text-gray-700 dark:text-gray-300 mb-2">角色與所屬權限定義</h2>
                    <Toolbar class="mb-6 overflow-x-auto whitespace-nowrap">
                        <template #start>
                            <Button severity="secondary" class="mr-2" @click="onAddClickRoles()">
                                <span class="fa-solid fa-plus" data-pc-section="icon"></span>
                                <span class="hidden sm:inline" data-pc-section="label">新增角色</span>
                            </Button>
                        </template>
                    </Toolbar>
                    <ag-grid-vue class="ag-theme-quartz"
                        style="width: 100%; height: calc(100vh - 19.85rem); min-height: 400px"
                        :gridOptions="gridOptionsRoles" @gridReady="onGridReadyRoles"
                        @selection-changed="onSelectionChangedRoles">
                    </ag-grid-vue>
                </div>
                <!-- 右側 Groups Grid -->
                <div class="flex flex-col">
                    <h2 class="text-lg font-bold text-gray-700 dark:text-gray-300 mb-2">群組與所屬權限定義</h2>
                    <Toolbar class="mb-6 overflow-x-auto whitespace-nowrap">
                        <template #start>
                            <Button severity="secondary" class="mr-2" @click="onAddClickGroups()">
                                <span class="fa-solid fa-plus" data-pc-section="icon"></span>
                                <span class="hidden sm:inline" data-pc-section="label">新增群組</span>
                            </Button>
                        </template>
                    </Toolbar>
                    <ag-grid-vue class="ag-theme-quartz"
                        style="width: 100%; height: calc(100vh - 19.85rem); min-height: 400px"
                        :gridOptions="gridOptionsGroups" @gridReady="onGridReadyGroups"
                        @selection-changed="onSelectionChangedGroups"></ag-grid-vue>
                </div>
            </div>
        </div>

        <Dialog v-model:visible="editDialogRoles" class="w-full max-w-[95%] md:max-w-[80%] lg:max-w-[70%]"
            :header="editDialogRolesHeader" :modal="true">
            <form @submit.prevent="dialogSubmitRoles">
                <div class="space-y-4">
                    <Fieldset legend="角色辨識">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">角色名稱</span>
                                <InputText id="RoleName" v-model="RoleName" :invalid="!!errorsRoles?.['RoleName']"
                                    fluid />
                                <small class="text-red-500">{{ errorsRoles?.['RoleName'] }}</small>
                            </div>
                            <div class="col-span-3">
                                <span class="block font-bold mb-3">角色描述</span>
                                <InputText id="RoleDescription" v-model="RoleDescription"
                                    :invalid="!!errorsRoles?.['RoleDescription']" fluid />
                                <small class="text-red-500">{{ errorsRoles?.['RoleDescription'] }}</small>
                            </div>
                        </div>
                    </Fieldset>
                    <Fieldset legend="是否為管理權限">
                        <div class="grid mt-4">
                            <div class="flex items-center gap-2">
                                <ToggleSwitch inputId="IsAdminRoles" v-model="IsAdminRoles" />
                                <span v-if="IsAdminRoles">是，本角色所屬之所有人，會取得所有權限</span>
                                <span v-if="!IsAdminRoles">否，一般角色</span>
                            </div>
                        </div>
                    </Fieldset>
                    <Fieldset legend="模組權限設定">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-2">
                            <div v-for="permission in permissionsList" :key="permission.PermissionId"
                                class="flex items-center gap-2">
                                <Checkbox v-model="rolePermissions" :value="permission.PermissionId"
                                    name="rolePermissions" :inputId="'rolePermissions-' + permission.PermissionId" />
                                <label :for="'rolePermissions-' + permission.PermissionId">
                                    {{ `${permission.ModuleName} - ${permission.PermissionModuleSubType}` }}</label>
                            </div>
                        </div>
                    </Fieldset>
                    <div class="flex justify-end items-center gap-4 pt-6">
                        <i v-if="errorsRoles && Object.keys(errorsRoles).length > 0"
                            class="pi pi-exclamation-triangle !text-lg text-red-500">
                            請檢查輸入
                        </i>
                        <i v-if="isLoading" class="loading-spinner-inline"></i>
                        <Button label="取消" icon="pi pi-times" text @click="editDialogRoles = false" />
                        <Button label="存檔" icon="pi pi-check" outlined @click="dialogSubmitRoles" />
                    </div>
                </div>
            </form>
        </Dialog>
        <Dialog v-model:visible="editDialogGroups" class="w-full max-w-[95%] md:max-w-[80%] lg:max-w-[70%]"
            :header="editDialogGroupsHeader" :modal="true">
            <form @submit.prevent="dialogSubmitGroups">
                <div class="space-y-4">
                    <Fieldset legend="群組辨識">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">群組名稱</span>
                                <InputText id="GroupName" v-model="GroupName" :invalid="!!errorsGroups?.['GroupName']"
                                    fluid />
                                <small class="text-red-500">{{ errorsGroups?.['GroupName'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">模組次類別</span>
                                <Select id="PermissionGroupType" v-model="PermissionGroupType"
                                    :options="permissionGroupTypeOptions"
                                    :invalid="!!errorsGroups?.['PermissionGroupType']" optionLabel="Description"
                                    optionValue="Code" placeholder="請選擇" class="w-full" />
                                <small class="text-red-500">{{ errorsGroups?.['PermissionGroupType'] }}</small>
                            </div>
                            <div class="col-span-2">
                                <span class="block font-bold mb-3">群組描述</span>
                                <InputText id="GroupDescription" v-model="GroupDescription"
                                    :invalid="!!errorsGroups?.['GroupDescription']" fluid />
                                <small class="text-red-500">{{ errorsGroups?.['GroupDescription'] }}</small>
                            </div>
                        </div>
                    </Fieldset>
                    <Fieldset legend="是否為管理權限">
                        <div class="grid mt-4">
                            <div class="flex items-center gap-2">
                                <ToggleSwitch inputId="IsAdminGroups" v-model="IsAdminGroups" />
                                <span v-if="IsAdminGroups">是，本群組所屬之所有人，會取得所有權限</span>
                                <span v-if="!IsAdminGroups">否，一般群組</span>
                            </div>
                        </div>
                    </Fieldset>
                    <Fieldset legend="模組權限設定">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-2">
                            <div v-for="permission in permissionsList" :key="permission.PermissionId"
                                class="flex items-center gap-2">
                                <Checkbox v-model="groupPermissions" :value="permission.PermissionId"
                                    name="groupPermissions" :inputId="'groupPermissions-' + permission.PermissionId" />
                                <label :for="'groupPermissions-' + permission.PermissionId">
                                    {{ `${permission.ModuleName} - ${permission.PermissionModuleSubType}` }}
                                </label>
                            </div>
                        </div>
                    </Fieldset>
                    <div class="flex justify-end items-center gap-4 pt-6">
                        <i v-if="errorsGroups && Object.keys(errorsGroups).length > 0"
                            class="pi pi-exclamation-triangle !text-lg text-red-500">
                            請檢查輸入
                        </i>
                        <i v-if="isLoading" class="loading-spinner-inline"></i>
                        <Button label="取消" icon="pi pi-times" text @click="editDialogGroups = false" />
                        <Button label="存檔" icon="pi pi-check" outlined @click="dialogSubmitGroups" />
                    </div>
                </div>
            </form>
        </Dialog>

        <Dialog v-model:visible="deleteDialogRoles" :style="{ width: '450px' }" header="請小心" :modal="true">
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span v-if="deleteDescription"><b>{{ deleteDescription }}</b></span>
            </div>
            <template #footer>
                <Button label="否" icon="pi pi-times" text @click="deleteDialogRoles = false" />
                <Button label="是" icon="pi pi-check" @click="deleteSubmitRoles" />
            </template>
        </Dialog>
        <Dialog v-model:visible="deleteDialogGroups" :style="{ width: '450px' }" header="請小心" :modal="true">
            <div class="flex items-center gap-4">
                <i class="pi pi-exclamation-triangle !text-3xl" />
                <span v-if="deleteDescription"><b>{{ deleteDescription }}</b></span>
            </div>
            <template #footer>
                <Button label="否" icon="pi pi-times" text @click="deleteDialogGroups = false" />
                <Button label="是" icon="pi pi-check" @click="deleteSubmitGroups" />
            </template>
        </Dialog>
    </div>
</template>
