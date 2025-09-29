<script setup lang="ts">
import * as models from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import { useToast } from 'primevue/usetoast';
import { onMounted, ref, watch } from 'vue';

const props = defineProps<{
    modelValue: models.CompanyServiceParam[] | null | undefined;
    toast: ReturnType<typeof useToast>;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: models.CompanyServiceParam[] | null | undefined): void;
}>();

const serviceParams = ref<models.CompanyServiceParam[]>(props.modelValue ?? []);

const addParam = () => {
    serviceParams.value.push({
        Id: '00000000-0000-0000-0000-000000000000',
        IntegrationId: '00000000-0000-0000-0000-000000000000',
        ParamKey: '',
        ParamValue: '',
        Memo: ''
    });
    emit('update:modelValue', serviceParams.value);
};

// const removeParam = (index: number) => {
//     serviceParams.value.splice(index, 1);
//     emit('update:modelValue', serviceParams.value);
// };

const removeParam = (data: any) => {
    const index = serviceParams.value.indexOf(data);
    if (index > -1) {
        serviceParams.value.splice(index, 1);
        emit('update:modelValue', serviceParams.value);
    }
};

watch(() => props.modelValue, (newValue) => {
    serviceParams.value = newValue ?? [];
}, { deep: true });

const updateParentValue = () => {
    emit('update:modelValue', serviceParams.value);
};

</script>

<template>
    <div>
        <DataTable :value="serviceParams" showGridlines tableStyle="min-width: 50rem" class="p-datatable-sm">
            <template #empty>
                <div class="text-center py-4 text-gray-500">請點選新增按鈕輸入參數</div>
            </template>
             <template #header>
                <Button icon="pi pi-plus" label="新增" class="p-button-text" @click="addParam" />
            </template>
            <Column field="ParamKey" header="鍵" style="width: 6rem;">
                <template #body="{ data }">
                    <InputText inputId="ParamKey" v-model="data.ParamKey" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="ParamValue" header="值" style="width: 6rem;">
                <template #body="{ data }">
                    <InputText inputId="ParamValue" v-model="data.ParamValue" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column field="Memo" header="備註" style="width: 15rem">
                <template #body="{ data }">
                    <InputText inputId="Memo" v-model="data.Memo" fluid @update:modelValue="updateParentValue" />
                </template>
            </Column>
            <Column header="操作" style="width: 2rem">
                <template #body="{ data }">
                    <Button icon="pi pi-trash" class="p-button-text" @click="removeParam(data)" />
                </template>
            </Column>
        </DataTable>
    </div>
</template> 