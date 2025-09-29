import dayjs from 'dayjs';

// formatter ////////////////////////////////////////////////////////////////////////
export const booleanFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    return params.value ? '是' : '否';
};
export const booleanCheckIconRenderer = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    return params.value ? '<i class="pi pi-check" style="color:green; font-weight:bold; text-shadow: 1px 1px 2px rgba(0, 128, 0, 0.5);"></i>' : '';
};

export const defaultCampaignFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    return params.value 
        ? '<span style="display: inline-flex; align-items: center; justify-content: center; width: 20px; height: 20px; background: #10b981; border-radius: 50%; color: white;"><i class="pi pi-check" style="font-size: 11px; font-weight: 600;"></i></span>'
        : '<span style="display: inline-flex; align-items: center; justify-content: center; width: 20px; height: 20px; background: #e5e7eb; border-radius: 50%;"></span>';
};

export const lockIconRenderer = (params: any, isLock: boolean) => {
    if (params?.value === null || params?.value === undefined) return '';
    return isLock ? `<i class="fas fa-lock mr-2"></i> ${params.value}` : params.value;
};

export const taskCompletedByFormatter = (params: any) => {
    if (params.value === null || params.value === undefined || params.value === '') return '';
    return `<span style="font-weight: bold;">${params.value}</span> <i class="pi pi-check-circle" style="color:green; font-weight:bold; text-shadow: 1px 1px 2px rgba(0, 128, 0, 0.5);"></i>`;
};

export const invoiceStatusFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';

    switch (params.value) {
        case '草稿':
            return '<span style="font-weight: bold;">草稿</span>';
        case '已開立':
            return '<span style="color: green; font-weight: bold;">已開立</span>';
        case '人工作廢':
            return '<span style="color: red; font-weight: bold;">人工作廢</span>';
        default:
            return params.value;
    }
};

export const campaignStatusFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';

    switch (params.value) {
        case '草稿':
            return '<span style="font-weight: bold;">草稿</span>';
        case '已發佈':
            return '<span style="color: green; font-weight: bold;">已發佈</span>';
        case '已作廢':
            return '<span style="color: red; font-weight: bold;">已作廢</span>';
        default:
            return params.value;
    }
};

export const salesTransferStatusFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';

    switch (params.value) {
        case '未轉檔':
            return '<span style="font-weight: bold;">未轉檔</span>';
        case '轉檔成功':
            return '<span style="color: green; font-weight: bold;">轉檔成功</span>';
        case '轉檔失敗':
            return '<span style="color: red; font-weight: bold;">轉檔失敗</span>';
        case '轉檔中':
            return '<span style="color: orange; font-weight: bold;">轉檔中</span>';
        default:
            return params.value;
    }
};

export const customerTransferStatusFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';

    switch (params.value) {
        case '無須轉檔':
            return '無須轉檔';
        case '未轉檔':
            return '<span style="font-weight: bold;">未轉檔</span>';
        case '轉檔成功':
            return '<span style="color: green; font-weight: bold;">轉檔成功</span>';
        case '轉檔失敗':
            return '<span style="color: red; font-weight: bold;">轉檔失敗</span>';
        case '轉檔中':
            return '<span style="color: orange; font-weight: bold;">轉檔中</span>';
        default:
            return params.value;
    }
};

export const dateFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    if (params.value) return dayjs(params.value).format('YYYY-MM-DD');
    return;
};

export const numericDateFormatter = (params: any) => {
    if (params.value === null || params.value === undefined || params.value === 0) return '';
    if (params.value) {
        try {
            const dateStr = params.value.toString();
            if (!/^\d{8}$/.test(dateStr)) {
                return '';
            }
            return dayjs(dateStr.replace(/(\d{4})(\d{2})(\d{2})/, '$1-$2-$3')).format('YYYY-MM-DD');
        } catch {
            return '';
        }
    }
    return;
};

export const dateTimeFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    if (params.value) return dayjs(params.value).format('YYYY-MM-DD HH:mm:ss').toString();
    return;
};

export const timeFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    if (params.value) return dayjs(params.value).format('HH:mm:ss');
    return;
};

export const numberFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    if (params.value) return params.value.toLocaleString();
    return;
};

export const percentageFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    if (params.value) return `${params.value}%`;
    return;
};

export const percentageFormatter2 = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    if (params.value) return `${params.value.toFixed(2)}%`;
    return;
};

export const thousandFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    if (typeof params.value === 'number') {
        return params.value.toLocaleString('en-US'); // 以千分位格式化數字
    }
    return params.value; // 保持非數字原樣
};

// ✅T357 性別格式化 (`true` → `男`, `false` → `女`) */
export const T357GenderFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    return params.value ? '男' : '女';
};

export const zeroToEmptyFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    return params.value === 0 ? '' : params.value;
};

export const nilToEmptyFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    return params.value === 'NIL' ? '' : params.value;
};

// ✅T8 性別格式化 (`-1` → `未指定`, `0` → `男`, `1` → `女`) */
export const T8SexFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    switch (Number(params.value)) {
        case -1:
            return '未指定';
        case 0:
            return '男';
        case 1:
            return '女';
        default:
            return params.value;
    }
};

// ✅T8 出庫狀態 0：未出庫，1:部分出庫，2：完成 */
export const InventoryStateFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    switch (Number(params.value)) {
        case 0:
            return '未出庫';
        case 1:
            return '部分出庫';
        case 2:
            return '完成';
        default:
            return params.value;
    }
};

// yzw 擴充的 formatter
export const codeToValueRenderer = (params: any, relationOptions: any) => {
    if (params?.value === null || params?.value === undefined) return '';
    const option = relationOptions.value.find((option: { Code: any; Value: any; value: any }) => option.Value === params?.value || option.Code === params?.value);

    return option ? option.Description : '';
};

export const creditStatusFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';

    switch (params.value) {
        case '已作廢':
            return '<span style="color: red; font-weight: bold;">已作廢</span>';
        case '已過期':
            return '<span style="color: orange; font-weight: bold;">已過期</span>';
        case '已使用':
            return '<span style="color: gray; font-weight: bold;">已使用</span>';
        case '可使用':
            return '<span style="color: green; font-weight: bold;">可使用</span>';
        default:
            return params.value;
    }
};

export const changeTypeFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';

    switch (params.value) {
        case '儲值':
            return '<span style="color: red; font-weight: bold;"><i class="fas fa-arrow-up"></i> 儲值</span>';
        case '使用':
            return '<span style="color: green; font-weight: bold;"><i class="fas fa-arrow-down"></i> 使用</span>';
        default:
            return params.value;
    }
};

// 帶$符號的千分位
export const dollarThousandFormatter = (params: any) => {
    if (params.value === null || params.value === undefined) return '';
    if (typeof params.value === 'number') {
        return '$' + params.value.toLocaleString('en-US'); // 以千分位格式化數字
    }
    return params.value; // 保持非數字原樣
};

// filter ////////////////////////////////////////////////////////////////////////
