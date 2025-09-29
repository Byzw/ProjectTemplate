<script setup>
import { useLayout } from '@/layout/composables/layout';
import { onBeforeMount, ref } from 'vue';
import { useRouter } from 'vue-router';

const { isDarkTheme, toggleDarkMode } = useLayout();

const router = useRouter();

const backgroundImage = ref('url(/layout/images/pages/exception/bg-404.png');
const backgroundStyle = ref({
    background: backgroundImage.value
});

onBeforeMount(() => {
    isDarkTheme.value && toggleDarkMode();
});

function navigateToDashboard() {
    router.push('/');
}
</script>

<template>
    <div class="overflow-hidden m-0 relative h-screen !bg-cover !bg-center" :style="backgroundStyle">
        <div class="text-center text-5xl pt-8 font-bold text-white">
            <span class="text-color">喔喔! 找不到您要的頁面</span>
        </div>
        <div class="w-full absolute bottom-0 text-center bg-surface-900 dark:bg-surface-0 h-56">
            <div class="w-full absolute flex justify-center z-10" style="top: -36px">
                <img src="/layout/images/pages/exception/icon-error.png" alt="avalon-ng" />
            </div>
            <div class="w-[29rem] relative text-white" style="margin-left: -200px; left: 50%; top: 30px">
                <div class="p-4">
                    <h3 class="m-0 mb-2 text-white">Page Not Found.</h3>
                    <p class="m-0">Please contact system administrator.</p>
                </div>
                <Button @click="navigateToDashboard" label="回到首頁"></Button>
            </div>
        </div>
    </div>
</template>
