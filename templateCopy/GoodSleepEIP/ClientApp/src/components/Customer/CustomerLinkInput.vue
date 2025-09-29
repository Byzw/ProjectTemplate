<script setup lang="ts">
import * as models from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { useToast } from 'primevue/usetoast';
import { onMounted, ref, watch } from 'vue';

const props = defineProps<{
    modelValue: models.ComCustomerLink[] | null | undefined;
    toast: ReturnType<typeof useToast>;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: models.ComCustomerLink[] | null | undefined): void;
}>();

const authStore = useAuthStore();

const customerLinks = ref<models.ComCustomerLink[]>(props.modelValue ?? []);

// 新增聯絡人相關方法
const addCustomerLink = () => {
    const newIndex = customerLinks.value.length;
    customerLinks.value.push({
        BizPartnerId: '',
        RowCode: newIndex + 1,
        RowNo: newIndex + 1,
        IsMain: false,
        LinkMan: '',
        Sex: -1,
        Duty: '',
        TelNo: '',
        Mobile: '',
        ContactAddress: '',
        Remark: ''
    });
    emit('update:modelValue', customerLinks.value);
};

const removeCustomerLinks = (data: any) => {
    const index = customerLinks.value.indexOf(data);
    if (index > -1) {
        customerLinks.value.splice(index, 1);
        // 重新排序 RowCode、RowNo
        customerLinks.value.forEach((item, index) => {
            item.RowCode = index + 1;
            item.RowNo = index + 1; // 假設從 1 開始
        });
        emit('update:modelValue', customerLinks.value);
    }
};

// 監聽 props 變化
watch(() => props.modelValue, (newValue) => {
    customerLinks.value = newValue ?? [];
}, { deep: true } );

// 更新父組件的值
const updateParentValue = () => {
    //console.log('CustomerLinks', customerLinks.value);
    emit('update:modelValue', customerLinks.value);
};

const SexOptions = [
    { Description: '無', value: -1 },
    { Description: '男', value: 0 },
    { Description: '女', value: 1 }
];

</script>

<template>
    <div>
        <DataTable :value="customerLinks" showGridlines tableStyle="min-width: 50rem" class="p-datatable-sm">
            <template #empty>
                <div class="text-center py-4 text-gray-500">請點選新增按鈕輸入聯絡人資訊</div>
            </template>
            <template #header>
                <Button icon="pi pi-plus" label="新增" class="p-button-text" @click="addCustomerLink" />
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
                                if (value) customerLinks.forEach(item => item !== data && (item.IsMain = false));
                                updateParentValue();
                            }"
                        />
                    </div>
                </template>
            </Column>
            <Column field="LinkMan" header="聯絡人" style="width: 8rem">
                <template #body="{ data }">
                    <InputText inputId="LinkMan" v-model="data.LinkMan" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>

            <Column field="Sex" header="性別" style="width: 6rem !important">
                <template #body="{ data }">
                    <Select inputId="Sex" v-model="data.Sex" :options="SexOptions" optionLabel="Description" optionValue="value" placeholder="請選擇" class="w-full" filter fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="Duty" header="職稱" style="width: 6rem">
                <template #body="{ data }">
                    <InputText inputId="Duty" v-model="data.Duty" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="TelNo" header="聯絡電話" style="width: 6rem">
                <template #body="{ data }">
                    <InputText inputId="TelNo" v-model="data.TelNo" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="Mobile" header="手機" style="width: 6rem">
                <template #body="{ data }">
                    <InputText inputId="Mobile" v-model="data.Mobile" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="Remark" header="備註" style="width: 15rem">
                <template #body="{ data }">
                    <InputText inputId="Remark" v-model="data.Remark" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column header="操作" style="width: 2rem">
                <template #body="{ data }">
                    <Button icon="pi pi-trash" class="p-button-text" @click="removeCustomerLinks(data)" />
                </template>
            </Column>
        </DataTable>
    </div>
</template>
