<script setup lang="ts">
import AppConfig from '@/layout/AppConfig.vue';
import { useLayout } from '@/layout/composables/layout';
import { apiService } from '@/service/apiClient';
import type { LoginRequest, Users } from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { nextTick, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

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
const isEnvOk = ref(false); // 環境是否正常
const isLoggingIn = ref(false); // 控制按鈕啟用狀態

onMounted(async () => {
    if (!authStore.checkStorageAvailability()) errorMessage.value = '您的瀏覽器需打開對本網站的儲存允許，方能繼續使用';
    else isEnvOk.value = true;

    await nextTick(); // 等待 Vue DOM 更新(如useLayout的isDarkTheme)，然後才跑 fetchCaptcha
    fetchCaptcha();
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
    loadingMessage.value = `登入中...`;
    try {
        const loginRequest: LoginRequest = {
            Username: userInputUsername.value,
            Password: userInputPassword.value,
            CaptchaId: captchaId.value,
            CaptchaAnswer: userInputCaptcha.value
        };
        const response: Users = await apiService.callApi(apiService.postApiWebLogin, loginRequest);

        // 成功後儲存 Token 和用戶資訊
        errorMessage.value = '';
        loadingMessage.value = `系統程式載入中...`;
        authStore.setToken(response.Token ?? '');
        authStore.setUserInfo(response);

        router.push('/');
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
    <div class="bg-surface-100 dark:bg-surface-950 h-screen w-screen flex items-center justify-center" @keydown.enter="login">
        <div class="bg-surface-0 dark:bg-surface-900 py-16 px-8 sm:px-16 shadow flex flex-col w-11/12 sm:w-[30rem]" style="border-radius: 14px">
            <div class="text-center mb-8">
                <img src="/layout/images/logo/GSlogo.png" alt="GoodSleepEIP" class="w-24 mx-auto" />
                <div class="text-surface-900 dark:text-surface-0 text-3xl font-medium mb-4">Welcome to GoodSleepEIP</div>
                <span class="text-muted-color text-2xl font-medium">請登入您的帳號</span>
            </div>
            <div class="mt-8 text-left">
                <label for="userInputUsername" class="block mb-2">帳號</label>
                <IconField class="block">
                    <InputText id="userInputUsername" type="text" v-model="userInputUsername" :disabled="isLoggingIn" class="w-full" />
                    <InputIcon class="pi pi-user" />
                </IconField>

                <label for="userInputPassword" class="block mb-2 mt-4">密碼</label>
                <IconField class="block">
                    <InputText id="userInputPassword" :type="isPasswordVisible ? 'text' : 'password'" v-model="userInputPassword" :disabled="isLoggingIn" class="w-full" />
                    <InputIcon :class="isPasswordVisible ? 'pi pi-eye-slash' : 'pi pi-eye'" @click="togglePasswordVisibility" class="cursor-pointer" />
                </IconField>

                <label for="userInputCaptcha" class="block mb-2 mt-4">驗證碼</label>
                <div class="mt-4 flex items-center gap-4">
                    <IconField class="block">
                        <InputText type="text" id="userInputCaptcha" v-model="userInputCaptcha" :disabled="isLoggingIn" class="w-full p-2 border rounded-md max-w-xs" />
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
