<script setup lang="ts">
import { provide, ref } from 'vue';
import { useRouter } from 'vue-router';
import AgGridLocale from '@/locales/ag-grid/zh-TW';
import ProgressSpinner from 'primevue/progressspinner';
import Toast from 'primevue/toast';

// 提供全局的依賴注入
provide('agGridLocale', AgGridLocale);
provide('paginationPageSize', 100);
provide('paginationPageSizeSelector', [100, 200, 500]);

provide('LiffIdLiffRoute', '2006978541-ENB6kMkj');
provide('LiffIdLineLogin', '2006978541-aM1MgNg9');

// 監聽路由變更，顯示 loading
const isRouteLoading = ref(false);
const router = useRouter();

router.beforeEach((to) => {
    if (to.name === 'LiffRoute') return; // 忽略 LiffRoute 這個頁面，避免顯示 loading，造成閃爍多次
    if (to.meta.skipLoading) return; // 如果路由有 skipLoading 標記，就不顯示 loading
    isRouteLoading.value = true;
});

router.afterEach(() => {
    setTimeout(() => {
        isRouteLoading.value = false;
    }, 200); // 增加緩衝時間，避免閃爍
});
</script>

<template>
    <!-- 全螢幕載入遮罩 -->
    <Toast />
    <div v-if="isRouteLoading" class="loading-overlay">
        <ProgressSpinner style="width: 100px; height: 100px" strokeWidth="4" fill="transparent" animationDuration=".9s"
            aria-label="Custom ProgressSpinner" />
    </div>

    <router-view />
</template>

<style scoped>
/* 全螢幕遮罩樣式 */
.loading-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.2);
    /* 淺黑色透明度 20%，適用各種風格 */
    backdrop-filter: blur(2px);
    /* 模糊背景，提升質感 */
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 9999;
}
</style>
