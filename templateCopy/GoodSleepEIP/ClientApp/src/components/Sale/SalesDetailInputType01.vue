<script setup lang="ts">
import { apiService } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import { useToast } from 'primevue/usetoast';
import { computed, onMounted, ref, watch } from 'vue';

export interface SalesDetailDto extends models.SalesOrderDetailDto {
    currentMaterialOptions?: models.CampaignDetailDto[];
}

const props = defineProps<{
    modelValue: SalesDetailDto[] | null | undefined;
    isEditMode?: boolean;
    toast: ReturnType<typeof useToast>;
    parentCampaignList: models.CampaignDto[] | null | undefined;
    defaultCampaignId?: string | null;
}>();

// ✨✨✨✨✨✨✨✨✨✨✨✨ 變數(資料等)區 ✨✨✨✨✨✨✨✨✨✨✨✨
const parameterList = ref<models.Parameter[]>([]);
const salesDetails = ref<SalesDetailDto[]>(props.modelValue ?? []);
const searchMaterialOptions = ref<models.CampaignDetailDto[]>([]);

interface SalesDetailSourceOption {
    sourceTypeValue: string; // '01' (檔期) or '02' (自由選商品)
    fullLabel: string; // 顯示的文字
    campaignId: string | null;
    originalParameterLabel: string; // "檔期" or "自由選商品"
}

onMounted(async () => {
    try {
        parameterList.value = await apiService.callApi(apiService.postApiWebGetListParameters, ['SalesDetailSourceType']);
        const materials = await apiService.callApi(apiService.getApiT8FetchMaterialRecord);
        searchMaterialOptions.value = materials as models.CampaignDetailDto[];
    } catch (error) {
        props.toast.add({ severity: 'error', summary: '取得來源類型錯誤', detail: error, life: 3000 });
    }
});

// 更新父組件的值
const updateParentValue = () => {
    emit('update:modelValue', salesDetails.value);
};

// ✨✨✨✨✨✨✨✨✨✨✨✨ 事件定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 透過 defineEmits 宣告本元件可以向父元件發送哪些事件（事件型別安全）
// 這裡定義 update:modelValue 事件，通常用於 v-model 雙向綁定
// 當 salesDetails 資料有異動時，會 emit 該事件將最新的明細資料同步給父元件
const emit = defineEmits<{
    (e: 'update:modelValue', value: SalesDetailDto[] | null | undefined): void;
}>();

// 監聽 defaultCampaignId 以自動載入/切換檔期商品
watch(
    () => props.defaultCampaignId,
    async (newCampaignId, oldCampaignId) => {
        const isValidNewCampaign = newCampaignId && newCampaignId !== '00000000-0000-0000-0000-000000000000';
        if (props.isEditMode && isValidNewCampaign) {
            // 修改補全時，展示當前訂單商品
            salesDetails.value = props.modelValue ?? [];
            const campaignMaterialsResult = (await apiService.callApi(apiService.postApiWebGetCampaignDetailList, { CampaignId: props.defaultCampaignId })) as models.CampaignDetailDto[];
            const materials = (await apiService.callApi(apiService.getApiT8FetchMaterialRecord)) as models.CampaignDetailDto[];
            salesDetails.value.forEach((item) => (item.SourceType == '01' ? (item.currentMaterialOptions = campaignMaterialsResult) : (item.currentMaterialOptions = materials)));

            return;
        }

        // 初始化(immediate:true)時，若父元件已透過v-model提供資料，則不自動載入，以v-model為準
        if (oldCampaignId === undefined && props.modelValue && props.modelValue.length > 0) {
            return;
        }

        if (isValidNewCampaign) {
            const currentLoadedCampaignId = salesDetails.value.length > 0 && salesDetails.value[0].SourceType === '01' ? salesDetails.value[0].CampaignId : null;

            // 只有當新選的檔期與目前已載入的不同，或目前明細為空時，才重新載入
            if (newCampaignId !== currentLoadedCampaignId) {
                try {
                    salesDetails.value = []; // 清空現有明細
                    const campaignMaterialsResult = (await apiService.callApi(apiService.postApiWebGetCampaignDetailList, { CampaignId: newCampaignId })) as models.CampaignDetailDto[];
                    if (campaignMaterialsResult && campaignMaterialsResult.length > 0) {
                        const defaultCampaign = props.parentCampaignList?.find((c) => c.CampaignId === newCampaignId);
                        const newDetails = campaignMaterialsResult.map((material, index) => {
                            // ✅ 使用檔期定義的正確數值
                            const orderedQuantity = material.QuantityOfSalesUnit ?? 1; // 檔期定義的銷售單位數
                            const itemsPerSalesUnit = material.ItemsPerSalesUnit ?? 1; // 每單位包含數量
                            const actualPrice = material.ActualPricePerSalesUnit ?? 0; // 每單位銷售價
                            const standardPrice = material.StandardPricePerSalesUnit ?? 0; // 每單位標準價

                            // ✅ 正確計算衍生欄位
                            const lineAmount = orderedQuantity * actualPrice; // 小計 = 銷售單位數 × 每單位銷售價
                            const totalBaseQuantity = orderedQuantity * itemsPerSalesUnit; // 總基礎數量 = 銷售單位數 × 每單位包含數量
                            const calculatedBaseUnitPrice = itemsPerSalesUnit > 0 ? actualPrice / itemsPerSalesUnit : 0; // 每件商品單價 = 每單位銷售價 ÷ 每單位包含數量

                            return {
                                RowNo: index + 1,
                                SourceType: '01', // 檔期
                                CampaignId: newCampaignId,
                                SourceId: material.DetailId,
                                MaterialId: material.MaterialId ?? null,
                                ItemDisplayName: material.ItemDisplayName ?? material.MaterialName ?? null,
                                SalesUnit: material.SalesUnit,
                                ItemsPerSalesUnit: itemsPerSalesUnit, // ✅ 新增：每單位包含數量
                                OrderedQuantity: orderedQuantity, // ✅ 修正：使用檔期定義的銷售單位數
                                StandardPricePerSalesUnit: standardPrice,
                                ActualPricePerSalesUnit: actualPrice,
                                LineAmount: lineAmount, // ✅ 修正：正確計算小計
                                TotalBaseQuantity: totalBaseQuantity, // ✅ 新增：總基礎數量
                                CalculatedBaseUnitPrice: calculatedBaseUnitPrice, // ✅ 新增：每件商品單價
                                WarehouseId: null,
                                IsGift: false,
                                DetailRemark: null,
                                CampaignName: defaultCampaign?.CampaignName ?? null,
                                AvailableStock: material.TotalStkQty ?? 0,
                                PendingQuickOrderQuantity: material.PendingQuickOrderQuantity ?? 0,
                                currentMaterialOptions: [...campaignMaterialsResult]
                            } as SalesDetailDto;
                        });
                        salesDetails.value = newDetails;
                    }
                    updateParentValue(); // 通知父元件
                } catch (error) {
                    props.toast.add({ severity: 'error', summary: '載入新檔期商品失敗', detail: String(error), life: 3000 });
                    salesDetails.value = []; // 錯誤時也清空
                    updateParentValue();
                }
            }
        } else {
            // newCampaignId 為空或無效 (檔期被清除)
            if (salesDetails.value.length > 0) {
                // 如果原本有資料才清空並通知
                salesDetails.value = [];
                updateParentValue();
            }
        }
    },
    { immediate: true } // 初始載入時執行一次
);

// 🌟 來源下拉選單觸發事件 (現在接收 fullLabel)
const onSourceTypeUpdate = async (newLabel: string | null, data: SalesDetailDto, index: number) => {
    // 根據 newLabel 找出完整的選項物件
    const selectedOption = newLabel ? searchSourceTypeOptions.value.find((opt) => opt.fullLabel === newLabel) : null;

    // 清空舊值 & 選項
    data.CampaignId = null;
    data.MaterialId = null;
    data.SourceId = null;
    data.ItemDisplayName = null;
    data.SalesUnit = null;
    data.ItemsPerSalesUnit = 1; // ✅ 重置為預設值
    data.OrderedQuantity = 1; // ✅ 重置為預設值
    data.ActualPricePerSalesUnit = 0;
    data.StandardPricePerSalesUnit = 0;
    data.LineAmount = 0; // ✅ 重置小計
    data.TotalBaseQuantity = 0; // ✅ 重置總基礎數量
    data.BaseUnitPrice = 0; // ✅ 重置每件商品單價
    data.AvailableStock = null; // ✅ 重置庫存
    data.PendingQuickOrderQuantity = null; // ✅ 重置快速單占用量
    data.currentMaterialOptions = [];

    try {
        if (selectedOption) {
            data.SourceType = selectedOption.sourceTypeValue; // 🌟 設定 SourceType
            data.CampaignId = selectedOption.campaignId; // 🌟 設定 CampaignId

            if (selectedOption.sourceTypeValue === '01' && selectedOption.campaignId) {
                // 直接在此載入檔期商品清單
                try {
                    const result = await apiService.callApi(apiService.postApiWebGetCampaignDetailList, { CampaignId: selectedOption.campaignId });
                    data.currentMaterialOptions = result as models.CampaignDetailDto[];
                } catch (error) {
                    console.error(`抓取檔期 ${selectedOption.campaignId} 的商品資料失敗 (行: ${data.RowNo}):`, error);
                    props.toast.add({ severity: 'error', summary: '錯誤', detail: `行 ${data.RowNo}: 無法載入檔期商品`, life: 3000 });
                    data.currentMaterialOptions = [];
                }
            } else if (selectedOption.sourceTypeValue === '02') {
                data.currentMaterialOptions = searchMaterialOptions.value;
            }
        } else {
            data.SourceType = null;
            data.CampaignId = null;
        }
    } catch (error) {
        props.toast.add({ severity: 'error', summary: '來源選項選擇錯誤', detail: error, life: 3000 });
    }
    updateParentValue(); // 觸發父組件更新
};

// ✨✨✨✨✨✨✨✨✨✨✨✨ [計算](computed)方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 初始化來源下拉選單組合，所有檔期 + 自由選商品
const searchSourceTypeOptions = computed<SalesDetailSourceOption[]>(() => {
    const options: SalesDetailSourceOption[] = [];
    const campaignSourceTypeParam = parameterList.value.find((p) => p.Category === 'SalesDetailSourceType' && p.Code === '01');
    const paramMaterialSelect = parameterList.value.find((p) => p.Category === 'SalesDetailSourceType' && p.Code === '02');

    if (campaignSourceTypeParam && props.parentCampaignList) {
        props.parentCampaignList.forEach((campaign) => {
            if (campaign.CampaignId && campaign.CampaignName) {
                options.push({
                    fullLabel: `[${campaignSourceTypeParam.Description}] ${campaign.CampaignName}`,
                    sourceTypeValue: campaignSourceTypeParam.Code!,
                    campaignId: campaign.CampaignId,
                    originalParameterLabel: campaignSourceTypeParam.Description!
                });
            }
        });
    }

    if (paramMaterialSelect) {
        options.push({
            fullLabel: `[${paramMaterialSelect.Description}]`,
            sourceTypeValue: paramMaterialSelect.Code!,
            campaignId: null,
            originalParameterLabel: paramMaterialSelect.Description!
        });
    }
    return options;
});

// ✨✨✨ 新增輔助函式 ✨✨✨
const findSourceOption = (data: SalesDetailDto) => {
    if (data.SourceType == '02') {
        const option = searchSourceTypeOptions.value.find((opt) => opt.sourceTypeValue === '02');
        return option || null;
    }
    if (!data.SourceType && !data.CampaignId) return null;
    const option = searchSourceTypeOptions.value.find((opt) => opt.sourceTypeValue === data.SourceType && opt.campaignId === data.CampaignId);
    return option || null;
};

// 輔助函式：根據 data 找出對應的 fullLabel
const getSourceLabel = (data: SalesDetailDto) => {
    const option = findSourceOption(data);
    return option ? option.fullLabel : null;
};

// 🌟 商品選擇觸發事件
const onMaterialSelect = (selectedValue: string | null, rowData: SalesDetailDto, index: number) => {
    // 先清空可能受影響的欄位
    rowData.ItemDisplayName = null;
    rowData.SalesUnit = null;
    rowData.ItemsPerSalesUnit = 1; // ✅ 重置為預設值
    rowData.OrderedQuantity = 1; // ✅ 重置為預設值
    rowData.ActualPricePerSalesUnit = 0;
    rowData.StandardPricePerSalesUnit = 0;
    rowData.LineAmount = 0; // ✅ 重置小計
    rowData.TotalBaseQuantity = 0; // ✅ 重置總基礎數量
    rowData.BaseUnitPrice = 0; // ✅ 重置每件商品單價
    rowData.AvailableStock = null; // ✅ 重置庫存
    rowData.PendingQuickOrderQuantity = null; // ✅ 重置快速單占用量

    // 根據 SourceType，有條件地清空 CampaignDetailId 或 MaterialId, 並準備重新賦值
    if (rowData.SourceType === '01') {
        rowData.SourceId = null;
        // MaterialId 會在找到 selectedMaterial 後被設定
    } else if (rowData.SourceType === '02') {
        rowData.MaterialId = null;
        rowData.SourceId = null; // 自由選商品沒有 CampaignDetailId
    } else {
        rowData.SourceId = null;
        rowData.MaterialId = null;
    }

    if (selectedValue && rowData.currentMaterialOptions && rowData.currentMaterialOptions.length > 0) {
        let selectedMaterial: models.CampaignDetailDto | undefined;

        if (rowData.SourceType === '01') {
            rowData.SourceId = selectedValue; // selectedValue is DetailId from CampaignDetail
            selectedMaterial = rowData.currentMaterialOptions.find((m) => m.DetailId === selectedValue);
            if (selectedMaterial) {
                // ✅ 使用檔期定義的正確數值
                const orderedQuantity = selectedMaterial.QuantityOfSalesUnit ?? 1; // 檔期定義的銷售單位數
                const itemsPerSalesUnit = selectedMaterial.ItemsPerSalesUnit ?? 1; // 每單位包含數量
                const actualPrice = selectedMaterial.ActualPricePerSalesUnit ?? 0; // 每單位銷售價
                const standardPrice = selectedMaterial.StandardPricePerSalesUnit ?? 0; // 每單位標準價

                // ✅ 設定基本資訊
                rowData.MaterialId = selectedMaterial.MaterialId ?? null; // 從檔期商品帶出 MaterialId
                rowData.ItemDisplayName = selectedMaterial.ItemDisplayName ?? selectedMaterial.MaterialName ?? null;
                rowData.SalesUnit = selectedMaterial.SalesUnit;
                rowData.ItemsPerSalesUnit = itemsPerSalesUnit; // ✅ 新增：每單位包含數量
                rowData.OrderedQuantity = orderedQuantity; // ✅ 修正：使用檔期定義的銷售單位數
                rowData.StandardPricePerSalesUnit = standardPrice;
                rowData.ActualPricePerSalesUnit = actualPrice;
                rowData.AvailableStock = selectedMaterial.TotalStkQty ?? 0;

                // ✅ 自動計算衍生欄位
                rowData.LineAmount = orderedQuantity * actualPrice; // 小計 = 銷售單位數 × 每單位銷售價
                rowData.TotalBaseQuantity = orderedQuantity * itemsPerSalesUnit; // 總基礎數量 = 銷售單位數 × 每單位包含數量
                rowData.BaseUnitPrice = itemsPerSalesUnit > 0 ? actualPrice / itemsPerSalesUnit : 0; // 每件商品單價

                getPendingQuickOrderQuantity(rowData.MaterialId, rowData, index);
            }
        } else if (rowData.SourceType === '02') {
            rowData.MaterialId = selectedValue; // selectedValue is MaterialId from general material list
            getPendingQuickOrderQuantity(rowData.MaterialId, rowData, index);
            selectedMaterial = rowData.currentMaterialOptions.find((m) => m.MaterialId === selectedValue);
            if (selectedMaterial) {
                rowData.ItemDisplayName = selectedMaterial.ItemDisplayName ?? selectedMaterial.MaterialName ?? null;
                rowData.SalesUnit = selectedMaterial.UnitId;
                rowData.ActualPricePerSalesUnit = selectedMaterial.StandardPrice ?? 0;
                rowData.StandardPricePerSalesUnit = selectedMaterial.StandardPrice ?? 0;
                rowData.AvailableStock = selectedMaterial.TotalStkQty ?? 0;
                //rowData.PendingQuickOrderQuantity = selectedMaterial.PendingQuickOrderQuantity ?? 0;
            }
        }
    }
    updateParentValue();
};

// 獲取快速單占用量
async function getPendingQuickOrderQuantity(selectedValue: string | null, rowData: SalesDetailDto, index: number) {
    const result = await apiService.callApi(apiService.getApiWebFetchPendingQuickOrderQuantity, { MaterialId: selectedValue, CampaignId: rowData.SalesId });
    salesDetails.value[index].PendingQuickOrderQuantity = result as number;
}

// ✅ 新增：自動重新計算相關欄位
const recalculateItemFields = (data: SalesDetailDto) => {
    const orderedQuantity = data.OrderedQuantity || 0;
    const actualPrice = data.ActualPricePerSalesUnit || 0;
    const itemsPerSalesUnit = data.ItemsPerSalesUnit || 1;

    // 重新計算衍生欄位
    data.LineAmount = orderedQuantity * actualPrice; // 小計 = 組合數量 × 每組價格
    data.TotalBaseQuantity = orderedQuantity * itemsPerSalesUnit; // 實際商品數量 = 組合數量 × 每組包含數量
    data.BaseUnitPrice = itemsPerSalesUnit > 0 ? actualPrice / itemsPerSalesUnit : 0; // 每件單價 = 每組價格 ÷ 每組包含數量

    updateParentValue();
};

// ✨✨✨✨✨✨✨✨✨✨✨✨ 方法區 ✨✨✨✨✨✨✨✨✨✨✨✨
const addMaterialDetail = () => {
    // 檢查父組件的資料，查看是否已選擇檔期
    if (!props.parentCampaignList || props.parentCampaignList.length === 0) {
        props.toast.add({ severity: 'error', summary: '錯誤', detail: '請先新增檔期', life: 3000 });
        return;
    }

    const newIndex = salesDetails.value.length;
    const newItem: SalesDetailDto = {
        RowNo: newIndex + 1,
        MaterialId: null,
        ItemDisplayName: null,
        SourceType: '',
        SourceId: null,
        SalesUnit: null,
        ItemsPerSalesUnit: 1, // ✅ 新增：每單位包含數量預設值
        OrderedQuantity: 1,
        StandardPricePerSalesUnit: 0,
        ActualPricePerSalesUnit: 0,
        LineAmount: 0,
        TotalBaseQuantity: 0, // ✅ 新增：總基礎數量預設值
        BaseUnitPrice: 0, // ✅ 新增：每件商品單價預設值
        IsGift: false,
        DetailRemark: null,
        CampaignId: null,
        CampaignName: null,
        AvailableStock: null,
        PendingQuickOrderQuantity: null,
        currentMaterialOptions: []
    };

    // ✨ 使用展開運算符建立新陣列
    salesDetails.value = [...salesDetails.value, newItem];
    updateParentValue();
};

const removeMaterialDetail = (data: any) => {
    const index = salesDetails.value.indexOf(data);
    if (index > -1) {
        salesDetails.value.splice(index, 1);
        updateParentValue();
    }
};
</script>

<template>
    <div class="overflow-x-auto">
        <DataTable :value="salesDetails" showGridlines scrollable scrollDirection="horizontal" scrollHeight="auto" class="p-datatable-sm" style="min-width: 1200px">
            <template #empty>
                <div class="text-center py-4 text-gray-500">請點擊新增按鈕輸入銷售訂單資料</div>
            </template>
            <template #header>
                <div class="col-span-1 flex gap-3 items-baseline">
                    <Button icon="pi pi-plus" label="新增" class="p-button-text" @click="addMaterialDetail" />
                </div>
            </template>
            <Column field="SourceType" header="來源" style="width: 10rem; max-width: 16rem">
                <template #body="{ data, index }">
                    <Select
                        :model-value="getSourceLabel(data)"
                        :options="searchSourceTypeOptions"
                        class="w-full"
                        optionLabel="fullLabel"
                        optionValue="fullLabel"
                        placeholder="請選擇來源"
                        :show-clear="true"
                        @update:model-value="(Value) => onSourceTypeUpdate(Value, data, index)"
                    />
                </template>
            </Column>
            <Column field="MaterialId" header="商品名稱" style="width: 10rem">
                <template #body="{ data, index }">
                    <Select
                        :options="data.currentMaterialOptions || []"
                        class="w-full"
                        :placeholder="data.SourceType ? (data.SourceType === '01' ? (data.CampaignId ? '選擇檔期商品' : '請先選擇檔期') : '搜尋商品') : '請先選擇來源'"
                        filter
                        :filter-match-mode="'contains'"
                        :show-clear="true"
                        :virtualScrollerOptions="{ lazy: true, itemSize: 40, delay: 0 }"
                        :model-value="data.SourceType === '01' ? data.SourceId : data.MaterialId"
                        @update:model-value="(selectedValue) => onMaterialSelect(selectedValue, data, index)"
                        :optionLabel="(item) => (data.SourceType === '01' ? (item.ItemDisplayName ?? item.MaterialName) : (item.MaterialName ?? item.ItemDisplayName))"
                        :optionValue="(item) => (data.SourceType === '01' ? item.DetailId : item.MaterialId)"
                    >
                    </Select>
                </template>
            </Column>
            <Column field="SalesUnit" header="單位" style="width: 4rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.SalesUnit }}</span>
                </template>
            </Column>

            <Column field="OrderedQuantity" header="組合數量" style="width: 5.5rem">
                <template #body="{ data }">
                    <InputNumber
                        :style="{ width: '100%' }"
                        inputClass="w-16 text-center"
                        v-model="data.OrderedQuantity"
                        :min="0"
                        :useGrouping="false"
                        :allowEmpty="false"
                        :minFractionDigits="0"
                        :step="1"
                        @update:modelValue="() => recalculateItemFields(data)"
                        showButtons
                        buttonLayout="horizontal"
                        incrementButtonIcon="pi pi-plus"
                        decrementButtonIcon="pi pi-minus"
                    />
                </template>
            </Column>
            <Column field="TotalBaseQuantity" header="商品數量" style="width: 5rem">
                <template #body="{ data }">
                    <span class="font-medium text-blue-600">{{ data.TotalBaseQuantity?.toLocaleString() || '0' }}</span>
                </template>
            </Column>
            <Column field="ActualPricePerSalesUnit" header="銷售價格" style="width: 5.5rem">
                <template #body="{ data }">
                    <InputNumber
                        :style="{ width: '100%' }"
                        inputClass="w-20"
                        v-model="data.ActualPricePerSalesUnit"
                        :min="0"
                        :useGrouping="false"
                        :allowEmpty="false"
                        :minFractionDigits="0"
                        :step="1"
                        @update:modelValue="() => recalculateItemFields(data)"
                        showButtons
                        buttonLayout="horizontal"
                        incrementButtonIcon="pi pi-plus"
                        decrementButtonIcon="pi pi-minus"
                    />
                </template>
            </Column>

            <Column field="LineAmount" header="小計" style="width: 6rem">
                <template #body="{ data }">
                    {{ data.OrderedQuantity && data.ActualPricePerSalesUnit ? (data.OrderedQuantity * data.ActualPricePerSalesUnit).toLocaleString('en-US') : 0 }}
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
                <template #body="{ data }">
                    <InputText inputId="DetailRemark" v-model="data.DetailRemark" fluid />
                </template>
            </Column>

            <Column header="操作" style="width: 5rem" :frozen="true" alignFrozen="right">
                <template #body="{ data, index }">
                    <Button icon="pi pi-trash" class="p-button-text p-button-danger" @click="removeMaterialDetail(data)" />
                </template>
            </Column>
        </DataTable>
        <!-- <div v-if="salesDetails.length > 0" class="flex justify-end mt-2 pr-2">
            <span class="font-bold text-lg">總計：</span>
        </div> -->
    </div>
</template>
