<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 只能選擇下拉的客戶組件，不能輸入不存的客戶， CustomerSelector是可以輸入不存在的客戶
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { isNullOrEmpty } from '@/utils/common';
import { debounce } from 'lodash'; // 防抖
import { useToast } from 'primevue/usetoast';
import { ref, watch } from 'vue';

const props = defineProps<{
    modelValue: models.CustomerSelectList | null | undefined;
    invalid?: boolean;
    toast: ReturnType<typeof useToast>;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: models.CustomerSelectList | null): void;
    (e: 'change', value: models.CustomerSelectList | null): void;
}>();

const authStore = useAuthStore();
const customerList = ref<models.CustomerSelectList[]>([]);
const selectRef = ref();

// 監聽 modelValue 變化
watch(
    () => props.modelValue?.BizPartnerId,
    async (newValue) => {
        if (!newValue) {
            customerList.value = [];
        } else if (customerList.value.length === 0 || !customerList.value.some((item) => item.BizPartnerId === newValue)) {
            // 如果 customerList 中沒有當前選中的值，從後端獲取資料
            try {
                const result = (await apiService.callApi(apiService.getApiWebFetchCustomerSelectList, {
                    keyword: newValue
                })) as models.CustomerSelectList[];
                if (result && result.length > 0) {
                    customerList.value = result;
                    emit('update:modelValue', {
                        BizPartnerId: customerList.value[0].BizPartnerId,
                        BizPartnerName: customerList.value[0].BizPartnerName,
                        WorkTelNo: null,
                        CustomerRemark: null,
                        FaxNo: null,
                        QQNo: null,
                        ContactAddress: null,
                        PartnerRemark: null,
                        LinkMan: null,
                        LinkSex: null,
                        LinkTelNo: null,
                        LinkMobile: null,
                        LinkContactAddress: null,
                        LinkRemark: null,
                        AddrId: null,
                        Address: null,
                        AddrLinkMan: null,
                        AddrTelNo: null,
                        AddrFaxNo: null,
                        AddrRemark: null,
                        SexType: null,
                        BirthDateTime: null
                    });
                }
            } catch (error) {
                props.toast.add({ severity: 'error', summary: '搜尋客戶失敗', detail: error, life: 3000 });
            }
        }
    },
    { immediate: true }
);

// 搜尋處理函數，防抖 600ms，注意為了避免資料太大，後端限制最高20筆
const onFilter = debounce(async (event) => {
    try {
        // 至少輸入1個字才開始搜尋
        if (!event.value || event.value.length < 1) {
            return;
        }
        const searchResults = (await apiService.callApi(apiService.getApiWebFetchCustomerSelectList, {
            keyword: event.value
        })) as models.CustomerSelectList[];
        customerList.value = props.modelValue && !searchResults.some((item) => item.BizPartnerId === props.modelValue) ? [...searchResults, ...customerList.value.filter((item) => item.BizPartnerId === props.modelValue)] : searchResults;
    } catch (error) {
        props.toast.add({ severity: 'error', summary: '搜尋客戶失敗', detail: error, life: 3000 });
        customerList.value = [];
    }
}, 500);

// 格式化生日
const formatBirthDate = (date: number) => {
    if (!date || date == 0) return '';
    const dateStr = date.toString();
    return `${dateStr.substring(0, 4)}/${dateStr.substring(5, 7)}/${dateStr.substring(8, 10)}`;
};

const onClick = () => {
    // 使用 setTimeout 確保下拉選單已經顯示
    setTimeout(() => {
        // 找到搜尋輸入框並聚焦
        selectRef.value.onBlur();
        const searchInput = document.querySelector('.p-select-filter') as HTMLInputElement;
        if (searchInput) {
            searchInput.focus();
        }
    }, 300);
};

const onSelect = (value: string | null) => {
    const selected = customerList.value.find((c) => c.BizPartnerId === value) || null;
    emit('update:modelValue', selected);
    emit('change', selected);
};

</script>

<template>
    <Select
        ref="selectRef"
        :model-value="modelValue"
        @update:model-value="onSelect"
        :options="customerList"
        :invalid="invalid"
        optionValue="BizPartnerId"
        :optionLabel="(item) => `${item.BizPartnerId} ${item.BizPartnerName} ${item.TelNo} ${item.BirthDateTime}`"
        placeholder="請輸入關鍵字搜尋"
        class="w-full"
        filter
        :filter-match-mode="'contains'"
        :show-clear="true"
        @filter="onFilter"
        :loading="isLoading"
        :virtualScrollerOptions="{ lazy: true, itemSize: 60, delay: 0 }"
        :dropdownStyle="{ maxHeight: '400px' }"
        @click="onClick"
    >
        <template #option="{ option }">
            <div class="flex flex-col gap-1">
                <div class="flex items-center gap-2">
                    <span class="font-medium text-900">{{ option.BizPartnerId }}</span>
                    <span class="text-700">{{ option.BizPartnerName || '未命名' }}</span>
                </div>
                <div class="flex gap-2 text-sm text-600">
                    <span v-if="option.WorkTelNo" class="inline-flex items-center gap-1">
                        <i class="fa-solid fa-phone"></i>
                        {{ option.WorkTelNo }}
                    </span>
                    <span v-if="option.BirthDateTime && option.BirthDateTime != 0" class="inline-flex items-center gap-1">
                        <i class="fa-solid fa-cake-candles"></i>
                        {{ formatBirthDate(option.BirthDateTime) }}
                    </span>
                </div>
            </div>
        </template>
        <template #value="{ value, placeholder }">
            <template v-if="value && customerList.length > 0 && !isNullOrEmpty(value.BizPartnerId)">
                <div class="flex items-center gap-2">
                    <span class="font-medium text-900">{{ value.BizPartnerName }}</span>
                    <template v-if="customerList.find((item) => item.BizPartnerId === value)">
                        <span class="text-700">
                            {{ customerList.find((item) => item.BizPartnerId === value)?.BizPartnerName || '' }}
                        </span>
                    </template>
                </div>
            </template>
            <span v-else class="text-gray-400">{{ placeholder }}</span>
        </template>
    </Select>
</template>
