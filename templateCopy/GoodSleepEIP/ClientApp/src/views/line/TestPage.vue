<script setup lang="ts">
import AppConfig from '@/layout/AppConfig.vue';
import { useLayout } from '@/layout/composables/layout';
import { apiService, isLoading } from '@/service/apiClient';
import type { LineBindRequest, Users } from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { nextTick, onMounted, inject, ref } from 'vue';
import liff from "@line/liff";
import type { Profile } from "@liff/get-profile";

const authStore = useAuthStore();
const layout = useLayout();



onMounted(async () => {
    try {

        return;
    } catch (error) {

    }

});
</script>

<template>
    <AppConfig />
    <div>
        <div class="card w-[90%] mx-auto mt-8">
            <div class="card-header">
                <h1 class="text-2xl font-bold">部門比較</h1>
            </div>
            <div class="space-y-4">
                <Fieldset legend="比較年度">
                    <div class="col-span-1">
                        <span class="block font-bold mb-3">部門名(描述)</span>
                        <InputText id="DepartmentDescription" fluid />
                    </div>
                    <div class="col-span-1">
                        <span class="block font-bold mb-3">部門名(1)</span>
                        <InputText id="DepartmentDescription1" fluid />
                    </div>
                    <div class="col-span-1">
                        <span class="block font-bold mb-3">部門名(2)</span>
                        <InputText id="DepartmentDescription2" fluid />
                    </div>
                    <div class="col-span-1">
                        <span class="block font-bold mb-3">部門名(3)</span>
                        <InputText id="DepartmentDescription3" fluid />
                    </div>
                </Fieldset>
                <span
                    class="flex justify-center">按下『工作開始』後系統即開始產生報表，每種報表依複雜度耗時不同，請耐心等候；您可隨時至『通知中心』(上方的小鈴鐺)查閱工作進度。</span>
                <div class="flex justify-end items-center gap-4 pt-6">
                    <i v-if="isLoading" class="loading-spinner-inline"></i>
                    <Button label="取消" icon="pi pi-times" text @click="liff.closeWindow" />
                    <Button label="工作開始" icon="fa-solid fa-person-digging" outlined @click="" />
                </div>
            </div>
        </div>
    </div>

</template>
<style scoped></style>