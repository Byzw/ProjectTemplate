<script setup lang="ts">
import { useLayout } from '@/layout/composables/layout';
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { useThemeStore } from '@/stores/themeStore';
import * as utils from '@/utils/common';
import dayjs from 'dayjs';
import { useToast } from 'primevue/usetoast';
import { useForm } from 'vee-validate';
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import * as yup from 'yup';

const { onMenuToggle, onTopbarMenuToggle, isDarkTheme, toggleDarkMode, layoutConfig } = useLayout();
const router = useRouter();
const authStore = useAuthStore();
const themeStore = useThemeStore();
const toast = useToast();

let intervalId: ReturnType<typeof setInterval>;
onMounted(async () => {
    try {
        GetAllNotifications(); // 首次執行一次
        intervalId = setInterval(GetAllNotifications, 10000); // 每 10 秒檢查更新一次
    } catch (error) {
        toast.add({ severity: 'error', summary: '錯誤', detail: error, life: 5000 });
    }
});
onBeforeUnmount(() => {
    clearInterval(intervalId); // 清理定時器，在組件卸載時停止定時調用
});

// 通知中心 /////////////////////////////////////////////////////////////////////////////////////////////////
// 切換通知面板
const notificationPopover = ref();
const toggleNotification = (event: Event) => {
    if (notificationPopover.value) {
        notificationPopover.value.toggle(event);
    }
};

// 訊息點選，依照訊息內容決定處理方式
async function notificationClick(notification: models.Notifications): Promise<void> {
    // 1. 如果 ProductionPercentage 不為 100，則不做反應
    if (notification.ProductionPercentage < 100) {
        toast.add({ severity: 'info', summary: '還沒好喔', detail: '還在處理中，請耐心等待', life: 1000 });
        return;
    }
    // 如果 NotificationErrorMessageContent 有資料代表錯誤發生，不做反應
    if (!utils.isNullOrEmpty(notification.NotificationErrorMessageContent)) return;

    // 2. 更新 ReadTime，並傳送至後端，公用通知無法標記已讀不更新
    if (notification.UserId !== null) {
        try {
            await apiService.callApi(apiService.getApiWebMarkNotificationRead, { NotificationId: notification.NotificationId });
            notification.ReadTime = new Date().toISOString(); // 本地更新
        } catch (error) {
            toast.add({ severity: 'error', summary: '錯誤', detail: `通知存取錯誤，${error}`, life: 5000 });
        }
    }

    // 3. 如果 NotificationLink 不為 null，根據:
    //    IsLinkNewWindow, IsInternalLink, IsBlob 決定如何開啟
    if (notification.NotificationLink) {
        if (notification.IsInternalLink) {
            // 內部，路由
            if (notification.IsBlob) {
                try {
                    await apiService.downloadFileWithApi(notification.NotificationLink, {}, 'get');
                    toast.add({ severity: 'success', summary: '下載檔案', detail: '檔案已開始下載', life: 5000 });
                } catch (error) {
                    toast.add({ severity: 'error', summary: '錯誤', detail: `無法下載檔案，${error}`, life: 5000 });
                }
            } else {
                if (notification.IsLinkNewWindow) window.open(notification.NotificationLink, '_blank');
                else router.push(notification.NotificationLink);
            }
        } else {
            // 外部，連結，不管(不支援) IsBlob 類型
            if (notification.IsLinkNewWindow)
                window.open(notification.NotificationLink, '_blank'); // 在新視窗打開
            else window.location.href = notification.NotificationLink;
        }
    }
}

// 取得所有 Notifications 通知
const notificationList = ref<models.Notifications[]>([]);
async function GetAllNotifications(): Promise<void> {
    try {
        notificationList.value = await apiService.callApi(apiService.getApiWebGetAllNotifications);
    } catch (error) {
        toast.add({ severity: 'error', summary: '錯誤', detail: `無法取得通知資料，${error}`, life: 5000 });
    }
}

// 計算未讀通知數，公用的過期前都算(出現都算)
const unreadNotificationCount = computed(() => {
    return notificationList.value.filter((notification) => {
        return notification.ReadTime === null && notification.ProductionPercentage >= 100;
    }).length;
});

// 監聽 unreadNotificationCount 變化，當有新通知時，數字跳動
const notificationBadgeIsAnimating = ref(false);
watch(unreadNotificationCount, (newVal, oldVal) => {
    if (newVal !== oldVal && newVal > 0) {
        notificationBadgeIsAnimating.value = true;
        setTimeout(() => {
            notificationBadgeIsAnimating.value = false;
        }, 5000); // 跳動效果持續 5 秒
    }
});

// 設定面板 /////////////////////////////////////////////////////////////////////////////////////////////////
const settingsPopover = ref();
const toggleSettings = (event: Event) => {
    if (settingsPopover.value) {
        settingsPopover.value.toggle(event);
    }
};

// 登出
const Logout = () => {
    router.push('/Logout');
};

// ✨✨✨✨✨✨✨✨✨✨✨✨ 表單定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
const userChangePasswordDialog = ref(false);

export interface FormCompositeData extends models.UserChangePasswordDTO {}
const initialValues: FormCompositeData = {
    NewPassword: '',
    ConfirmPassword: ''
};
const validationSchema = yup.object({
    NewPassword: yup.string().required('請輸入密碼').min(6, '密碼最少為 6 碼').max(20, '密碼最長為 20 碼'),
    ConfirmPassword: yup
        .string()
        .required('請再次輸入密碼')
        .oneOf([yup.ref('NewPassword'), ''], '密碼不一致')
});
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
const [NewPassword] = defineField('NewPassword');
const [ConfirmPassword] = defineField('ConfirmPassword');

// 打開對話框、綁定資料
const onUserChangePassword = async () => {
    userChangePasswordDialog.value = true;
    await resetForm({ values: initialValues });
};
// 表單提交
const userChangePasswordSubmit = handleSubmit(async (values: FormCompositeData) => {
    try {
        const submitData = ref<FormCompositeData>({ ...values });
        await apiService.callApi(apiService.postApiWebUserChangePassword, submitData.value);
        userChangePasswordDialog.value = false;
        toast.add({ severity: 'success', summary: '密碼修改成功，下次登入請使用新密碼', life: 3000 });
    } catch (error) {
        toast.add({ severity: 'error', summary: '修改密碼發生錯誤', detail: error, life: 3000 });
    }
});
// 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚 END. 表單定義區 🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚🔚
</script>

<template>
    <div class="layout-topbar">
        <div class="layout-topbar-start flex items-center">
            <a ref="menuButton" class="layout-menu-button" @click="onMenuToggle">
                <i class="pi pi-bars"></i>
            </a>
            <span class="layout-topbar-logo flex-shrink-0">
                <router-link to="/">
                    <img src="/layout/images/logo/GSlogo.png" style="width: 4rem; height: auto" alt="Logo" />
                </router-link>
            </span>

            <div class="pl-1.5 flex flex-col">
                <span class="text-lg text-surface-900 dark:text-surface-0 font-semibold"> 好睡王家居 </span>
            </div>

            <a ref="mobileMenuButton" class="layout-topbar-mobile-button" @click="onTopbarMenuToggle">
                <i class="pi pi-ellipsis-v"></i>
            </a>
        </div>

        <div class="layout-topbar-end">
            <div class="layout-topbar-actions-end">
                <ul class="layout-topbar-items">
                    <li>
                        <a @click="toggleNotification($event)">
                            <div class="relative flex items-center">
                                <i class="pi pi-bell"></i>
                                <!-- 通知數字 -->
                                <span
                                    v-if="unreadNotificationCount > 0"
                                    class="absolute top-[-0.6rem] right-[-0.6rem] text-xs bg-red-500 text-white rounded-full w-5 h-5 flex items-center justify-center"
                                    :class="{ 'animate-bounce': notificationBadgeIsAnimating }"
                                >
                                    {{ unreadNotificationCount }}
                                </span>
                            </div>
                        </a>
                        <Popover ref="notificationPopover">
                            <div class="flex flex-col gap-4 w-72">
                                <h3 class="text-lg font-semibold p-3 border-b border-gray-300 dark:border-gray-700">通知中心</h3>
                                <ul v-if="notificationList.length > 0" class="max-h-80 overflow-y-auto">
                                    <li
                                        v-for="(notification, index) in notificationList"
                                        :key="notification.NotificationId"
                                        class="p-3 border-gray-200 dark:border-gray-700 cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800"
                                        :class="{ 'border-b': index !== notificationList.length - 1 }"
                                        @click="notificationClick(notification)"
                                    >
                                        <!-- 最後一個不顯示邊框 -->
                                        <div class="flex items-center gap-2">
                                            <!-- 根據優先級顯示不同圖示 -->
                                            <i v-if="notification.NotificationPriority === 1" class="fa-solid fa-circle-info text-blue-500" aria-hidden="true"></i>
                                            <i v-else-if="notification.NotificationPriority === 2" class="fa-solid fa-circle-info text-yellow-500" aria-hidden="true"></i>
                                            <i v-else-if="notification.NotificationPriority === 3" class="fa-solid fa-triangle-exclamation text-red-500" aria-hidden="true"></i>
                                            <i v-else class="fa-solid fa-bookmark text-green-500" aria-hidden="true"></i>

                                            <!-- 顯示通知內容，根據已讀/未讀狀態調整字體 -->
                                            <div :class="{ 'font-bold': !notification.ReadTime, 'text-gray-500': notification.ReadTime }" class="flex flex-col">
                                                <div :class="[{ 'text-blue-200': isDarkTheme, 'text-blue-500': !isDarkTheme }]">
                                                    <i v-if="!notification.UserId" class="fa-solid fa-bullhorn"></i>
                                                    <span v-if="notification.NotificationType"> [{{ notification.NotificationType }}] </span>
                                                </div>
                                                <span>{{ notification.NotificationMessageContent }}</span>
                                                <span v-if="notification.NotificationErrorMessageContent" :class="[{ 'text-red-100': isDarkTheme, 'text-red-500': !isDarkTheme }]">{{ notification.NotificationErrorMessageContent }}</span>

                                                <!-- 顯示通知發送時間 -->
                                                <span class="text-xs text-gray-400">
                                                    {{ dayjs(notification.CreationTime).format('YYYY-MM-DD HH:mm:ss') }}
                                                </span>

                                                <!-- 顯示進度條（若需要顯示） -->
                                                <div v-if="notification.ProductionPercentage < 100" class="mt-2">
                                                    <ProgressBar v-if="notification.ProductionPercentage < 100" :value="notification.ProductionPercentage" />
                                                    <span class="text-sm text-gray-600 block mt-1">處理中，請稍後...</span>
                                                </div>
                                            </div>
                                        </div>
                                    </li>
                                </ul>
                                <p v-else class="p-3 text-center text-gray-500">目前沒有通知</p>
                            </div>
                        </Popover>
                    </li>
                    <li>
                        <a @click="toggleSettings($event)">
                            <i class="pi pi-cog"></i>
                        </a>
                        <Popover ref="settingsPopover">
                            <ul class="list-none p-0 m-0 text-lg">
                                <li>
                                    <div class="py-2 px-4 flex gap-2 mt-2 mb-2">
                                        <i class="pi pi-fw pi-user text-lg"></i>
                                        <span> {{ authStore.user?.Username }} ({{ authStore.user?.UserDescription }}) </span>
                                    </div>
                                </li>
                                <hr class="border-t border-surface-300 dark:border-surface-700 mt-2 mb-2" />
                                <li>
                                    <a
                                        class="py-2 px-4 flex gap-2 cursor-pointer text-color hover:text-primary"
                                        @click="
                                            toggleDarkMode();
                                            themeStore.setDarkTheme(!layoutConfig.darkTheme);
                                        "
                                    >
                                        <i :class="['pi text-lg', { 'pi-sun': isDarkTheme, 'pi-moon': !isDarkTheme }]"></i>
                                        <span>切換至{{ isDarkTheme ? '淺色' : '深色' }}風格</span>
                                    </a>
                                </li>
                                <li>
                                    <a class="py-2 px-4 flex gap-2 cursor-pointer text-color hover:text-primary" @click="onUserChangePassword">
                                        <i class="fa-solid fa-key"></i>
                                        <span>修改登入密碼</span>
                                    </a>
                                </li>
                                <li>
                                    <a class="py-2 px-4 flex gap-2 cursor-pointer text-color hover:text-primary" @click="Logout">
                                        <i class="fa-solid fa-right-from-bracket text-lg"></i>
                                        <span>帳號登出</span>
                                    </a>
                                </li>
                            </ul>
                        </Popover>
                    </li>
                </ul>
            </div>
        </div>
        <i v-if="isLoading" class="loading-spinner"></i>

        <Dialog v-model:visible="userChangePasswordDialog" class="w-full max-w-[95%] md:max-w-[50%] lg:max-w-[40%]" header="修改登入密碼" :modal="true">
            <form @submit.prevent="userChangePasswordSubmit">
                <div class="space-y-4">
                    <Fieldset legend="使用者帳號">
                        <div class="grid grid-cols-2 gap-4">
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">密碼</span>
                                <InputText id="NewPassword" v-model="NewPassword" :invalid="!!errors?.['NewPassword']" placeholder="********" type="password" fluid />
                                <small class="text-red-500">{{ errors?.['NewPassword'] }}</small>
                            </div>
                            <div class="col-span-1">
                                <span class="block font-bold mb-3">確認密碼</span>
                                <InputText id="ConfirmPassword" v-model="ConfirmPassword" :invalid="!!errors?.['ConfirmPassword']" placeholder="********" type="password" fluid />
                                <small class="text-red-500">{{ errors?.['ConfirmPassword'] }}</small>
                            </div>
                        </div>
                    </Fieldset>
                    <div class="flex justify-end items-center gap-4 pt-6">
                        <i v-if="isLoading" class="loading-spinner-inline"></i>
                        <Button label="取消" icon="pi pi-times" text @click="userChangePasswordDialog = false" />
                        <Button label="確定修改" icon="pi pi-check" outlined @click="userChangePasswordSubmit" />
                    </div>
                </div>
            </form>
        </Dialog>
    </div>
</template>
