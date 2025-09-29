<script setup lang="ts">
import { ref, watch, defineExpose } from 'vue';
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import { debounce } from 'lodash';
import { useToast } from 'primevue/usetoast';
import dayjs from 'dayjs';

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

// 監聽 props.modelValue (通常是 BizPartnerId)
// 確保當 BizPartnerId 從外部設定或初始載入時，
// 其完整資料存在於 customerList 中，以便於顯示/選取。
watch(() => props.modelValue, async (currentModelId) => {
    if (currentModelId && !customerList.value.some(item => item.BizPartnerId === currentModelId)) {
        try {
            const results = await apiService.callApi(apiService.getApiWebFetchCustomerSelectList, { keyword: currentModelId }) as models.CustomerSelectList[];
            const targetCustomer = results.find(c => c.BizPartnerId === currentModelId);
            if (targetCustomer) {
                // 如果客戶不在列表中，則將其加入，或確保其優先顯示
                const existingIndex = customerList.value.findIndex(c => c.BizPartnerId === targetCustomer.BizPartnerId);
                if (existingIndex === -1) {
                    customerList.value = [targetCustomer, ...customerList.value.filter(c => c.BizPartnerId !== targetCustomer.BizPartnerId)].slice(0, 20);
                }
            }
        } catch (error) {
            props.toast.add({ severity: 'error', summary: '載入客戶資料失敗', detail: error, life: 3000 });
        }
    } 
}, { immediate: true });

// 防抖搜尋函式，用於處理直接在可編輯 Select 輸入框中輸入的文字
const debouncedSearchFromEditableInput = debounce(async (searchText: string | null) => {
    if (typeof searchText === 'string' && searchText.trim().length > 0) {
        try {
            const searchResults = await apiService.callApi(apiService.getApiWebFetchCustomerSelectList, { keyword: searchText }) as models.CustomerSelectList[];
            customerList.value = searchResults || [];
        } catch (error) {
            props.toast.add({ severity: 'error', summary: '搜尋客戶失敗', detail: error, life: 3000 });
            customerList.value = [];
        }
    } else if (!searchText) {
        // 如果輸入被清除，且先前已選取 BizPartnerId，嘗試顯示該選取內容的上下文。
        if (props.modelValue && customerList.value.some(c => c.BizPartnerId === props.modelValue)) {
             // 不進行 API 呼叫，僅確保列表中包含選取項或不要突然清除它
             // 或者，如果列表為空，則重新獲取選取項
             if (!customerList.value.find(c=>c.BizPartnerId === props.modelValue)){
                const result = await apiService.callApi(apiService.getApiWebFetchCustomerSelectList, { keyword: props.modelValue } ) as models.CustomerSelectList[];
                customerList.value = result;
             }
        } else if (props.modelValue) { // 選取的 ID 存在，但不在列表中 (例如列表來自不同的搜尋)
            const result = await apiService.callApi(apiService.getApiWebFetchCustomerSelectList, { keyword: props.modelValue } ) as models.CustomerSelectList[];
            customerList.value = result;
        }else {
            customerList.value = []; // 如果沒有選取的 ID 且沒有搜尋文字，則完全清除
        }
    }
}, 500);

// 處理來自 Select 的 @update:modelValue 事件；區分輸入文字和選取項目
const handleEditableInputAndSelection = (newValue: string | null) => {
    emit('update:modelValue', newValue);

    if (newValue) {
        const selectedCustomer = customerList.value.find(c => c.BizPartnerId === newValue);
        if (selectedCustomer) {
            // 從列表中選取了一個客戶，或者輸入/貼上了一個有效的 BizPartnerId。
            emit('customerChange', selectedCustomer);
        } else {
            // 輸入的文字不是當前列表中的 BizPartnerId。
            emit('customerChange', null); // 此文字沒有對應的有效客戶物件。
            debouncedSearchFromEditableInput(newValue); // 根據輸入的文字進行搜尋。
        }
    } else {
        // 輸入已被清除。
        emit('customerChange', null);
        debouncedSearchFromEditableInput(null); // 處理建議列表的清除。
    }
};

// 新增：供父元件呼叫的 focus 方法
const focusInputElement = () => {
  if (selectRef.value && typeof selectRef.value.focus === 'function') {
    selectRef.value.focus(); // 呼叫 PrimeVue Select 元件自身的 focus 方法
  } else if (selectRef.value && selectRef.value.$el) {
    // 備用方案：如果直接呼叫 focus 不可用或無效，嘗試聚焦其 DOM 元素中的主要輸入框
    const inputElement = selectRef.value.$el.querySelector('input.p-select-label, input[role="combobox"]');
    if (inputElement) {
      inputElement.focus();
    }
  }
};

defineExpose({
  focus: focusInputElement
});

</script>

<template>
    <Select ref="selectRef"
        :model-value="props.modelValue"
        @update:model-value="handleEditableInputAndSelection"
        :options="customerList"
        :invalid="props.invalid"
        :disabled="props.disabled"
        optionValue="BizPartnerId"
        option-label="BizPartnerId"
        placeholder="請輸入關鍵字搜尋"
        class="w-full" 
        editable 
        :loading="isLoading"
        :virtualScrollerOptions="{ lazy: true, itemSize: 75, delay: 0 }"
        :dropdownStyle="{ maxHeight: '400px' }"
        showClear
    >
        <template #option="{ option }">
            <div class="flex flex-col gap-1">
                <div class="flex items-center gap-2">
                    <span class="font-medium text-900">{{ option.BizPartnerId }}</span>
                    <span class="text-700">{{ option.BizPartnerName || '未命名' }}</span>
                </div>
                <div class="grid grid-cols-2 gap-2 text-sm text-600">
                    <span v-if="option.WorkTelNo" class="inline-flex items-center gap-1">
                        <i class="fa-solid fa-phone"></i>{{ option.WorkTelNo }}
                    </span>
                    <span v-if="option.FaxNo" class="inline-flex items-center gap-1">
                        <i class="fa-solid fa-house"></i>{{ option.FaxNo }}
                    </span>
                    <span v-if="option.QQNo" class="inline-flex items-center gap-1">
                        <i class="fa-solid fa-mobile-screen-button"></i>{{ option.QQNo }}
                    </span>
                    <span v-if="option.BirthDateTime && option.BirthDateTime != 0" class="inline-flex items-center gap-1">
                        <i class="fa-solid fa-cake-candles"></i>{{ dayjs(option.BirthDateTime).format('YYYY/MM/DD') }}
                    </span>
                </div>
            </div>
        </template>
        <template #value="{ value, placeholder }">
            <span v-if="value" class="font-medium text-900">{{ value }}</span>
            <span v-else class="text-gray-400">{{ placeholder }}</span>
        </template>
    </Select>
</template>
