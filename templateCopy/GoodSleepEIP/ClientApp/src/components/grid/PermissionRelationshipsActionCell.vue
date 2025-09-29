<script setup lang="ts">
const props = defineProps(['params']);

const ClickRowButton = (buttonType: string) => {
    if (!props.params.sourceAction) {
        console.warn('無指定 sourceAction');
        return;
    }

    if (props.params.onGridRowButtonClickCallBack) {
        props.params.onGridRowButtonClickCallBack(buttonType, props.params.sourceAction, props.params.data); // 呼叫父組件 `onEditClick`
    } else console.warn('無法呼叫呼叫父組件');
};
</script>

<template>
    <div class="buttonCell">
        <button @click="ClickRowButton('edit')">
            <i class="fa-solid fa-pen-to-square text-gray-700 dark:text-gray-300"> 修改</i>
        </button>
        <button @click="ClickRowButton('del')">
            <i class="fa-regular fa-trash-can text-gray-700 dark:text-gray-300"> 刪除</i>
        </button>
    </div>
</template>

<style>
.buttonCell {
    display: flex;
    gap: 1.3em;
}

.buttonCell button {
    padding: 0.1em 0.1em 0.1em;
    white-space: nowrap;
    outline: none;
    border: 1px solid transparent;
    cursor: pointer;
    display: flex !important;
    justify-content: center;
    align-items: center;
    height: 40px;
    width: 40px;
}
</style>
