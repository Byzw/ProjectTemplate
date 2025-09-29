<!-- 
 繼承自 PrimeVue DatePicker
 專門處理後端 C# DateTime? 轉換成前端 string | null 的情況，將 string | null | undefined 轉換為 Date | null 供 PrimeVue DatePicker 使用
 支援日期+時間選擇，輸出為完整的 ISO 字符串格式
 可透過 showTime 參數控制是否顯示時間選擇器
 
 版權所有: kenghua@gmail.com 
 -->
<template>

    <div class="flex w-full">
        <div :class="showTime ? 'flex-1' : 'w-full'">
            <DatePicker ref="datePicker" :modelValue="internalDate" @update:modelValue="onDateChange"
                :class="props.class" :inputStyle="inputStyle" :invalid="invalid" showIcon :placeholder="placeholder"
                :dateFormat="dateFormat" :manualInput="manualInput" :hourFormat="hourFormat" :disabled="disabled"
                :readonly="readonly" class="enhanced-date-time-selector w-full" @focus="onDatePickerFocus" />
        </div>
        <div v-if="showTime" class="w-32 ml-2">
            <DatePicker ref="timePicker" :modelValue="internalTime" @update:modelValue="onTimeChange" timeOnly
                :hourFormat="hourFormat" showIcon :disabled="disabled" :readonly="readonly" placeholder="時間"
                class="w-full" @focus="onTimePickerFocus" />
        </div>
    </div>

</template>

<script setup lang="ts">
import { computed, ref, nextTick, onMounted } from 'vue';

interface Props {
    modelValue: string | null | undefined;
    showTime?: boolean;
    class?: string;
    invalid?: boolean;
    placeholder?: string;
    dateFormat?: string;
    inputStyle?: object;
    manualInput?: boolean;
    hourFormat?: '12' | '24';
    disabled?: boolean;
    readonly?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
    showTime: false,
    placeholder: '請選擇日期',
    dateFormat: 'yy 年 m 月 d 日',
    manualInput: true,
    hourFormat: '24',
    disabled: false,
    readonly: false
});

const emit = defineEmits<{
    'update:modelValue': [value: string | null | undefined];
}>();

// 組件引用
const datePicker = ref();
const timePicker = ref();

// 內部狀態，用於存儲當前的日期和時間部分
const currentDate = ref<Date | null>(null);
const currentTime = ref<Date | null>(null);

// 將 string | null 轉換為 Date | null 供 PrimeVue DatePicker 使用 (日期部分)
const internalDate = computed(() => {
    if (!props.modelValue) return null;

    try {
        const date = new Date(props.modelValue);
        if (isNaN(date.getTime())) return null;
        currentDate.value = date;
        return date;
    } catch {
        return null;
    }
});

// 將 string | null 轉換為 Date | null 供 PrimeVue DatePicker 使用 (時間部分)
const internalTime = computed(() => {
    if (!props.modelValue || !props.showTime) return null;

    try {
        const date = new Date(props.modelValue);
        if (isNaN(date.getTime())) return null;
        currentTime.value = date;
        return date;
    } catch {
        return null;
    }
});

// 合併日期和時間並輸出
function combineDateTime(dateValue: Date | null, timeValue: Date | null): string | null {
    if (!dateValue) return null;

    // 使用日期部分
    const year = dateValue.getFullYear();
    const month = dateValue.getMonth();
    const day = dateValue.getDate();

    // 使用時間部分，如果不顯示時間或沒有時間則使用 00:00:00
    let hours = 0;
    let minutes = 0;
    let seconds = 0;

    if (props.showTime && timeValue) {
        hours = timeValue.getHours();
        minutes = timeValue.getMinutes();
        seconds = timeValue.getSeconds();
    }

    // 創建新的日期時間對象
    const combinedDate = new Date(year, month, day, hours, minutes, seconds);

    // 如果不顯示時間，返回只有日期的格式（但仍保持 ISO 格式兼容性）
    return combinedDate.toISOString();
}

// 處理日期變更事件
function onDateChange(date: Date | Date[] | (Date | null)[] | null | undefined) {
    if (date instanceof Date && !isNaN(date.getTime())) {
        currentDate.value = date;
        const combined = combineDateTime(currentDate.value, props.showTime ? currentTime.value : null);
        emit('update:modelValue', combined);
    } else {
        currentDate.value = null;
        emit('update:modelValue', null);
    }
}

// 處理時間變更事件
function onTimeChange(time: Date | Date[] | (Date | null)[] | null | undefined) {
    if (!props.showTime) return;

    if (time instanceof Date && !isNaN(time.getTime())) {
        currentTime.value = time;
        const combined = combineDateTime(currentDate.value, currentTime.value);
        emit('update:modelValue', combined);
    } else {
        currentTime.value = null;
        // 如果只是清除時間，保留日期部分但時間設為 00:00:00
        if (currentDate.value) {
            const combined = combineDateTime(currentDate.value, null);
            emit('update:modelValue', combined);
        }
    }
}

// 自動選取數字部分的函數
function selectNumberPart(input: HTMLInputElement, clickPosition: number) {
    const value = input.value;
    if (!value) return;

    // 定義數字區間的正則表達式
    const numberRegex = /\d+/g;
    let match;

    while ((match = numberRegex.exec(value)) !== null) {
        const start = match.index;
        const end = match.index + match[0].length;

        // 如果點擊位置在這個數字範圍內，就選取這個數字
        if (clickPosition >= start && clickPosition <= end) {
            input.setSelectionRange(start, end);
            return;
        }
    }
}

// 處理日期選擇器焦點事件
function onDatePickerFocus(event: Event) {
    nextTick(() => {
        const input = datePicker.value?.$el?.querySelector('input');
        if (input) {
            setupInputHandlers(input);
        }
    });
}

// 處理時間選擇器焦點事件
function onTimePickerFocus(event: Event) {
    nextTick(() => {
        const input = timePicker.value?.$el?.querySelector('input');
        if (input) {
            setupInputHandlers(input);
        }
    });
}

// 設置輸入框的事件處理器
function setupInputHandlers(input: HTMLInputElement) {
    // 移除之前的事件監聽器以避免重複綁定
    input.removeEventListener('click', handleInputClick);
    input.removeEventListener('keydown', handleInputKeydown);
    input.removeEventListener('input', handleInputChange);

    // 添加點擊事件監聽器
    input.addEventListener('click', handleInputClick);
    // 添加鍵盤事件監聽器
    input.addEventListener('keydown', handleInputKeydown);
    // 添加輸入事件監聽器（額外保護）
    input.addEventListener('input', handleInputChange);
}

// 處理輸入框點擊事件
function handleInputClick(event: MouseEvent) {
    const input = event.target as HTMLInputElement;
    const clickPosition = input.selectionStart || 0;

    // 延遲執行以確保光標位置已設置
    setTimeout(() => {
        selectNumberPart(input, clickPosition);
    }, 10);
}

// 處理輸入變更事件（額外保護，過濾非數字字符）
function handleInputChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const originalValue = input.value;

    // 保留原始的光標位置
    const cursorPosition = input.selectionStart || 0;

    // 只保留數字和分隔符（年、月、日、時、分、秒的分隔符）
    // 這個正則表達式允許數字以及常見的日期時間分隔符
    const filteredValue = originalValue.replace(/[^\d年月日時分秒\s:/-]/g, '');

    // 如果值有變化，更新輸入框並調整光標位置
    if (filteredValue !== originalValue) {
        input.value = filteredValue;

        // 調整光標位置，考慮被移除的字符
        const removedChars = originalValue.length - filteredValue.length;
        const newCursorPosition = Math.max(0, cursorPosition - removedChars);

        // 設置新的光標位置
        input.setSelectionRange(newCursorPosition, newCursorPosition);

        // 觸發 input 事件讓 Vue 知道值已改變
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }
}

// 處理鍵盤事件，增強導航體驗
function handleInputKeydown(event: KeyboardEvent) {
    const input = event.target as HTMLInputElement;

    // 定義允許的按鍵
    const allowedKeys = [
        'Tab', 'Shift', 'Backspace', 'Delete', 'ArrowLeft', 'ArrowRight',
        'ArrowUp', 'ArrowDown', 'Home', 'End', 'Enter', 'Escape'
    ];

    // 檢查是否為數字鍵（0-9）
    const isNumber = /^[0-9]$/.test(event.key);

    // 檢查是否為 Ctrl/Cmd 組合鍵（如 Ctrl+A, Ctrl+C, Ctrl+V 等）
    const isControlKey = event.ctrlKey || event.metaKey;

    // 如果不是允許的按鍵、不是數字、也不是控制組合鍵，則阻止輸入
    if (!allowedKeys.includes(event.key) && !isNumber && !isControlKey) {
        event.preventDefault();
        return;
    }

    // 如果按下 Tab 鍵，自動選取下一個數字部分
    if (event.key === 'Tab' && !event.shiftKey) {
        event.preventDefault();
        const currentSelectionEnd = input.selectionEnd || 0;
        const value = input.value;

        // 找到當前選取範圍結束位置之後的下一個數字
        const remainingText = value.substring(currentSelectionEnd);
        const nextNumberMatch = remainingText.match(/\d+/);

        if (nextNumberMatch && nextNumberMatch.index !== undefined) {
            const nextStart = currentSelectionEnd + nextNumberMatch.index;
            const nextEnd = nextStart + nextNumberMatch[0].length;
            input.setSelectionRange(nextStart, nextEnd);
        } else {
            // 如果沒有下一個數字，選取第一個數字
            const firstNumberMatch = value.match(/\d+/);
            if (firstNumberMatch && firstNumberMatch.index !== undefined) {
                const firstStart = firstNumberMatch.index;
                const firstEnd = firstStart + firstNumberMatch[0].length;
                input.setSelectionRange(firstStart, firstEnd);
            }
        }
    }

    // 如果按下 Shift+Tab，選取上一個數字部分
    if (event.key === 'Tab' && event.shiftKey) {
        event.preventDefault();
        const currentSelection = input.selectionStart || 0;
        const value = input.value;

        // 找到當前選取位置之前的上一個數字
        const precedingText = value.substring(0, currentSelection);
        const numberMatches = [...precedingText.matchAll(/\d+/g)];

        if (numberMatches.length > 0) {
            const lastMatch = numberMatches[numberMatches.length - 1];
            const prevStart = lastMatch.index!;
            const prevEnd = prevStart + lastMatch[0].length;
            input.setSelectionRange(prevStart, prevEnd);
        } else {
            // 如果沒有上一個數字，選取最後一個數字
            const allNumberMatches = [...value.matchAll(/\d+/g)];
            if (allNumberMatches.length > 0) {
                const lastMatch = allNumberMatches[allNumberMatches.length - 1];
                const lastStart = lastMatch.index!;
                const lastEnd = lastStart + lastMatch[0].length;
                input.setSelectionRange(lastStart, lastEnd);
            }
        }
    }
}

// 組件掛載後設置事件監聽器
onMounted(() => {
    nextTick(() => {
        // 為日期選擇器設置事件處理器
        if (datePicker.value) {
            const dateInput = datePicker.value.$el?.querySelector('input');
            if (dateInput) {
                setupInputHandlers(dateInput);
            }
        }

        // 為時間選擇器設置事件處理器
        if (timePicker.value && props.showTime) {
            const timeInput = timePicker.value.$el?.querySelector('input');
            if (timeInput) {
                setupInputHandlers(timeInput);
            }
        }
    });
});
</script>
