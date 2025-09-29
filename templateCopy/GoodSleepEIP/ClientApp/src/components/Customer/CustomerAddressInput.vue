<script setup lang="ts">
import * as models from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { useToast } from 'primevue/usetoast';
import { onMounted, ref, watch } from 'vue';

const props = defineProps<{
    modelValue: models.ComCustomerAddr[] | null | undefined;
    toast: ReturnType<typeof useToast>;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: models.ComCustomerAddr[] | null | undefined): void;
}>();

const authStore = useAuthStore();

const customerAddresses = ref<models.ComCustomerAddr[]>(props.modelValue ?? []);

// 新增送貨地址相關方法
const addCustomerAddress = () => {
    const newIndex = customerAddresses.value.length;
    customerAddresses.value.push({
        BizPartnerId: '',
        AddrId: '',
        RowCode: newIndex + 1,
        RowNo: newIndex + 1,
        IsMain: false,
        Address: '',
        Postalcode: '',
        LinkMan: '',
        Duty: '',
        TelNo: '',
        FaxNo: '',
        Path: '',
        Remark: ''
    });
    emit('update:modelValue', customerAddresses.value);
};

const removeCustomerAddress = (data: any) => {
    const index = customerAddresses.value.indexOf(data);
    if (index > -1) {
        customerAddresses.value.splice(index, 1);
        // 重新排序 RowCode、RowNo
        customerAddresses.value.forEach((item, index) => {
            item.RowCode = index + 1;
            item.RowNo = index + 1; // 假設從 1 開始
        });
        emit('update:modelValue', customerAddresses.value);
    }
};

// 監聽 props 變化
watch(() => props.modelValue, (newValue) => {
    customerAddresses.value = newValue ?? [];
}, { deep: true } );

// 更新父組件的值
const updateParentValue = () => {
    //console.log('CustomerAddresses', customerAddresses.value);
    emit('update:modelValue', customerAddresses.value);
};

</script>

<template>
    <div>
        <DataTable :value="customerAddresses" showGridlines tableStyle="min-width: 50rem" class="p-datatable-sm">
            <template #empty>
                <div class="text-center py-4 text-gray-500">請點選新增按鈕輸入送貨地址</div>
            </template>
            <template #header>
                <Button icon="pi pi-plus" label="新增" class="p-button-text" @click="addCustomerAddress" />
            </template>
            <Column field="RowNo" style="width: 2rem;" class="text-center">
                <template #body="slotProps">
                    <span class="block text-center">
                        {{ slotProps.index + 1 }}
                    </span>
                </template>
            </Column>
            <Column field="IsMain" header="主要" style="width: 4rem;" class="text-center">
                <template #body="{ data }">
                    <div class="flex justify-center">
                        <Checkbox
                            v-model="data.IsMain"
                            :binary="true"
                            @update:modelValue="(value) => {
                                if (value) customerAddresses.forEach(item => item !== data && (item.IsMain = false));
                                updateParentValue();
                            }"
                        />
                    </div>
                </template>
            </Column>
            <Column field="AddrId" header="地址編號" style="width: 6rem">
                <template #body="{ data }">
                    <InputText inputId="AddrId" v-model="data.AddrId" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="Address" header="地址" style="width: 15rem">
                <template #body="{ data }">
                    <InputText inputId="Address" v-model="data.Address" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="Postalcode" header="郵遞區號" style="width: 4rem">
                <template #body="{ data }">
                    <InputText inputId="Postalcode" v-model="data.Postalcode" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="LinkMan" header="聯絡人" style="width: 6rem">
                <template #body="{ data }">
                    <InputText inputId="LinkMan" v-model="data.LinkMan" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="TelNo" header="聯絡電話" style="width: 6em">
                <template #body="{ data }">
                    <InputText inputId="TelNo" v-model="data.TelNo" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="Path" header="行走路線" style="width: 6rem">
                <template #body="{ data }">
                    <InputText inputId="Path" v-model="data.Path" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="Remark" header="備註" style="width: 10rem">
                <template #body="{ data }">
                    <InputText inputId="Remark" v-model="data.Remark" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column header="操作" style="width: 2rem">
                <template #body="{ data }">
                    <Button icon="pi pi-trash" class="p-button-text" @click="removeCustomerAddress(data)" />
                </template>
            </Column>
        </DataTable>
    </div>
</template>
