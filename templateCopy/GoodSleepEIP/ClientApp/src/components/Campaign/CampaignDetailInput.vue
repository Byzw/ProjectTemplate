<script setup lang="ts">
import * as models from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { useToast } from 'primevue/usetoast';
import { onMounted, ref, watch } from 'vue';

const props = defineProps<{
    modelValue: models.CampaignDetailDto[] | null | undefined;
    toast: ReturnType<typeof useToast>;
    readonly?: boolean;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: models.CampaignDetailDto[] | null | undefined): void;
}>();

const authStore = useAuthStore();

const CampaignDetails = ref<models.CampaignDetailDto[]>(props.modelValue ?? []);

const removeCampaignDetail = (data: any) => {
    if (props.readonly) {
        props.toast.add({ severity: 'warn', summary: '只讀模式', detail: '當前為只讀模式，無法編輯', life: 3000 });
        return;
    }
    
    const index = CampaignDetails.value.indexOf(data);
    if (index > -1) {
        CampaignDetails.value.splice(index, 1);
        emit('update:modelValue', CampaignDetails.value);
    }
};

// 監聽 props 變化
watch(
    () => props.modelValue,
    (newValue) => {
        CampaignDetails.value = newValue ?? [];
    },
    { deep: true }
);

// 更新父組件的值
const updateParentValue = () => {
    if (props.readonly) return;
    
    emit('update:modelValue', CampaignDetails.value);
};

// 當數量變更時，自動重新計算銷售價格和計算後單價
const recalculatePrices = (item: models.CampaignDetailDto) => {
    if (props.readonly) return;
    
    // 🔥 簡潔邏輯：當數量變更時，根據標準單價重新計算銷售價格
    // 銷售價格 = 標準單價 × 每單位包含數量
    if (item.StandardPricePerSalesUnit && item.ItemsPerSalesUnit) {
        item.ActualPricePerSalesUnit = item.StandardPricePerSalesUnit * item.ItemsPerSalesUnit;
    }
    
    // 🔧 修正：計算後單價 = 每單位銷售價 ÷ 每單位包含數量（4位小數精度）
    if (item.ActualPricePerSalesUnit && item.ItemsPerSalesUnit) {
        item.BaseUnitPrice = Math.round((item.ActualPricePerSalesUnit / item.ItemsPerSalesUnit) * 10000) / 10000;
    } else {
        item.BaseUnitPrice = 0;
    }
    
    updateParentValue();
};

// 當銷售價格手動變更時，重新計算後單價
const onPriceManualChange = (item: models.CampaignDetailDto) => {
    if (props.readonly) return;
    
    // 🔧 修正：重新計算後單價 = 每單位銷售價 ÷ 每單位包含數量（4位小數精度）
    if (item.ActualPricePerSalesUnit && item.ItemsPerSalesUnit) {
        item.BaseUnitPrice = Math.round((item.ActualPricePerSalesUnit / item.ItemsPerSalesUnit) * 10000) / 10000;
    } else {
        item.BaseUnitPrice = 0;
    }
    
    updateParentValue();
};

</script>

<template>
    <div>
        <DataTable :value="CampaignDetails" showGridlines tableStyle="min-width: 120rem" class="p-datatable-sm" scrollable>
            <template #empty>
                <div class="text-left py-4 text-gray-500">目前尚無檔期商品，請由上方商品清單選取並加入。</div>
            </template>
            <Column field="ItemDisplayName" style="width: 10rem">
                <template #header>
                    <span v-tooltip.bottom="'檔期活動中商品的顯示名稱，可自訂。例如：咖啡4入特惠組'" class="font-semibold">
                        檔期品名 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <InputText v-if="!props.readonly" inputId="ItemDisplayName" v-model="data.ItemDisplayName" @update:modelValue="updateParentValue" fluid />
                        <span v-else class="font-medium">{{ data.ItemDisplayName || '-' }}</span>
                    </div>
                </template>
            </Column>
            <Column field="MaterialName" style="width: 10rem">
                <template #header>
                    <span v-tooltip.bottom="'商品主檔中的商品名稱（不可修改）'" class="font-semibold">
                        商品名稱 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <span class="font-medium">{{ data.MaterialName || '-' }}</span>
                    </div>
                </template>
            </Column>
            <Column field="SalesUnit" style="width: 5rem">
                <template #header>
                    <span v-tooltip.bottom="'銷售單位的名稱或描述。例如：組、箱、包、等'" class="font-semibold">
                        銷售單位 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <InputText v-if="!props.readonly" inputId="SalesUnit" v-model="data.SalesUnit" @update:modelValue="updateParentValue" fluid />
                        <span v-else class="font-medium">{{ data.SalesUnit || '-' }}</span>
                    </div>
                </template>
            </Column>
            <Column field="ItemsPerSalesUnit" style="width: 8rem">
                <template #header>
                    <span v-tooltip.bottom="'每1個「銷售單位」包含多少個基礎商品。例如：1組包含24瓶，則填入24'" class="font-semibold">
                        每單位包含數量 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <InputNumber
                            v-if="!props.readonly"
                            v-model="data.ItemsPerSalesUnit"
                            :min="1"
                            :style="{ width: '100%' }"
                            :inputStyle="{ width: '30px' }"
                            :useInput="true"
                            :allowEmpty="false"
                            :minFractionDigits="0"
                            :step="1"
                            showButtons
                            buttonLayout="horizontal"
                            incrementButtonIcon="pi pi-plus"
                            decrementButtonIcon="pi pi-minus"
                            @update:modelValue="() => { recalculatePrices(data); }"
                        />
                        <span v-else class="font-medium">{{ data.ItemsPerSalesUnit?.toLocaleString() || '0' }}</span>
                    </div>
                </template>
            </Column>
            <Column field="QuantityOfSalesUnit" style="width: 8rem">
                <template #header>
                    <span v-tooltip.bottom="'本明細要販售多少個「銷售單位」。例如：要賣3組，則填入3'" class="font-semibold">
                        銷售單位數 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <InputNumber
                            v-if="!props.readonly"
                            v-model="data.QuantityOfSalesUnit"
                            :min="1"
                            :style="{ width: '100%' }"
                            :inputStyle="{ width: '30px' }"
                            :useInput="true"
                            :allowEmpty="false"
                            :minFractionDigits="0"
                            :step="1"
                            showButtons
                            buttonLayout="horizontal"
                            incrementButtonIcon="pi pi-plus"
                            decrementButtonIcon="pi pi-minus"
                            @update:modelValue="() => { recalculatePrices(data); }"
                        />
                        <span v-else class="font-medium">{{ data.QuantityOfSalesUnit?.toLocaleString() || '0' }}</span>
                    </div>
                </template>
            </Column>
            <Column field="StandardPricePerSalesUnit" style="width: 8rem">
                <template #header>
                    <span v-tooltip.bottom="'單一基礎商品的標準售價（來自ERP商品主檔，不可修改）。例如：每瓶20元'" class="font-semibold">
                        基礎商品標準價 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <span class="font-medium">{{ data.StandardPricePerSalesUnit?.toLocaleString() || '0' }}</span>
                    </div>
                </template>
            </Column>
            <Column field="ActualPricePerSalesUnit" style="width: 8rem">
                <template #header>
                    <span v-tooltip.bottom="'每1個「銷售單位」的實際銷售價格（基礎商品標準價×每單位包含數量，可手動調整）。例如：每1組的實際售價。修改數量時會自動重新計算'" class="font-semibold">
                        每單位銷售價 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <InputNumber
                            v-if="!props.readonly"
                            v-model="data.ActualPricePerSalesUnit"
                            :min="0"
                            :style="{ width: '100%' }"
                            :inputStyle="{ width: '50px' }"
                            :useInput="true"
                            :allowEmpty="false"
                            :minFractionDigits="0"
                            :step="1"
                            showButtons
                            buttonLayout="horizontal"
                            incrementButtonIcon="pi pi-plus"
                            decrementButtonIcon="pi pi-minus"
                            @update:modelValue="() => { onPriceManualChange(data); }"
                        />
                        <span v-else class="font-medium">{{ data.ActualPricePerSalesUnit?.toLocaleString() || '0' }}</span>
                    </div>
                </template>
            </Column>
            <Column field="BaseUnitPrice" style="width: 8rem">
                <template #header>
                    <span v-tooltip.bottom="'每個基礎商品的實際售價（自動計算，顯示2位小數）= 每單位銷售價 ÷ 每單位包含數量。例如：每組480元÷24瓶=每瓶20.00元'" class="font-semibold">
                        每件商品單價 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <span class="font-medium">{{ (data.BaseUnitPrice || 0).toFixed(2) }}</span>
                    </div>
                </template>
            </Column>
            <Column field="CampaignExpectedSalesQuantityOfSalesUnit" style="width: 8rem">
                <template #header>
                    <span v-tooltip.bottom="'預估銷售的「銷售單位」數量。例如：預估可賣出5組，則填入5'" class="font-semibold">
                        預估銷售數量 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <InputNumber
                            v-if="!props.readonly"
                            v-model="data.CampaignExpectedSalesQuantityOfSalesUnit"
                            :min="0"
                            :style="{ width: '100%' }"
                            :inputStyle="{ width: '30px' }"
                            :useInput="true"
                            :allowEmpty="false"
                            :minFractionDigits="0"
                            :step="1"
                            showButtons
                            buttonLayout="horizontal"
                            incrementButtonIcon="pi pi-plus"
                            decrementButtonIcon="pi pi-minus"
                            @update:modelValue="updateParentValue"
                        />
                        <span v-else class="font-medium">{{ data.CampaignExpectedSalesQuantityOfSalesUnit?.toLocaleString() || '0' }}</span>
                    </div>
                </template>
            </Column>
            <Column field="DetailRemark" style="width: 8rem">
                <template #header>
                    <span v-tooltip.bottom="'此檔期商品的額外備註說明'" class="font-semibold">
                        備註 <i class="pi pi-info-circle ml-1 text-xs"></i>
                    </span>
                </template>
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center">
                        <InputText v-if="!props.readonly" inputId="DetailRemark" v-model="data.DetailRemark" @update:modelValue="updateParentValue" fluid />
                        <span v-else class="font-medium">{{ data.DetailRemark || '-' }}</span>
                    </div>
                </template>
            </Column>

            <Column header="操作" style="width: 5rem">
                <template #body="{ data }">
                    <div class="min-h-[2.5rem] flex items-center justify-center">
                        <Button v-if="!props.readonly" icon="pi pi-trash" class="p-button-text" @click="removeCampaignDetail(data)" />
                        <span v-else>&nbsp;</span>
                    </div>
                </template>
            </Column>
        </DataTable>
    </div>
</template>
