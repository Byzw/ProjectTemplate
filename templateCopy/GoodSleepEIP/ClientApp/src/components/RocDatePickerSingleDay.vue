<!-- 
 vue3-datepicker 民國年客製化，版權所有: kenghua@gmail.com 
 輸入還是使用西元年(utils.formatRocToGregorian())，但顯示時會轉換成民國年
 -->
<template>
    <VueDatePicker class="custom-datepicker" v-model="modelValue" :uid="props.id" locale="zh-TW" mode="date" :year-range="[1992, 2100]" :enable-time-picker="false" :teleport="true" :text-input="false" auto-apply :format="utils.formatRocDbString">
        <template #month-year="{ month, year, months, years, updateMonthYear }">
            <div class="roc-month-year">
                民國
                <select :value="year" @change="updateMonthYear(month, Number($event.target.value || 0))">
                    <option v-for="y in years" :key="y.value" :value="y.value">{{ y.text - 1911 }}年</option>
                </select>
                <select :value="month" @change="updateMonthYear(Number($event.target.value || 0), year)">
                    <option v-for="m in months" :key="m.value" :value="m.value">{{ m.text }}</option>
                </select>
            </div>
        </template>
        <template #calendar-header="{ index, day }">
            <div :style="index === 5 || index === 6 ? 'color: red' : ''">
                {{ ['一', '二', '三', '四', '五', '六', '日'][index] }}
            </div>
        </template>
        <template #action-preview="{ value }">
            {{ utils.formatRocDateFullString(value) }}
        </template>
    </VueDatePicker>
</template>

<script setup>
import * as utils from '@/utils/common';
import { ref } from 'vue';

// 用於接收父組件傳遞的 v-model 綁定值
const modelValue = ref('');
const props = defineProps({
    id: String
});
</script>

<style>
.roc-month-year {
    display: flex !important;
    justify-content: center !important;
    gap: 10px !important;
    font-size: 15px;
}

.dp__month_year_wrap {
    justify-content: center !important;
    width: 100% !important;
}

/* 調整輸入框的高度、邊框顏色等 */
.custom-datepicker .dp__input {
    height: 37px !important; /* 根據右邊的高度設置，視情況調整 */
    font-size: 1rem !important; /* 字體大小調整一致 */
    border: 1px solid var(--p-inputtext-border-color);
    border-radius: var(--p-inputtext-border-radius);
}

/* 調整外框顏色與樣式 */
.custom-datepicker .dp__input:focus {
    border-color: var(--p-inputtext-focus-border-color) !important; /* 聚焦時的邊框顏色 */
    outline: none;
}
</style>
