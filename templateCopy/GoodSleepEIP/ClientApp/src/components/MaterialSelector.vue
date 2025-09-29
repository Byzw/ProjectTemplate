<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import { apiService, isLoading } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import { debounce } from 'lodash';  // 防抖
import { useToast } from 'primevue/usetoast';

const props = defineProps<{
    modelValue: string | null;
    invalid?: boolean;
    toast: ReturnType<typeof useToast>;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: string | null): void;
    (e: 'materialChange', material: models.ComMaterialDto | null): void;
}>();

const materialList = ref<models.ComMaterialDto[]>([]);
const selectRef = ref();

// 監聽 modelValue 變化
watch(() => props.modelValue, async (newValue) => {
    if (!newValue) {
        materialList.value = [];
    } else if (materialList.value.length === 0 || !materialList.value.some(item => item.MaterialId === newValue)) {
        // 如果 materialList 中沒有當前選中的值，從後端獲取資料
        try {
            const result = await apiService.callApi(
                apiService.getApiT8FetchMaterialSelectList,
                { keyword: newValue }
            ) as models.ComMaterialDto[];
            if (result && result.length > 0) {
                materialList.value = result;
            }
        } catch (error) {
            props.toast.add({ severity: 'error', summary: '獲取產品資料失敗', detail: error, life: 3000 });
        }
    }
}, { immediate: true });

// 搜尋處理函數，防抖 500ms，注意為了避免資料太大，後端限制最高20筆
const onFilter = debounce(async (event) => {
    try {
        // 至少輸入1個字才開始搜尋
        if (!event.value || event.value.length < 1) {
            return;
        }
        const searchResults = await apiService.callApi(
            apiService.getApiT8FetchMaterialSelectList,
            { keyword: event.value }
        ) as models.ComMaterialDto[];

        materialList.value = props.modelValue && !searchResults.some(item => item.MaterialId === props.modelValue)
            ? [...searchResults, ...materialList.value.filter(item => item.MaterialId === props.modelValue)]
            : searchResults;
    } catch (error) {
        props.toast.add({ severity: 'error', summary: '搜尋產品失敗', detail: error, life: 3000 });
        materialList.value = [];
    }
}, 500);

// 處理選擇變更，發送materialChange事件
const handleSelectionChange = (newValue: string | null) => {
    emit('update:modelValue', newValue);
    
    if (newValue) {
        const selectedMaterial = materialList.value.find(m => m.MaterialId === newValue);
        if (selectedMaterial) {
            emit('materialChange', selectedMaterial);
        } else {
            emit('materialChange', null);
        }
    } else {
        emit('materialChange', null);
    }
};

const onClick = () => {
    // 使用 setTimeout 確保下拉選單已經顯示
    setTimeout(() => {
        // 找到搜尋輸入框並聚焦
        const searchInput = document.querySelector('.p-select-filter') as HTMLInputElement;
        if (searchInput) {
            searchInput.focus();
        }
    }, 100);
};
</script>

<template>
    <Select ref="selectRef" :model-value="modelValue"
        @update:model-value="handleSelectionChange" :options="materialList" :invalid="invalid"
        optionValue="MaterialId" :optionLabel="item => `${item.MaterialId} ${item.MaterialName} ${item.UnitId}`"
        placeholder="請輸入關鍵字搜尋" class="w-full" filter :filter-match-mode="'contains'" :show-clear="true"
        @filter="onFilter" :loading="isLoading" :virtualScrollerOptions="{ lazy: true, itemSize: 60, delay: 0 }"
        :dropdownStyle="{ maxHeight: '400px' }" @click="onClick">

        <template #option="{ option }">
            <div class="flex flex-col gap-1">
                <div class="flex items-center gap-2">
                    <span class="font-medium text-900">{{ option.MaterialId }}</span>
                    <span class="text-700">{{ option.MaterialName || '未命名' }}</span>
                </div>
                <div class="flex gap-2 text-sm text-600">
                    <span v-if="option.UnitId" class="inline-flex items-center gap-1">
                        <i class="fa-solid fa-ruler"></i>單位: {{ option.UnitId }}
                    </span>
                    <span v-if="option.StandardPrice != null" class="inline-flex items-center gap-1">
                        <i class="fa-solid fa-dollar-sign"></i>價格: {{ option.StandardPrice }}
                    </span>
                </div>
            </div>
        </template>
        
        <template #value="{ value, placeholder }">
            <template v-if="value && materialList.length > 0">
                <div class="flex items-center gap-2">
                    <span class="font-medium text-900">{{ value }}</span>
                    <template v-if="materialList.find(item => item.MaterialId === value)">
                        <span class="text-700">
                            {{ materialList.find(item => item.MaterialId === value)?.MaterialName || '' }}
                        </span>
                    </template>
                </div>
            </template>
            <span v-else class="text-gray-400">{{ placeholder }}</span>
        </template>
    </Select>
</template>
