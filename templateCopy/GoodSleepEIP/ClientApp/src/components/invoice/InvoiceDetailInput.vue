<script setup lang="ts">
import * as models from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { useToast } from 'primevue/usetoast';
import { onMounted, ref, watch } from 'vue';
import MaterialSelector from '@/components/MaterialSelector.vue';

const props = defineProps<{
    modelValue: models.InvoiceDetailDto[] | null | undefined;
    toast: ReturnType<typeof useToast>;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: models.InvoiceDetailDto[] | null | undefined): void;
}>();

const invoiceDetails = ref<models.InvoiceDetailDto[]>(props.modelValue ?? []);

// 新增發票明細相關方法
const addInvoiceDetail = () => {
    const newIndex = invoiceDetails.value.length;
    invoiceDetails.value.push({
        InvoiceId: '00000000-0000-0000-0000-000000000000',
        RowNo: newIndex + 1,
        SourceBillNo: '',
        SourceRowCode: 0,
        MaterialId: '',
        ItemName: '',
        ItemCount: 0,
        ItemWord: '',
        ItemPrice: 0,
        ItemTaxType: '',
        ItemAmount: 0,
        DetailRemark: ''
    });
    emit('update:modelValue', invoiceDetails.value);
};

const removeInvoiceDetail = (data: any) => {
    const index = invoiceDetails.value.indexOf(data);
    if (index > -1) {
        invoiceDetails.value.splice(index, 1);
        // 重新排序 RowCode、RowNo
        invoiceDetails.value.forEach((item, index) => {
            item.RowNo = index + 1; // 假設從 1 開始
        });
        emit('update:modelValue', invoiceDetails.value);
    }
};

// 處理商品選擇
const onMaterialChange = (material: models.ComMaterialDto | null, data: models.InvoiceDetailDto) => {
    if (material) {
        // 自動填入商品相關資訊
        data.ItemName = material.MaterialName;
        data.ItemCount = 1;
        data.ItemWord = material.UnitId;
        data.ItemPrice = material.StandardPrice || 0;
        // 重新計算小計
        data.ItemAmount = (data.ItemCount || 0) * (data.ItemPrice || 0);
    } else {
        // 清空相關欄位
        data.ItemName = '';
        data.ItemCount = 0;
        data.ItemWord = '';
        data.ItemPrice = 0;
        data.ItemAmount = 0;
    }
    updateParentValue();
};

// 監聽 props 變化
watch(() => props.modelValue, (newValue) => {
    invoiceDetails.value = newValue ?? [];
}, { deep: true } );

// 更新父組件的值
const updateParentValue = () => {
    emit('update:modelValue', invoiceDetails.value);
};

// 當數量或單價改變時，重新計算小計
const onQuantityOrPriceChange = (data: models.InvoiceDetailDto) => {
    data.ItemAmount = (data.ItemCount || 0) * (data.ItemPrice || 0);
    updateParentValue();
};

</script>

<template>
    <div>
        <DataTable :value="invoiceDetails" showGridlines tableStyle="min-width: 50rem; table-layout: fixed;" class="p-datatable-sm">
            <template #empty>
                <div class="text-center py-4 text-gray-500">請點選新增按鈕輸入發票項目</div>
            </template>
            <template #header>
                <Button icon="pi pi-plus" label="新增" class="p-button-text" @click="addInvoiceDetail" />
            </template>
            <Column field="RowNo" style="width: 2rem;" class="text-center">
                <template #body="slotProps">
                    <span class="block text-center">
                        {{ slotProps.index + 1 }}
                    </span>
                </template>
            </Column>
            <Column field="MaterialId" header="搜尋商品" style="width: 12rem">
                <template #body="{ data }">
                    <MaterialSelector 
                        v-model="data.MaterialId" 
                        :toast="toast"
                        @materialChange="(material) => onMaterialChange(material, data)"
                    />
                </template>
            </Column>
            <Column field="ItemName" header="商品名稱" style="width: 12rem">
                <template #body="{ data }">
                    <InputText inputId="ItemName" v-model="data.ItemName" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="ItemCount" header="數量" style="width: 6rem">
                <template #body="{ data }">
                    <InputNumber inputId="ItemCount" v-model="data.ItemCount" fluid @update:modelValue="onQuantityOrPriceChange(data)" />
                </template>
            </Column>
            <Column field="ItemWord" header="單位" style="width: 6rem">
                <template #body="{ data }">
                    <InputText inputId="ItemWord" v-model="data.ItemWord" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="ItemPrice" header="單價" style="width: 6rem">
                <template #body="{ data }">
                    <InputNumber inputId="ItemPrice" v-model="data.ItemPrice" fluid @update:modelValue="onQuantityOrPriceChange(data)" />
                </template>
            </Column>
            <Column field="ItemAmount" header="小計" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.ItemAmount?.toLocaleString() || '0' }}</span>
                </template>
            </Column>
            <Column field="DetailRemark" header="備註" style="width: 15rem">
                <template #body="{ data }">
                    <InputText inputId="DetailRemark" v-model="data.DetailRemark" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column header="操作" style="width: 2rem">
                <template #body="{ data }">
                    <Button icon="pi pi-trash" class="p-button-text" @click="removeInvoiceDetail(data)" />
                </template>
            </Column>
        </DataTable>
    </div>
</template>
