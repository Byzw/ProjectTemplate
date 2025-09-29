<script setup lang="ts">
import { useAuthStore } from '@/stores/auth';
import { useToast } from 'primevue/usetoast';
import { onMounted, ref } from 'vue';

const toast = useToast();
const authStore = useAuthStore();

const SaleUnApprovedCount = ref(0);
const SaleAllCount = ref(0);
const UserCount = ref(0);
const CustomerCount = ref(0);

onMounted(async () => {
    try {
        // if (!authStore.CurrentSelectedCompany) {
        //     toast.add({ severity: 'error', summary: '無有效門市', detail: "請聯絡系統管理員", life: 5000 });
        //     return;
        // }
        // await fetchCollection();
    } catch (error) {
        toast.add({ severity: 'error', summary: '初始化參數錯誤!', detail: error, life: 5000 });
    }
});

// ✨✨✨✨✨✨✨✨✨✨✨✨ [參數取得]區與功能呼叫區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 各類參數一次取得
// async function fetchCollection(): Promise<void> {
//     try {
//         const statisticResult = await apiService.callApi(apiService.getApiWebGetDashboardStatistics, { companyId: authStore.CurrentSelectedCompany }) as { SaleUnApprovedCount: number, SaleAllCount: number, UserCount: number, CustomerCount: number };
//         SaleUnApprovedCount.value = statisticResult.SaleUnApprovedCount;
//         SaleAllCount.value = statisticResult.SaleAllCount;
//         UserCount.value = statisticResult.UserCount;
//         CustomerCount.value = statisticResult.CustomerCount;
//     } catch (error) {
//         throw error;
//     }
// }
</script>

<template>
    <div class="grid grid-cols-12 gap-8">
        <div class="col-span-12">
            <div class="p-4 h-24 rounded-border m-0 bg-center bg-cover bg-no-repeat bg-cyan-400" style="background-image: url('/layout/images/dashboard/effect-1.svg')">
                <div class="text-gray-600 dark:text-black font-bold text-xl w-full mb-2">
                    <span>歡迎使用 EIP 系統</span>
                </div>
                <div class="text-gray-700 text-2xl font-bold w-full flex items-center py-1">
                    <!-- {{ authStore.CurrentSelectedCompanyName }} -->
                    好睡王家居
                </div>
            </div>
        </div>

        <!-- <div class="col-span-12 lg:col-span-4">
            <div class="card">
                <div class="flex items-center justify-between">
                    <div>
                        <span class="font-bold text-3xl text-color">
                            {{ SaleAllCount.toLocaleString() }} /
                            <span :class="SaleUnApprovedCount > 0 ? 'blink text-red-500' : ''">
                                {{ SaleUnApprovedCount.toLocaleString() }}
                            </span>
                        </span>
                        <p class="mt-2 mb-0 text-2xl text-muted-color">本月銷售憑單數 / 待審核數</p>
                    </div>
                    <div>
                        <img src="/layout/images/dashboard/stats-illustration-3.svg" />
                    </div>
                </div>
            </div>
        </div>

        <div class="col-span-12 lg:col-span-4">
            <div class="card">
                <div class="flex items-center justify-between">
                    <div>
                        <span class="font-bold text-3xl text-color">{{ CustomerCount.toLocaleString() }}</span>
                        <p class="mt-2 mb-0 text-2xl text-muted-color">本門市累積客戶數</p>
                    </div>
                    <div>
                        <img src="/layout/images/dashboard/stats-illustration-1.svg" />
                    </div>
                </div>
            </div>
        </div>

        <div class="col-span-12 lg:col-span-4">
            <div class="card">
                <div class="flex items-center justify-between">
                    <div>
                        <span class="font-bold text-3xl text-color">{{ UserCount.toLocaleString() }}</span>
                        <p class="mt-2 mb-0 text-2xl text-muted-color">本門市關聯使用者帳號數</p>
                    </div>
                    <div>
                        <img src="/layout/images/dashboard/stats-illustration-2.svg" />
                    </div>
                </div>
            </div>
        </div> -->
    </div>
</template>

<style scoped>
.blink {
    animation: blink 1s step-end infinite;
}

@keyframes blink {
    0%,
    100% {
        opacity: 1;
    }

    50% {
        opacity: 0;
    }
}
</style>
