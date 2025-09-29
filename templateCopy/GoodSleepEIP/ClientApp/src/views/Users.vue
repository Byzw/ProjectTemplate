<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 使用者設定頁面
// ModuleName: Users
// Backend: UsersManagerController.cs
// Database: Users, Groups, Roles, Departments
// Author: Keng-hua Ku
// Version: 20250205, 1.0
// Memo: 關聯太多後端呼叫模組，建議直接開對應管理員權限群組
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
export interface FormCompositeData extends models.UsersDTO { }

// ✅ 2. 定義表單的[初始值](比如新增資料)，使用 initialValues 來初始化表單
// 📑元素初始值 (比如新增資料)，使用 initialValues 來初始化表單，
// 📑表單內[有的務必設定]，否則不會依此值初始化，會造成之前資料還在
const initialValues: FormCompositeData = {
    UserId: 0,
    Username: '',
    Password: '',
    UserDescription: '',
    DepartmentId: 0,
    UserEmail: '',
    LineUserId: '',
    CreationTime: new Date().toISOString(),
    UpdateTime: new Date().toISOString(),
    Groups: [],
    Roles: [],
}

// ✅ 3. 定義表單[驗證]規則，用 yup
const validationSchema = yup.object({
    Username: yup.string().required('使用者名稱為必填欄位').matches(/^[a-zA-Z0-9]+$/, '只能包含英文字母與數字').min(3, '使用者名稱至少 3 個字元').max(50, '使用者名稱最多 50 個字元'),
    UserDescription: yup.string().required('使用者描述為必填欄位').min(3, '使用者描述至少 3 個字元').max(50, '使用者描述最多 50 個字元'),
    DepartmentId: yup.number().required('部門為必填欄位').min(1, '部門為必填欄位'),
    UserEmail: yup.string().email('請輸入正確的 Email 格式').max(50, 'Email 最多 50 個字元').nullable(),
    Password: yup.string().when([], () => { // []代表不依賴表單值，而是外部變數，這裡是 editMode
        return editMode.value === 'add'
            ? yup.string().required('請輸入密碼').min(6, '密碼最少為 6 碼').max(20, '密碼最長為 20 碼')
            : yup.string().notRequired().test('min-length', '密碼最少為 6 碼', (value) => {
                return !value || value.length >= 6; // 允許空值，但如果輸入了，就至少 6 碼
            }).max(20, '密碼最長為 20 碼');
    })
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
const [Username] = defineField('Username');
const [Password] = defineField('Password');
const [UserDescription] = defineField('UserDescription');
const [DepartmentId] = defineField('DepartmentId');
const [UserEmail] = defineField('UserEmail');
const [LineUserId] = defineField('LineUserId');
const [Groups] = defineField('Groups');
const [Roles] = defineField('Roles');

// ✨✨✨✨✨✨✨✨✨✨✨✨ Grid(AG-Grid) 定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// ✅ Grid Column 定義
const gridColumnDefs = ref([
    { headerName: '帳號', field: 'Username', sortable: true, filter: 'agTextColumnFilter', sort: 'asc', sortIndex: 0 },
    { headerName: '帳號名稱(說明)', field: 'UserDescription', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '部門', field: 'DepartmentName', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: 'Email', field: 'UserEmail', sortable: true, filter: 'agTextColumnFilter' },
    { headerName: '建立時間', field: 'CreationTime', sortable: true, filter: 'agTextColumnFilter', width: 230, valueFormatter: gridFormatter.dateTimeFormatter },
    { headerName: '更新時間', field: 'UpdateTime', sortable: true, filter: 'agTextColumnFilter', width: 230, valueFormatter: gridFormatter.dateTimeFormatter }
]);

// ✅ 解構賦值(Destructuring Assignment) 來自 Composables 封裝好的邏輯與定義
const {
    gridApi: gridApi,
    gridSelectedRows: gridSelectedRows,
    gridOptions: gridOptions,
    onGridReady: onGridReady,
    onSelectionChanged: onSelectionChanged
} = useGridOptions(apiService.postApiWebUsersList, gridColumnDefs.value, 'UserId', toast);

// ✅ 上述 gridOptions 如需調整，在此複寫
//gridOptions.value.rowSelection.mode = 'multiRow'; // 多選
//gridOptions.value.rowSelection.checkboxes = true; // 顯示 checkbox
//gridOptions.value.multiSortKey = ''; // 清空為關閉多重排序，'ctrl'為按下 Ctrl 鍵或 Cmd 鍵才觸發，alwaysMultiSort = true 為不按鍵即可觸發多重排序
gridOptions.value.autoSizeStrategy.type = ''; // 不自動調整欄寬
// 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚 END. Grid(AG-Grid) 定義區 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚

// ✨✨✨✨✨✨✨✨✨✨✨✨ 變數(資料等)區 ✨✨✨✨✨✨✨✨✨✨✨✨
const parameterList = ref<models.Parameter[]>([]);
const departmentsList = ref<models.Users[]>([]);
const groupsList = ref<models.Groups[]>([]);
const rolesList = ref<models.Roles[]>([]);
const isPasswordChanged = ref(false);   // 記錄使用者是否修改過密碼

// ✨✨✨✨✨✨✨✨✨✨✨✨ [監聽](watch)變數方法區 ✨✨✨✨✨✨✨✨✨✨✨✨

// ✨✨✨✨✨✨✨✨✨✨✨✨ [計算](computed)方法區 ✨✨✨✨✨✨✨✨✨✨✨✨

// ✨✨✨✨✨✨✨✨✨✨✨✨ [參數取得]區與功能呼叫區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 各類參數一次取得
async function fetchCollection(): Promise<void> {
    try {
        parameterList.value = await apiService.callApi(apiService.getApiWebGetPrefixParameters, { CategoryPrefix: 'Users' });
        groupsList.value = await apiService.callApi(apiService.getApiWebFetchGroupsList);
        rolesList.value = await apiService.callApi(apiService.getApiWebFetchRolesList);
        departmentsList.value = await apiService.callApi(apiService.getApiWebFetchDepartmentsList);
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
            if (currentGridRecord.UserId == 0) throw new Error('無法取得選擇資料');

            const fetchRecordData: models.Users = await apiService.callApi(apiService.getApiWebFetchUsersRecord, { UserId: currentGridRecord.UserId });

            // 取得此使用者關聯的所有 Groups, Roles
            const userGroups: models.UserGroups[] = await apiService.callApi(apiService.getApiWebFetchUserGroupsByUserId, { UserId: currentGridRecord.UserId });
            const userRoles: models.UserRoles[] = await apiService.callApi(apiService.getApiWebFetchUserRolesByUserId, { UserId: currentGridRecord.UserId });
            await resetForm({
                values: {
                    ...fetchRecordData,
                    Groups: userGroups.map(group => group.GroupId),
                    Roles: userRoles.map(role => role.RoleId)
                }
            });

            editDialogHeader.value = `修改使用者: ${currentGridRecord.Username}`;
            editDialog.value = true;
        } catch (error) {
            toast.add({ severity: 'error', summary: '載入表單發生錯誤', detail: error, life: 3000 });
            return;
        }
    } else if (buttonType == 'add') {
        editDialogHeader.value = '新增使用者';

        await resetForm({ values: initialValues }); // 重置表單值並給予初始值初始化表單

        editDialog.value = true;
    } else if (buttonType == 'del') {
        if (!(gridSelectedRows.value.length > 0)) return;
        const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得 Grid 選中的行資料
        deleteDescription.value = `確定要刪除使用者: ${currentGridRecord.UserDescription} 嗎?`;
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
            await apiService.callApi(apiService.postApiWebUsersAdd, submitData.value);

            // 更新列表
            gridApi.value.refreshServerSide({ route: [], purge: true }); // 當 purge: true 時，會清除現有資料並顯示 loading 狀態，直到新的資料從伺服器載入進來， false 僅更新當頁資料
            toast.add({ severity: 'success', summary: '新增成功', life: 3000 });
            // 修改 //////////////////////////////////////////////////////////////////////////////////////////////
        } else if (editMode.value == 'edit' && values.UserId > 0) {
            const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得選中的行資料
            const submitData = ref<FormCompositeData>({ ...values });
            await apiService.callApi(apiService.postApiWebUsersEdit, submitData.value);

            // 編輯成功後，從後端取得最新的資料，使用 gridApi: getRowNode->setData 刷新或更新當前行的資料，而不是重新載入整個列表
            const updatedRowData = await gridClient.getOneRowFromApi(apiService.postApiWebUsersList, currentGridRecord.UserId.toString(), 'UserId');
            const rowNode = gridApi.value.getRowNode(currentGridRecord.UserId.toString());
            if (rowNode) rowNode.setData(updatedRowData);

            toast.add({ severity: 'success', summary: '修改成功', life: 3000 });
        } else if (editMode.value == 'del') {
            const currentGridRecord: FormCompositeData = gridSelectedRows.value[0]; // 取得選中的行資料
            await apiService.callApi(apiService.getApiWebUsersDel, { UserId: currentGridRecord.UserId });

            // 刪除成功後，從後端取得最新的資料，使用 gridApi: refreshServerSide 刷新或更新整個列表
            gridApi.value.refreshServerSide({ route: [], purge: false }); // purge false 只更新當頁資料

            toast.add({ severity: 'success', summary: '刪除成功', life: 3000 });
            deleteDialog.value = false;
        } else {
            throw new Error('選擇模式錯誤' + editMode.value);
        }

        editDialog.value = false; // 關閉編輯對話框
        gridApi.value.deselectAll(); // 取消選中行，避免取表格內的資料時取到舊資料
    } catch (error) {
        toast.add({ severity: 'error', summary: '失敗', detail: error, life: 3000 });
    }
});

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
                    <Fieldset legend="使用者設定">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">帳號</span>
                                <InputText id="Username" v-model="Username" :invalid="!!errors?.['Username']" fluid
                                    :disabled="editMode === 'edit'" />
                                <small class="text-red-500">{{ errors?.['Username'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">密碼 {{ editMode == 'edit' ? '(填寫則重設)' : '' }}</span>
                                <InputText id="Password" v-model="Password" :invalid="!!errors?.['Password']"
                                    placeholder="********" type="text" fluid @input="isPasswordChanged = true" />
                                <small class="text-red-500">{{ errors?.['Password'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">帳號名稱 (說明)</span>
                                <InputText id="UserDescription" v-model="UserDescription"
                                    :invalid="!!errors?.['UserDescription']" fluid />
                                <small class="text-red-500">{{ errors?.['UserDescription'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">Email</span>
                                <InputText id="UserEmail" v-model="UserEmail" :invalid="!!errors?.['UserEmail']"
                                    fluid />
                                <small class="text-red-500">{{ errors?.['UserEmail'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">所屬部門</span>
                                <Select id="DepartmentId" v-model="DepartmentId" :options="departmentsList"
                                    :invalid="!!errors?.['DepartmentId']"
                                    :optionLabel="(item) => `${item.DepartmentName} (${item.DepartmentDescription})`"
                                    optionValue="DepartmentId" placeholder="請選擇" class="w-full" filter />
                                <small class="text-red-500">{{ errors?.['DepartmentId'] }}</small>
                            </div>
                        </div>
                    </Fieldset>
                    <Fieldset legend="所屬群組">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-2">
                            <div v-for="group in groupsList" :key="group.GroupId" class="flex items-center gap-2">
                                <Checkbox v-model="Groups" :value="group.GroupId" name="Groups"
                                    :inputId="'Groups-' + group.GroupId" />
                                <label :for="'Groups-' + group.GroupId">{{ `${group.GroupName}` }}</label>
                            </div>
                        </div>
                    </Fieldset>
                    <Fieldset legend="所屬角色">
                        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-2">
                            <div v-for="role in rolesList" :key="role.RoleId" class="flex items-center gap-2">
                                <Checkbox v-model="Roles" :value="role.RoleId" name="Roles"
                                    :inputId="'Roles-' + role.RoleId" />
                                <label :for="'Roles-' + role.RoleId">{{ `${role.RoleName}` }}</label>
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
                <Button label="是" icon="pi pi-check" @click="dialogSubmit" />
            </template>
        </Dialog>
    </div>
</template>
