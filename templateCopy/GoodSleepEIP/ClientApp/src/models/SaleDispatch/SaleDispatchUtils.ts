// 發貨單掃碼記錄
export const createLog = (message: string, type: 'normal' | 'success' | 'error' = 'normal') => {
    const time = new Date().toLocaleTimeString('zh-Hans-CN', { hour12: false });
    const cssClass = {
        normal: 'text-gray-800',
        success: 'text-blue-600 font-semibold',
        error: 'text-red-600 font-bold'
    }[type];
    return { message, time, class: cssClass };
};
