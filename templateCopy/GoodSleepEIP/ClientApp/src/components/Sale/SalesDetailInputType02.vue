<script setup lang="ts">
import { apiService } from '@/service/apiClient';
import * as models from '@/service/apiServices.schemas';
import { useAuthStore } from '@/stores/auth';
import * as gridFormatter from '@/utils/gridFormatter';
import { AxiosResponse } from 'axios';
import { isEmpty } from 'lodash';
import { useToast } from 'primevue/usetoast';
import { ref, watch } from 'vue';

const props = defineProps<{
    modelValue: models.CustomerCreditDto[] | null | undefined;
    isEditMode?: boolean;
    BizPartnerId: string | null | undefined;
    SaleOrderTotalAmount: number;
    toast: ReturnType<typeof useToast>;
    updateShowCreditTable: Function;
}>();

const emit = defineEmits<{
    (e: 'update:modelValue', value: models.CustomerCreditDto[] | null | undefined): void;
}>();

const authStore = useAuthStore();

const CustomerAllCredit = ref<models.CustomerCreditDto[]>([]); // 客戶可用的購物金列表
const CreditSelectedRows = ref<models.CustomerCreditDto[]>(props.modelValue ?? []); // 客戶正在使用的購物金

// 智慧配置購物金：根據訂單總金額自動分配購物金使用額度
// 優先使用即將到期的購物金，並考慮單次使用限額
const AutoUsedCredit = () => {
    let remainingNeed = props.SaleOrderTotalAmount; // 剩餘需要折抵的金額
    let selectedRowsTemp: models.CustomerCreditDto[] = [];

    // 按優先順序（通常按到期時間排序）遍歷可用購物金
    for (const credit of CustomerAllCredit.value) {
        const totalAmount = credit.Amount ?? 0;
        const usedAmount = credit.UsedAmount ?? 0;
        const availableAmount = totalAmount - usedAmount; // 該購物金的可用餘額
        const maxUsableAmount = Math.min(availableAmount, credit.MaxSingleUsageAmount!); // 考慮單次使用限額

        if (remainingNeed <= 0) break; // 已滿足所有需求

        let usingAmount = 0;
        if (remainingNeed >= maxUsableAmount) {
            // 需求金額 >= 該購物金最大可用額度，全額使用
            usingAmount = maxUsableAmount;
        } else {
            // 需求金額 < 該購物金最大可用額度，部分使用
            usingAmount = remainingNeed;
        }

        if (usingAmount > 0) {
            selectedRowsTemp.push({
                ...credit,
                UsingAmount: usingAmount
            });
            remainingNeed -= usingAmount;
        }
    }

    CreditSelectedRows.value = selectedRowsTemp;

    // 更新所有購物金的使用金額顯示
    for (const credit of CustomerAllCredit.value) {
        const selectedCredit = CreditSelectedRows.value.find((item) => item.CreditId == credit.CreditId);
        credit.UsingAmount = selectedCredit ? selectedCredit.UsingAmount : 0;
    }
    emit('update:modelValue', CreditSelectedRows.value);
};

const updateModelValue = (data: models.CustomerCreditDto, value: number) => {
    const target = CreditSelectedRows.value.find((c) => c.CreditId === data.CreditId);
    if (target) {
        target.UsingAmount = value;
    }
};

// 監聽 props.BizPartnerId 變化,動態獲取客戶的購物金
watch(
    () => props.BizPartnerId,
    async (newValue) => {
        // 客戶改變，重新獲取購物金
        if (props.BizPartnerId) {
            const param = {
                ExtraParams: { BizPartnerId: props.BizPartnerId },
                filterModel: {},
                sortModel: [
                    {
                        sort: 'desc',
                        colId: 'CreditExpiryTime'
                    }
                ],
                startRow: 0,
                endRow: 100
            };

            // 新增模式：只查詢可使用的購物金
            if (!props.isEditMode) {
                param.filterModel = {
                    CreditStatus: {
                        values: ['可使用'],
                        filterType: 'set'
                    }
                };
            }
            // 編輯模式：查詢所有購物金，包括已使用的（因為可能當前訂單已使用）

            await apiService.postApiWebCustomerCreditList(param).then((response: AxiosResponse<any, any>) => {
                if (response.status === 200) {
                    let allCredits = response.data.rows as models.CustomerCreditDto[];

                    if (props.isEditMode) {
                        // 編輯模式：過濾出可使用的購物金 + 當前訂單已使用的購物金
                        const availableCredits = allCredits.filter((credit: any) => credit.CreditStatus === '可使用');
                        const usedCreditSns = props.modelValue?.map((item) => item.CreditSn) || [];
                        const usedCredits = allCredits.filter((credit: any) => usedCreditSns.includes(credit.CreditSn) && credit.CreditStatus !== '可使用');

                        CustomerAllCredit.value = [...availableCredits, ...usedCredits];
                    } else {
                        // 新增模式：只顯示可使用的購物金
                        CustomerAllCredit.value = allCredits;
                    }

                    // 購物金為空時，不展示
                    props.updateShowCreditTable(isEmpty(CustomerAllCredit.value) ? false : true);
                    if (props.isEditMode) {
                        CreditSelectedRows.value = props.modelValue ?? [];
                        // 設置已選擇的購物金金額,否則只會選擇，不會顯示金額
                        CreditSelectedRows.value?.forEach((item) => {
                            const target = CustomerAllCredit.value.find((c) => c.CreditSn === item.CreditSn);
                            if (target) {
                                target.UsingAmount = item.UsingAmount ?? 0;
                            } else {
                                const temp = { ...item };
                                CustomerAllCredit.value.push(temp);
                            }
                        });
                    } else {
                        AutoUsedCredit();
                    }
                }
            });
        } else {
            CustomerAllCredit.value = [];
            CreditSelectedRows.value = [];
            props.updateShowCreditTable(isEmpty(CustomerAllCredit.value) ? false : true);
            emit('update:modelValue', CreditSelectedRows.value);
        }
    },
    { deep: true, immediate: true }
);

// 更新已勾選的用戶購物金
watch(
    () => CreditSelectedRows,
    async (newValue) => {
        emit('update:modelValue', CreditSelectedRows.value);
    },
    { deep: true }
);
</script>

<template>
    <div class="overflow-x-auto">
        <DataTable
            :value="CustomerAllCredit"
            dataKey="CreditSn"
            headerStyle="text-align: left;"
            showGridlines
            tableStyle="min-width: 30rem"
            class="p-datatable-sm"
            v-model:selection="CreditSelectedRows"
            selectionMode="multiple"
            style="min-width: 1200px"
        >
            <template #empty>
                <div class="text-center py-4 text-gray-500">無可用的購物金</div>
            </template>
            <template #header>
                <Button icon="fa-solid fa-magic-wand-sparkles" label="一鍵配置" class="p-button-text" @click="AutoUsedCredit" v-tooltip.bottom="'根據訂單金額自動分配購物金使用額度'" />
            </template>
            <Column selectionMode="multiple" bodyStyle="text-align: center;" style="width: 0.5rem" />
            <Column field="CreditTypeDescription" header="購物金類型" style="width: 4rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.CreditTypeDescription }}</span>
                </template>
            </Column>
            <Column field="CreditSn" header="購物金序號" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.CreditSn }}</span>
                </template>
            </Column>

            <Column field="CreditName" header="購物金名稱" style="width: 8rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ data.CreditName }}</span>
                </template>
            </Column>
            <Column field="Amount" header="購物金總金額" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ gridFormatter.thousandFormatter(ref(data.Amount)) }}</span>
                </template>
            </Column>
            <Column field="RemainAmount" header="可用金額" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ gridFormatter.thousandFormatter(ref(data.RemainAmount)) }}</span>
                </template>
            </Column>
            <Column field="MaxSingleUsageAmount" header="單次最大可用金額" style="width: 6rem">
                <template #body="{ data }">
                    <span class="font-medium">{{ gridFormatter.thousandFormatter(ref(data.MaxSingleUsageAmount)) }}</span>
                </template>
            </Column>
            <Column field="UsingAmount" header="欲使用金額" style="width: 6rem">
                <template #body="{ data }">
                    <InputNumber
                        :style="{ width: '100%' }"
                        inputClass="w-16 text-left"
                        v-model="data.UsingAmount"
                        @update:model-value="updateModelValue(data, $event)"
                        :min="0"
                        :max="data.MaxSingleUsageAmount"
                        :useGrouping="false"
                        :allowEmpty="false"
                        :minFractionDigits="0"
                        :step="1"
                        showButtons
                        buttonLayout="horizontal"
                        incrementButtonIcon="pi pi-plus"
                        decrementButtonIcon="pi pi-minus"
                    />
                </template>
            </Column>
        </DataTable>
    </div>
</template>

<style scoped>
:deep(th > .p-datatable-column-header-content > .p-checkbox) {
    margin: 0 auto !important;
    display: flex !important;
    justify-content: center !important;
    align-items: center !important;
}

:deep(th > .p-datatable-column-header-content > .p-datatable-column-title) {
    display: flex !important;
    justify-content: flex-start !important;
    align-items: center !important;
}
</style>
