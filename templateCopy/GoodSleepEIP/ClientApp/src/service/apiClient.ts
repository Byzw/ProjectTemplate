import { getWhFrameworkAPI } from '@/service/apiServices'; // 導入 orval 自動產生的 API 方法
import type { JsonResponse } from '@/service/apiServices.schemas'; // 導入 orval 自動產生的資料模型
import { useAuthStore } from '@/stores/auth';
import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { ref, watch } from 'vue';

// 創建 Axios 實例
const apiClient: AxiosInstance = axios.create({
    //baseURL: process.env.NODE_ENV === 'development' ? '/apiBase' : '', // 根據環境動態設置 baseURL
    baseURL: '',
    headers: {
        'Content-Type': 'application/json'
    }
});

export const isLoading = ref(false);    // 全局的 loading 狀態

watch(isLoading, (newVal) => {
    const buttons = document.querySelectorAll('button, input[type="button"], input[type="submit"], input[type="reset"]');
    buttons.forEach((button) => {
        if (newVal) button.setAttribute('disabled', 'true');
        else button.removeAttribute('disabled');
    });
});

// 添加請求攔截器
apiClient.interceptors.request.use(
    async (config) => {
        var checkUrl = config.url?.toLowerCase() ?? "";
        var showIsLoading = true;

        if (checkUrl.includes('login')) return config; // 登入請求不需要攔截
        if (checkUrl.includes('getallnotifications')) showIsLoading = false;    // 通知列表不需要顯示 loading
        if (checkUrl.includes('refreshtoken')) return config; // 刷新 token 的請求不需要攔截

        if (showIsLoading) isLoading.value = true; // 在請求開始前設置 loading 為 true
        const authStore = useAuthStore();

        // 檢查 token 是否即將過期
        if (authStore.isTokenExpiringSoon()) {
            // 嘗試更新 token
            const refreshed = await authStore.refreshAccessToken();
            if (refreshed) {
                // 使用新的 token
                config.headers = config.headers || {};
                config.headers.Authorization = `Bearer ${authStore.token}`;
            }
        } else if (authStore.token && authStore.token !== '') {
            config.headers = config.headers || {};
            config.headers.Authorization = `Bearer ${authStore.token}`;
        }
        return config;
    },
    (error) => {
        isLoading.value = false; // 出現錯誤時設置 loading 為 false
        return Promise.reject(error);
    }
);

// 添加響應攔截器
apiClient.interceptors.response.use(
    (response) => {
        isLoading.value = false; // 在成功響應後設置 loading 為 false
        return response;
    },
    async (error) => {
        isLoading.value = false; // 在響應錯誤時設置 loading 為 false
        const authStore = useAuthStore();

        let customMessage = '發生未知錯誤，請稍後再試。';
        if (error.response) {
            switch (error.response.status) {
                case 401:
                    // 檢查是否為 token 過期，於後端 program.cs 中 JwtBearerEvents 中設定特定 message
                    if (error.response.data?.message?.includes('Token has expired')) {
                        // 嘗試刷新 token
                        const refreshed = await authStore.refreshAccessToken();
                        if (refreshed) {
                            // 重新發送原始請求
                            const originalRequest = error.config;
                            originalRequest.headers.Authorization = `Bearer ${authStore.token}`;
                            return apiClient(originalRequest);
                        }
                    }
                    // 如果不是 token 過期，或是刷新失敗，則顯示權限錯誤訊息
                    customMessage = `授權失敗，請聯絡管理員給予代碼: 『${error.response.request.responseURL.split('/').pop()}-${error.response.status}』。`;
                    break;
                case 403:
                    customMessage = `授權失敗『${error.response.data?.message}』，代碼: 『${error.response.request.responseURL.split('/').pop()}-${error.response.status}』。`;
                    break;
                case 400:
                    customMessage = `\n1. 請求錯誤，請檢查輸入的內容。\n2. 除錯訊息: ${error.response.data?.message || error.response.request.statusText || '未知錯誤'}。`;
                    break;
                case 404:
                    customMessage = '找不到您請求的資源。';
                    break;
                case 500:
                    customMessage = '伺服器發生內部錯誤，請稍後再試。';
                    break;
            }
            // 用 ApiError 包裝錯誤，拋出自定義訊息
            return Promise.reject(new ApiError(customMessage, error.response.status));
        } else if (error.request) {
            // 網路連線問題
            customMessage = '無法連接到伺服器，請檢查您的網路連線。';
            return Promise.reject(new ApiError(customMessage));
        } else {
            // 處理其他未知錯誤
            return Promise.reject(new ApiError(customMessage));
        }
    }
);

// 擴充 Error 類別，自定義錯誤訊息
export class ApiError extends Error {
    constructor(
        public message: string,
        public statusCode?: number
    ) {
        super(message); // 調用父類別的建構函數
        this.name = 'API錯誤'; // 為錯誤命名
    }
}

// 通用的後端 handleResponse 方法
export function handleResponse<T = any>(response: any): T {
    // 解構出 response.data，並檢查其是否符合 JsonResponse 結構
    const result: JsonResponse = response?.data;

    if (!result || typeof result !== 'object') {
        throw new Error('回應內容格式錯誤');
    }

    // 檢查 success 屬性
    if (result.success) {
        // 確保 data 存在
        if (result.data === undefined) {
            throw new Error('回應內容格式缺漏 data 錯誤');
        }
        return result.data as T;
    } else {
        throw result.message || '未知錯誤';
    }
}
export function handleResponseWholeMessage<T = any>(response: any): JsonResponse {
    // 解構出 response.data，並檢查其是否符合 JsonResponse 結構
    const result: JsonResponse = response?.data;

    if (!result || typeof result !== 'object') {
        throw new Error('回應內容格式錯誤');
    }

    // 檢查 success 屬性
    if (result.success) {
        // 確保 data 存在
        if (result.data === undefined) {
            throw new Error('回應內容格式缺漏 data 錯誤');
        }
        return result;
    } else {
        throw new Error(result.message || '未知錯誤');
    }
}

/**
 * 通用下載檔案方法，支援 GET/POST，也會打認證資訊，並可傳參數與錯誤處理
 * @param url API 路徑
 * @param params 查詢參數（GET: query string, POST: body）
 * @param method 'get' | 'post'
 */
export async function downloadFileWithApi(
    url: string,
    params: any = {},
    method: 'get' | 'post' = 'get'
): Promise<void> {
    try {
        const authStore = useAuthStore();
        const config: any = {
            responseType: 'blob',
            headers: {
                Authorization: `Bearer ${authStore.token}`
            }
        };

        let response;
        if (method === 'get') {
            config.params = params;
            response = await axios.get(url, config);
        } else {
            response = await axios.post(url, params, config);
        }

        // 嘗試判斷是否為錯誤訊息（JSON）
        const contentType = response.headers['content-type'];
        if (contentType && contentType.includes('application/json')) {
            // 解析 blob 內容為 JSON
            const text = await response.data.text();
            let errorJson;
            try {
                errorJson = JSON.parse(text);
            } catch {
                throw new Error('下載失敗，無法解析訊息');
            }
            throw new Error(errorJson.message || '下載失敗，未知錯誤');
        }

        // 解析檔名
        const contentDisposition = response.headers['content-disposition'];

        let filename: string = crypto.randomUUID() + '.xlsx';
        if (contentDisposition) {
            let matchUtf8 = contentDisposition.match(/filename\*=UTF-8''([^;]+)/);
            let matchAscii = contentDisposition.match(/filename="?([^";]+)"?/);
            if (matchUtf8) filename = decodeURIComponent(matchUtf8[1]); // UTF-8 解碼
            else if (matchAscii) filename = matchAscii[1]; // 傳統 ASCII 編碼
        }

        // 觸發下載
        //const blob = new Blob([response.data], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
        const blob = new Blob([response.data], { type: response.headers['content-type'] });
        const urlObj = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = urlObj;
        link.setAttribute('download', filename);
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(urlObj);
    } catch (error: any) {
        // axios 400/500 會進這裡
        if (axios.isAxiosError(error) && error.response && error.response.data) {
            // 嘗試解析錯誤訊息
            try {
                const reader = new FileReader();
                reader.onload = function () {
                    try {
                        const json = JSON.parse(reader.result as string);
                        throw new Error(json.message || '下載失敗');
                    } catch {
                        throw new Error('下載失敗，且無法解析錯誤訊息');
                    }
                };
                reader.readAsText(error.response.data);
            } catch {
                throw new Error('下載失敗，且無法解析錯誤訊息');
            }
        } else {
            throw error;
        }
    }
}

// 自定義 axios 請求邏輯，支持泛型
export const customInstance = <T = any>(config: AxiosRequestConfig): Promise<AxiosResponse<T>> => {
    const authStore = useAuthStore();
    if (authStore.token) {
        config.headers = {
            ...config.headers,
            Authorization: `Bearer ${authStore.token}`
        };
    }

    // 使用自定義的配置發送請求
    try {
        return apiClient.request<T>(config);
    }
    catch (error) {
        console.error(error);
        return Promise.reject(`呼叫 API 方法時發生錯誤: ${error}`);
    }
};

// 包裝函數，包裝一下資料回來調用 handleResponse，比較好看
function callApi<T>(apiMethod: (params?: any) => Promise<AxiosResponse>, params?: any): Promise<T> {
    try {
        return apiMethod(params).then((response) => handleResponse<T>(response));
    } catch (error) {
        console.error(error);
        return Promise.reject(`呼叫 API 方法時發生錯誤: ${error}`);
    }
}

// 調用 getMisApi() 並將其展開
const ApiFunctions = getWhFrameworkAPI();

export const apiService = {
    apiClient,
    handleResponse,
    callApi,
    downloadFileWithApi,
    ...ApiFunctions
};


