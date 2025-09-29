<script setup lang="ts">
import * as models from '@/service/apiServices.schemas';
import dayjs from 'dayjs';
import { useToast } from 'primevue/usetoast';
import { ref, watch } from 'vue';

export interface SalesDetailDto extends models.SalesOrderDetailDto {
    currentMaterialOptions?: models.CampaignDetailDto[];
}

const props = defineProps<{
    modelValue: SalesDetailDto[] | null | undefined;
    toast: ReturnType<typeof useToast>;
}>();

const salesDetails = ref<SalesDetailDto[]>(props.modelValue ?? []);
watch(
    () => props.modelValue,
    (newVal) => {
        salesDetails.value = newVal ?? [];
    },
    { immediate: true }
);
</script>

<template>
    <div class="overflow-x-auto">
        <DataTable :value="salesDetails" showGridlines scrollable scrollDirection="horizontal" scrollHeight="auto" class="p-datatable-sm" style="min-width: 1200px">
            <template #empty>
                <div class="text-center py-4 text-gray-500">無資料</div>
            </template>
            <Column field="SourceType" header="來源" style="width: 10rem; max-width: 16rem">
                <template #body="{ data, index }">
                    <span class="font-medium">
                        {{ data.CampaignName ? '[檔期]' + data.CampaignName : '[自由選商品]' }}
                    </span>
                </template>
            </Column>
            <Column field="MaterialId" header="商品名稱" style="width: 10rem">
                <template #body="{ data, index }">
                    <span class="font-medium">
                        {{ data.ItemDisplayName }}
                    </span>
                </template>
            </Column>
            <Column field="SalesUnit" header="單位" style="width: 4rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.SalesUnit }}</span>
                </template>
            </Column>
            <Column field="OrderedQuantity" header="組合數量" style="width: 5.5rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.OrderedQuantity }}</span>
                </template>
            </Column>
            <Column field="TotalBaseQuantity" header="商品數量" style="width: 5rem">
                <template #body="{ data }">
                    <span class="font-medium text-blue-600">{{ data.TotalBaseQuantity?.toLocaleString() || '0' }}</span>
                </template>
            </Column>
            <Column field="ActualPricePerSalesUnit" header="銷售價格" style="width: 5.5rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.ActualPricePerSalesUnit.toLocaleString('en-US') }}</span>
                </template>
            </Column>
            <Column field="LineAmount" header="小計" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.OrderedQuantity && data.ActualPricePerSalesUnit ? (data.OrderedQuantity * data.ActualPricePerSalesUnit).toLocaleString('en-US') : 0 }}</span>
                </template>
            </Column>
            <Column field="availableStock" header="ERP 庫存" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.AvailableStock?.toLocaleString() || 'N/A' }}</span>
                </template>
            </Column>
            <Column field="pendingQuickOrderQuantity" header="快速單占用" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.PendingQuickOrderQuantity?.toLocaleString() || 'N/A' }}</span>
                </template>
            </Column>
            <Column field="DetailRemark" header="備註" style="width: 6rem">
                <template #body="{ data, index }">
                    <span class="font-medium">
                        {{ data.DetailRemark }}
                    </span>
                </template>
            </Column>
        </DataTable>
    </div>
</template>
