<script setup lang="ts">
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import dayjs from 'dayjs';
import { debounce } from 'lodash';
import { useToast } from 'primevue/usetoast';
import { computed, defineExpose, ref, watch } from 'vue';

const props = defineProps<{
    modelValue: string | null;
    invalid?: boolean;
    toast: ReturnType<typeof useToast>;
    disabled?: boolean;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: string | null): void;
    (e: 'customerChange', customer: models.CustomerSelectList | null): void;
}>();

const customerList = ref<models.CustomerSelectList[]>([]);
const selectRef = ref<any>(null);
const queryKey = ref();

const selectedCustomer = computed(() => {
    return customerList.value.find((c) => c.BizPartnerId === props.modelValue) || null;
});

// 監聽 modelValue，確保外部設定的值在 customerList 中
watch(
    () => props.modelValue,
    async (currentModelId) => {
        if (currentModelId && !customerList.value.some((item) => item.BizPartnerId === currentModelId)) {
            try {
                const results = (await apiService.callApi(apiService.getApiWebFetchCustomerSelectList, { keyword: currentModelId })) as models.CustomerSelectList[];
                const targetCustomer = results.find((c) => c.BizPartnerId === currentModelId);
                if (targetCustomer) {
                    customerList.value = [targetCustomer, ...customerList.value.filter((c) => c.BizPartnerId !== targetCustomer.BizPartnerId)].slice(0, 20);
                }
            } catch (error) {
                props.toast.add({ severity: 'error', summary: '載入客戶資料失敗', detail: error, life: 3000 });
            }
        }
    },
    { immediate: true }
);

// 搜尋方法，供 AutoComplete 使用
const searchCustomer = debounce(async (query: string) => {
    if (!query?.trim()) {
        customerList.value = [];
        return;
    }

    try {
        queryKey.value = query;
        const result = (await apiService.callApi(apiService.getApiWebFetchCustomerSelectList, { keyword: queryKey.value })) as models.CustomerSelectList[];
        customerList.value = result;
    } catch (error) {
        customerList.value = [];
        props.toast.add({ severity: 'error', summary: '搜尋客戶失敗', detail: error, life: 3000 });
    }
}, 500);

// 當選取項目改變
const handleCustomerChange = (selected: models.CustomerSelectList | null) => {
    const newId = selected?.BizPartnerId || null;
    emit('update:modelValue', newId);
    emit('customerChange', selected || null);
};

// 聚焦功能
const focusInputElement = () => {
    if (selectRef.value?.focus) {
        selectRef.value.focus();
    } else {
        const input = selectRef.value?.$el?.querySelector('input');
        if (input) input.focus();
    }
};

defineExpose({
    focus: focusInputElement
});
</script>

<template>
    <AutoComplete
        ref="selectRef"
        class="w-full"
        :modelValue="selectedCustomer"
        :suggestions="customerList"
        @complete="(e) => searchCustomer(e.query)"
        @update:modelValue="handleCustomerChange"
        :optionLabel="'BizPartnerId'"
        :disabled="props.disabled"
        :class="{ 'p-invalid': props.invalid }"
        placeholder="請輸入關鍵字搜尋"
        forceSelection
        dropdown
        :loading="isLoading"
        @dropdown-click="
            () => {
                searchCustomer(queryKey);
            }
        "
    >
        <!-- 下拉選項模板 -->
        <template #option="{ option }">
            <div class="flex flex-col gap-1">
                <div class="flex items-center gap-2">
                    <span class="font-medium text-900">{{ option.BizPartnerId }}</span>
                    <span class="text-700">{{ option.BizPartnerName || '未命名' }}</span>
                </div>
                <div class="grid grid-cols-2 gap-2 text-sm text-600">
                    <span v-if="option.WorkTelNo" class="inline-flex items-center gap-1"> <i class="fa-solid fa-phone"></i>{{ option.WorkTelNo }} </span>
                    <span v-if="option.FaxNo" class="inline-flex items-center gap-1"> <i class="fa-solid fa-house"></i>{{ option.FaxNo }} </span>
                    <span v-if="option.QQNo" class="inline-flex items-center gap-1"> <i class="fa-solid fa-mobile-screen-button"></i>{{ option.QQNo }} </span>
                    <span v-if="option.BirthDateTime && option.BirthDateTime != 0" class="inline-flex items-center gap-1"> <i class="fa-solid fa-cake-candles"></i>{{ dayjs(option.BirthDateTime).format('YYYY/MM/DD') }} </span>
                </div>
            </div>
        </template>
    </AutoComplete>
</template>
