<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useLayout } from '@/layout/composables/layout';

const router = useRouter();
const layout = useLayout();

const backgroundImage = ref('url(/layout/images/pages/exception/bg-access.png)');
const backgroundStyle = ref({
    background: backgroundImage.value
});

function navigateToDashboard() {
    router.push('/Login');
}
</script>

<template>
    <div class="overflow-hidden m-0 relative h-screen !bg-cover !bg-center" :style="backgroundStyle">
        <div class="text-center text-5xl pt-8 font-bold text-white">
            <div class="inline-block py-1 px-2 text-color bg-surface-50 dark:bg-surface-950">
                <span>Access</span>
            </div>
            <span>Denied</span>
        </div>
        <div class="w-full absolute bottom-0 text-center bg-surface-900 dark:bg-surface-0 h-56">
            <div class="w-full absolute flex justify-center z-10" style="top: -36px">
                <img src="/layout/images/pages/exception/icon-error.png" alt="avalon-ng" />
            </div>
            <div class="w-[29rem] relative text-white" style="margin-left: -200px; left: 50%; top: 30px">
                <div class="p-4">
                    <h3 class="m-0 mb-2 text-surface-0 dark:text-surface-900">Access denied to this resource.</h3>
                    <p class="m-0 text-surface-0 dark:text-surface-900">您沒有存取本模組的權限，如有錯誤請告知系統管理員</p>
                </div>
                <Button @click="navigateToDashboard">重新登入</Button>
            </div>
        </div>
    </div>
</template>
