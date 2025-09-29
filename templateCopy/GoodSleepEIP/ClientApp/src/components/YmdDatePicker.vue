<!-- 
 vue3/primevue-YmdDatePicker 因應正航 T357 使用習慣，可快速輸入 yyyymmdd 選擇日期。
 版權所有: kenghua@gmail.com 
  -->
<template>
    <DatePicker :modelValue="calendarDate" @update:modelValue="onCalendarChange" :inputStyle="{ fontSize: '1rem' }"
        :class="{ 'p-invalid': invalid }" showIcon placeholder="快速輸入: yyyymmdd" dateFormat="yy 年 mm 月 dd 日"
        :manualInput="true" @input="onInput" />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import dayjs from 'dayjs';

const props = defineProps<{
    modelValue: number | null,
    invalid?: boolean
}>();
const emit = defineEmits(['update:modelValue']);

const calendarDate = computed(() =>
    props.modelValue && /^\d{8}$/.test(props.modelValue.toString())
        ? dayjs(props.modelValue.toString(), 'YYYYMMDD').toDate()
        : null
);

function onInput(e: Event) {
    const val = (e.target as HTMLInputElement).value;
    // 只在 8 碼時才 emit，否則不動 v-model
    if (/^\d{8}$/.test(val)) {
        emit('update:modelValue', parseInt(val));
    }
}

function onCalendarChange(val: Date | Date[] | (Date | null)[] | null | undefined) {
    if (val instanceof Date) {
        emit('update:modelValue', parseInt(dayjs(val).format('YYYYMMDD')));
    } else {
        emit('update:modelValue', null);
    }
}
</script>

<style scoped>
.p-invalid {
    border-color: var(--p-inputtext-invalid-border-color, #f87171) !important;
}
</style>