<script setup lang="ts">
import AppConfig from '@/layout/AppConfig.vue';
import { useLayout } from '@/layout/composables/layout';
import { apiService } from '@/service/apiClient';
import type { LineBindRequest, Users } from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { nextTick, onMounted, inject, ref } from 'vue';
import { useRouter } from 'vue-router';
import liff from "@line/liff";
import type { Profile } from "@liff/get-profile";

const router = useRouter();
const authStore = useAuthStore();
const layout = useLayout();

const userInputUsername = ref<string | undefined>(''); // 用戶輸入的帳號
const userInputPassword = ref<string | undefined>(''); // 用戶輸入的密碼
const userInputCaptcha = ref<string | undefined>(''); // 用戶輸入的驗證碼
const captchaImage = ref(''); // CAPTCHA 圖片 base64 資料
const captchaId = ref(''); // CAPTCHA 唯一識別ID
const errorMessage = ref(''); // 錯誤訊息
const loadingMessage = ref(''); // 載入訊息
const isPasswordVisible = ref(false); // 密碼是否可見
const isEnvOk = ref(false); // 環境是否正常，控制可否執行登入
const isLoggingIn = ref(false); // 控制按鈕啟用狀態
const isLineInitialized = ref(false); // LINE 是否初始化完成
const showClose = ref(false); // 顯示關閉視窗
const messageClose = ref(''); // 關閉視窗訊息
const profile = ref<Profile>({
    userId: '',
    displayName: '',
    pictureUrl: '',
    statusMessage: ''
});

onMounted(async () => {
    try {
        if (!authStore.checkStorageAvailability()) errorMessage.value = '您的瀏覽器需打開對本網站的儲存允許，方能繼續使用';
        else isEnvOk.value = true;

        // LIFF 初始化
        await liff.init({ liffId: inject('LiffIdLineLogin') ?? '' });

        if (!liff.isLoggedIn()) {
            await liff.login();
            return; // 登入後會重新進入此頁，因此不執行後續渲染(Line Endpoint URL Callback)
        }

        // 取得 ID Token
        const LineIdToken = liff.getIDToken();
        if (!LineIdToken) throw new Error('無法取得 LINE ID Token，請重新嘗試。');


        // 取得使用者資訊: 大頭貼與暱稱
        profile.value = await liff.getProfile();

        // 檢查是否已綁定, 是的話關閉頁面（不需要了）, 否的話繼續原本畫面的初始化（顯示登入介面）
        await apiService.callApi(apiService.getApiLineCheckLineBind, { LineIdToken: LineIdToken });

        messageClose.value = '您已綁定過 LINE 帳號，無需再次綁定';
        showClose.value = true;
        return;             // 防止執行後續初始化
    } catch (error) {
        errorMessage.value = `${error}`;
    }

    // 繼續原本畫面的初始化
    isLineInitialized.value = true;

    await nextTick(); // 等待 Vue DOM 更新(如useLayout的isDarkTheme)，然後才跑 fetchCaptcha
    await fetchCaptcha();
});

// 獲取 CAPTCHA
const fetchCaptcha = async () => {
    try {
        const response: { CaptchaImage: string; CaptchaId: string } = await apiService.callApi(apiService.getApiWebGenerateCaptcha, { isDarkTheme: layout.isDarkTheme.value });

        if (response) {
            captchaImage.value = response.CaptchaImage; // 圖片的 base64 數據
            captchaId.value = response.CaptchaId; // 唯一識別ID
        }
    } catch (error) {
        errorMessage.value = `取得 CAPTCHA 失敗，${error}`;
    }
};

const togglePasswordVisibility = () => {
    isPasswordVisible.value = !isPasswordVisible.value; // 切換密碼遮蔽狀態
};

// 登入
const login = async () => {
    if (!isEnvOk.value) return;
    errorMessage.value = '';
    loadingMessage.value = '';

    if (!userInputUsername.value || !userInputPassword.value) {
        errorMessage.value = '請輸入帳號密碼';
        return;
    }
    if (!userInputCaptcha.value) {
        errorMessage.value = '請輸入驗證碼';
        return;
    }

    isLoggingIn.value = true;
    loadingMessage.value = `綁定中...`;
    try {
        // 在執行綁定前，再次取得 ID Token
        const LineIdToken = liff.getIDToken();
        if (!LineIdToken) throw new Error('無法取得 LINE ID Token，請重新操作。');

        const loginRequest: LineBindRequest = {
            Username: userInputUsername.value,
            Password: userInputPassword.value,
            LineIdToken: LineIdToken,
            CaptchaId: captchaId.value,
            CaptchaAnswer: userInputCaptcha.value
        };
        const response: Users = await apiService.callApi(apiService.postApiLineLineBindLogin, loginRequest);

        // 成功後儲存 Token 和用戶資訊
        errorMessage.value = '';
        loadingMessage.value = `設定中...`;
        authStore.setToken(response.Token ?? '');
        authStore.setUserInfo(response);

        messageClose.value = '已綁定 LINE 帳號，下次見！';
        showClose.value = true;
    } catch (error) {
        errorMessage.value = `登入失敗，${error}`;
        loadingMessage.value = '';
        fetchCaptcha();
    } finally {
        isLoggingIn.value = false;
    }
};
</script>

<template>
    <AppConfig />
    <Dialog v-model:visible="showClose" :style="{ width: '80vw', maxWidth: '400px' }" :closable="false" :modal="true">
        <div class="flex items-center gap-4">
            <i class="pi pi-info-circle !text-3xl" />
            <span v-if="messageClose"><b>{{ messageClose }}</b></span>
        </div>
        <template #footer>
            <Button label="了解" icon="pi pi-check" @click="liff.closeWindow()" />
        </template>
    </Dialog>

    <div v-if="isLineInitialized"
        class="bg-surface-100 dark:bg-surface-950 h-screen w-screen flex items-center justify-center"
        @keydown.enter="login">
        <div class="bg-surface-0 dark:bg-surface-900 py-16 px-8 sm:px-16 shadow flex flex-col w-11/12 sm:w-[30rem]"
            style="border-radius: 14px">
            <div class="flex flex-col text-center mb-8">
                <div class="flex items-center justify-center gap-x-4">
                    <img src="/layout/images/logo/logo.png" alt="GoodSleepEIP" class="w-24" />
                    <img src="/layout/images/logo/logo-line.svg" alt="LINE" class="w-14" />
                </div>
                <div class="text-surface-900 dark:text-surface-0 text-2xl font-medium mt-4">Welcome to GoodSleepEIP</div>
                <!-- 讓大頭貼與文字平行 -->
                <div class="flex items-center justify-center gap-x-4 mt-4">
                    <img v-if="profile.pictureUrl" :src="profile.pictureUrl" alt="LINE 大頭貼"
                        class="rounded-full w-16 h-16" />
                    <div class="text-left">
                        <span class="text-muted-color text-xl font-medium block">請登入您的帳號</span>
                        <span class="text-muted-color text-xl font-medium block">與您的 LINE 進行綁定</span>
                    </div>
                </div>
            </div>
            <div class="mt-6 text-left">
                <label for="userInputUsername" class="block mb-2">帳號</label>
                <IconField class="block">
                    <InputText id="userInputUsername" type="text" v-model="userInputUsername" :disabled="isLoggingIn"
                        class="w-full" />
                    <InputIcon class="pi pi-user" />
                </IconField>

                <label for="userInputPassword" class="block mb-2 mt-4">密碼</label>
                <IconField class="block">
                    <InputText id="userInputPassword" :type="isPasswordVisible ? 'text' : 'password'"
                        v-model="userInputPassword" :disabled="isLoggingIn" class="w-full" />
                    <InputIcon :class="isPasswordVisible ? 'pi pi-eye-slash' : 'pi pi-eye'"
                        @click="togglePasswordVisibility" class="cursor-pointer" />
                </IconField>

                <label for="userInputCaptcha" class="block mb-2 mt-4">驗證碼</label>
                <div class="mt-4 flex items-center gap-4">
                    <IconField class="block">
                        <InputText type="text" id="userInputCaptcha" v-model="userInputCaptcha" :disabled="isLoggingIn"
                            class="w-full p-2 border rounded-md max-w-xs" />
                        <InputIcon class="pi pi-key" />
                    </IconField>
                    <img :src="captchaImage" alt="驗證碼" class="rounded-md cursor-pointer" @click="fetchCaptcha" />
                </div>

                <div class="flex items-center justify-end mt-6">
                    <Button label="登入" outlined class="w-40" :disabled="isLoggingIn" @click="login"></Button>
                    <a href="#" class="secondary-button"></a>
                </div>

                <div v-if="errorMessage" class="mt-4 text-red-500 text-lg flex items-center gap-2">
                    <i class="fa-solid fa-circle-exclamation mr-2"></i>
                    {{ errorMessage }}
                </div>
                <div v-if="loadingMessage" class="mt-4 text-blue-500 text-lg flex items-center gap-2">
                    <i class="fa-solid fa-chevron-right"></i>
                    {{ loadingMessage }}
                </div>
            </div>
        </div>
    </div>
</template>
