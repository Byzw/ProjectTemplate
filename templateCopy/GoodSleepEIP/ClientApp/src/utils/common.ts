// 定義一個延遲函數，接受毫秒數作為參數
export function sleep(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

export function isNullOrEmpty(str: string | null | undefined): boolean {
    return !str || str.trim().length === 0;
}

/**
 * 處理數字輸入的共用方法
 * @param value 輸入值
 * @param decimalPlaces 小數點位數，0 表示整數
 * @param defaultValue 轉換失敗時的預設值
 * @returns 處理後的數字
 */
export function handleNumberInput(value: string, decimalPlaces: number = 0, defaultValue: number = 1): number {
    // 根據小數點位數建立正則表達式
    const regex =
        decimalPlaces > 0
            ? new RegExp(`^\\d*\\.?\\d*$`) // 允許輸入任意小數位數
            : /^\d*$/;

    if (!regex.test(value)) {
        return defaultValue;
    }

    try {
        const parsedValue =
            decimalPlaces > 0
                ? parseFloat(parseFloat(value || '0').toFixed(decimalPlaces)) // 使用 toFixed 自動截斷小數位數
                : parseInt(value || '0', 10);

        return isNaN(parsedValue) ? defaultValue : parsedValue;
    } catch {
        return defaultValue;
    }
}

/**
 * 將字串以第一個出現的分隔符分割成兩段（只分割一次）
 * @param input 原始字串
 * @param delimiter 分隔符（如 ":"、"=" 等）
 * @param trim 是否自動去除左右空白（預設 false）
 * @returns [前段, 後段]；若找不到分隔符，後段為空字串
 */
export function splitOnce(input: string, delimiter: string, trim: boolean = false): [string, string] {
    const index = input.indexOf(delimiter);

    if (index === -1) {
        return trim ? [input.trim(), ''] : [input, ''];
    }

    const left = input.substring(0, index);
    const right = input.substring(index + delimiter.length);

    return trim ? [left.trim(), right.trim()] : [left, right];
}
