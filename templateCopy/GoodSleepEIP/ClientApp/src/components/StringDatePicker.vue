<!-- 
 繼承自 PrimeVue DatePicker
 專門處理後端 C# DateTime? 轉換成前端 string | null 的情況，將 string | null | undefined 轉換為 Date | null 供 PrimeVue DatePicker 使用
 
 版權所有: kenghua@gmail.com 
 -->
<template>
    <DatePicker :modelValue="internalDate" @update:modelValue="onDateChange" :class="props.class"
        :inputStyle="inputStyle" :invalid="invalid" showIcon :placeholder="placeholder" :dateFormat="dateFormat"
        :manualInput="manualInput" :showTime="showTime" :timeOnly="timeOnly" :hourFormat="hourFormat"
        :disabled="disabled" :readonly="readonly" />
</template>

<script setup lang="ts">
import { computed } from 'vue';

interface Props {
    modelValue: string | null | undefined;
    class?: string;
    invalid?: boolean;
    placeholder?: string;
    dateFormat?: string;
    inputStyle?: object;
    manualInput?: boolean;
    showTime?: boolean;
    timeOnly?: boolean;
    hourFormat?: '12' | '24';
    disabled?: boolean;
    readonly?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
    placeholder: '請選擇日期',
    dateFormat: 'yy 年 mm 月 dd 日',
    manualInput: true,
    showTime: false,
    timeOnly: false,
    hourFormat: '24',
    disabled: false,
    readonly: false
});

const emit = defineEmits<{
    'update:modelValue': [value: string | null | undefined];
}>();

// 將 string | null 轉換為 Date | null 供 PrimeVue DatePicker 使用
const internalDate = computed(() => {
    if (!props.modelValue) return null;

    try {
        // 嘗試解析 ISO 字串或其他日期格式
        const date = new Date(props.modelValue);
        return isNaN(date.getTime()) ? null : date;
    } catch {
        return null;
    }
});

// 處理日期變更事件
function onDateChange(date: Date | Date[] | (Date | null)[] | null | undefined) {
    if (date instanceof Date && !isNaN(date.getTime())) {
        // 轉換為 ISO 字串格式 (C# DateTime 可以正確解析)
        emit('update:modelValue', date.toISOString());
    } else {
        emit('update:modelValue', null);
    }
}
</script>