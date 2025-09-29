<script setup lang="ts">
import { apiService } from '@/service/apiClient';
import type { Users } from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { onMounted, inject, ref } from 'vue';
import { useRouter } from 'vue-router';
import liff from "@line/liff";

const router = useRouter();
const authStore = useAuthStore();
const module = ref('');
const errorMessage = ref('');

onMounted(async () => {
    try {
        await liff.init({ liffId: inject('LiffIdLiffRoute') ?? '' });

        const urlParams = new URLSearchParams(window.location.search);
        if (!liff.isLoggedIn()) {
            if (!urlParams.has('liff.state')) { // **避免 LIFF 重導兩次**
                await liff.login();
                return; // 登入後 LIFF 會自動重新導向，因此不執行後續渲染(Line Endpoint URL Callback)
            }
        }

        // 取得 ID Token
        const LineIdToken = liff.getIDToken();
        if (!LineIdToken) throw new Error('無法取得 LINE ID Token，請重新嘗試。');

        // 檢查有無登入系統，沒有的話用 LineId 登入
        if (authStore.user === null || authStore.isTokenExpired()) {
            const userData: Users = await apiService.callApi(apiService.getApiLineLiffRouteLogin, { LineIdToken: LineIdToken });
            await authStore.setToken(userData.Token ?? '');
            await authStore.setUserInfo(userData);
        }

        // 解析 URL 參數
        //const urlParams = new URLSearchParams(window.location.search);
        module.value = urlParams.get("module") || '';

        // Router 路由
        if (module.value) router.push('/' + decodeURIComponent(module.value));
        else router.replace('/errors/notfound'); // 如果 module 無效，導至 404 頁面
    } catch (err) {
        errorMessage.value = `錯誤: ${err}`;
    }
});
</script>

<template>
    <div>{{ errorMessage }}</div>
</template>
