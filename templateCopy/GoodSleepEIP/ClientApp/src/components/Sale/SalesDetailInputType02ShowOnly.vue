<script setup lang="ts">
import * as models from '@/service/apiServices.schemas';
import * as gridFormatter from '@/utils/gridFormatter';
import { useToast } from 'primevue/usetoast';
import { ref, watch } from 'vue';

const props = defineProps<{
    modelValue: models.CustomerCreditDto[] | null | undefined;
    toast: ReturnType<typeof useToast>;
}>();

const CreditUsedList = ref<models.CustomerCreditDto[]>(props.modelValue ?? []);
watch(
    () => props.modelValue,
    (newVal) => {
        CreditUsedList.value = newVal ?? [];
    },
    { immediate: true }
);
</script>

<template>
    <div class="overflow-x-auto">
        <DataTable :value="CreditUsedList" dataKey="CreditSn" headerStyle="text-align: left;" showGridlines tableStyle="min-width: 30rem" class="p-datatable-sm">
            <template #empty>
                <div class="text-center py-4 text-gray-500">沒有使用購物金</div>
            </template>
            <Column field="CreditSn" header="購物金序號" style="width: 8rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.CreditSn }}</span>
                </template>
            </Column>

            <Column field="CreditName" header="購物金名稱" style="width: 8rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.CreditName }}</span>
                </template>
            </Column>
            <Column field="RemainAmount" header="可用金額" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ gridFormatter.thousandFormatter(ref(data.RemainAmount + data.UsingAmount)) }}</span>
                </template>
            </Column>
            <Column field="MaxSingleUsageAmount" header="單次最大可用金額" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ gridFormatter.thousandFormatter(ref(data.MaxSingleUsageAmount)) }}</span>
                </template>
            </Column>
            <Column field="UsingAmount" header="欲使用金額" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ gridFormatter.thousandFormatter(ref(data.UsingAmount)) }}</span>
                </template>
            </Column>
        </DataTable>
    </div>
</template>
