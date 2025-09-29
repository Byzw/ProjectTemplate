<script setup>
import { ref } from 'vue';

import { useRouter } from 'vue-router';

const router = useRouter();
const backgroundImage = ref('url(/layout/images/pages/exception/bg-error.png)');
const backgroundStyle = ref({
    background: backgroundImage.value
});

function navigateToDashboard() {
    router.push('/');
}
</script>

<template>
    <div class="overflow-hidden m-0 relative h-screen !bg-cover !bg-center" :style="backgroundStyle">
        <div class="text-center text-5xl pt-8 font-bold text-surface-0 dark:text-surface-900">
            <div class="inline-block py-1 px-2 text-surface-900 dark:text-surface-0 bg-surface-50 dark:bg-surface-950">
                <span>Error</span>
            </div>
            <span>Occured</span>
        </div>
        <div class="w-full absolute bottom-0 text-center bg-surface-900 dark:bg-surface-0 h-56">
            <div class="w-full absolute flex justify-center z-10" style="top: -36px">
                <img src="/layout/images/pages/exception/icon-error.png" alt="avalon-ng" />
            </div>
            <div class="w-[29rem] relative text-white" style="margin-left: -200px; left: 50%; top: 30px">
                <div class="p-4">
                    <h3 class="m-0 mb-2 text-surface-0 dark:text-surface-900">An error occured.</h3>
                    <p class="m-0 text-surface-0 dark:text-surface-900">Please contact system administrator.</p>
                </div>
                <Button @click="navigateToDashboard" label="Go To Dashboard"></Button>
            </div>
        </div>
    </div>
</template>
