<script setup lang="ts">
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
// Description: 業務員選擇器組件，只能選擇下拉的業務員，不能輸入不存在的業務員
// ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️ℹ️
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import { isNullOrEmpty } from '@/utils/common';
import { debounce } from 'lodash'; // 防抖
import { useToast } from 'primevue/usetoast';
import { ref, watch } from 'vue';

const props = defineProps<{
    modelValue: models.PersonProgramTimeSlotDto | null | undefined;
    invalid?: boolean;
    toast: ReturnType<typeof useToast>;
    placeholder?: string;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: models.PersonProgramTimeSlotDto | null): void;
    (e: 'change', value: models.PersonProgramTimeSlotDto | null): void;
}>();

const personList = ref<models.PersonProgramTimeSlotDto[]>([]);
const selectRef = ref();

// 監聽 modelValue 變化
watch(
    () => props.modelValue?.PersonId,
    async (newValue) => {
        if (!newValue) {
            // 如果當前有選中的業務員且有有效的 PersonId，保留在列表中
            if (props.modelValue && props.modelValue.PersonId) {
                personList.value = [props.modelValue];
            } else {
                personList.value = [];
            }
        } else if (personList.value.length === 0 || !personList.value.some((item) => item.PersonId === newValue)) {
            // 如果 personList 中沒有當前選中的值，從後端獲取資料
            try {
                const result = (await apiService.callApi(apiService.getApiWebFetchPersonList, {
                    keyword: newValue
                })) as models.PersonProgramTimeSlotDto[];
                if (result && result.length > 0) {
                    personList.value = result;
                    // 找到匹配的業務員並更新 modelValue
                    const matchedPerson = result.find(p => p.PersonId === newValue);
                    if (matchedPerson) {
                        emit('update:modelValue', matchedPerson);
                    }
                }
            } catch (error) {
                props.toast.add({ severity: 'error', summary: '搜尋業務員失敗', detail: error, life: 3000 });
            }
        }
    },
    { immediate: true }
);

// 搜尋處理函數，防抖 600ms
const onFilter = debounce(async (event) => {
    try {
        // 至少輸入1個字才開始搜尋
        if (!event.value || event.value.length < 1) {
            // 該行的原本資料，也該加入列表以免不見
            personList.value = (props.modelValue && props.modelValue.PersonId) ? [props.modelValue] : [];
            return;
        }
        const searchResults = (await apiService.callApi(apiService.getApiWebFetchPersonList, {
            keyword: event.value
        })) as models.PersonProgramTimeSlotDto[];
        
        // 確保當前選中的業務員也在列表中
        personList.value = props.modelValue && !searchResults.some((item) => item.PersonId === props.modelValue?.PersonId) 
            ? [...searchResults, ...personList.value.filter((item) => item.PersonId === props.modelValue?.PersonId)] 
            : searchResults;
    } catch (error) {
        props.toast.add({ severity: 'error', summary: '搜尋業務員失敗', detail: error, life: 3000 });
        personList.value = (props.modelValue && props.modelValue.PersonId) ? [props.modelValue] : [];
    }
}, 600);

const onDropdownShow = () => {
    // 確保下拉選單展開時有預設的選項（包含當前選中的項目）
    if ((!personList.value || personList.value.length === 0) && props.modelValue && props.modelValue.PersonId) {
        personList.value = [props.modelValue];
    }
    
    // 使用 setTimeout 確保下拉選單已經顯示
    setTimeout(() => {
        selectRef.value?.onBlur();
        const searchInput = document.querySelector('.p-select-filter') as HTMLInputElement;
        if (searchInput) {
            searchInput.focus();
        }
    }, 100);
};

const onSelect = (personId: string | null) => {
    const selected = personList.value.find((p) => p.PersonId === personId) || null;
    emit('update:modelValue', selected);
    emit('change', selected);
};
</script>

<template>
    <Select
        ref="selectRef"
        :model-value="modelValue?.PersonId"
        @update:model-value="onSelect"
        :options="personList"
        :invalid="invalid"
        optionValue="PersonId"
        :optionLabel="(item) => `${item.PersonName}`"
        :placeholder="placeholder || '搜尋業務員'"
        class="w-full"
        filter
        :filter-match-mode="'contains'"
        :show-clear="true"
        :resetFilterOnHide="false"
        :autoFilterFocus="true"
        filterPlaceholder="輸入業務名稱..."
        @filter="onFilter"
        @show="onDropdownShow"
        :loading="isLoading"
        :virtualScrollerOptions="{ lazy: true, itemSize: 50, delay: 0 }"
        :dropdownStyle="{ maxHeight: '400px' }"
    >
        <template #option="{ option }">
            <div class="flex flex-col gap-1">
                <div class="flex items-center gap-2">
                    <span class="font-medium text-900">{{ option.PersonName }}</span>
                    <span class="text-600 text-sm">({{ option.PersonId }})</span>
                </div>
            </div>
        </template>
        <template #value="{ value, placeholder }">
            <template v-if="value && modelValue && !isNullOrEmpty(modelValue.PersonId)">
                <span class="font-medium text-900">{{ modelValue.PersonName }}</span>
            </template>
            <span v-else class="text-gray-400">{{ placeholder }}</span>
        </template>
    </Select>
</template> 