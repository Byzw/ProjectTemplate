import type { UserSessionData } from '@/service/apiServices.schemas';
import { jwtDecode } from 'jwt-decode';
import { defineStore } from 'pinia';
import { apiService } from '@/service/apiClient';

// 本方法應與後端 HasActionPermission 方法保持一致
export function hasActionPermission(ModuleName: string, action: string): boolean {
    const authStore = useAuthStore();

    // 取得所有符合 `ModuleName` 的權限記錄
    const matchedPermissions = authStore.user?.Permissions?.filter((p) => p.ModuleName === ModuleName) || [];

    if (matchedPermissions.length === 0) return false; // 如果沒有該 `ModuleName` 的記錄，直接返回 false

    // 如果 `CanManage = true`，直接返回 true
    if (matchedPermissions.some((p) => p.CanManage)) return true;

    // 如果沒有 `CanManage`，則正常檢查 `action` 欄位
    return matchedPermissions.some((permission) => !!permission[action as keyof typeof permission]);
}

export const useAuthStore = defineStore('auth', {
    state: () => ({
        token: localStorage.getItem('token') || '',
        tokenExpiry: localStorage.getItem('tokenExpiry') ? new Date(localStorage.getItem('tokenExpiry')!) : null,
        refreshToken: localStorage.getItem('refreshToken') || '',
        refreshTokenExpiry: localStorage.getItem('refreshTokenExpiry') ? new Date(localStorage.getItem('refreshTokenExpiry')!) : null,
        user: JSON.parse(localStorage.getItem('user') || 'null') as UserSessionData | null,
    }),
    getters: { },
    actions: {
        setToken(token: string) {
            this.token = token;
            localStorage.setItem('token', token);

            // 解碼 Token 獲取過期時間
            const decoded: { exp?: number } = jwtDecode(token);
            if (decoded.exp) {
                this.tokenExpiry = new Date(decoded.exp * 1000);
                localStorage.setItem('tokenExpiry', this.tokenExpiry.toISOString());
            }
        },
        setRefreshToken(refreshToken: string) {
            this.refreshToken = refreshToken;
            localStorage.setItem('refreshToken', refreshToken);
        },
        setUserInfo(user: UserSessionData) {
            // 先設置 token，因為這是驗證的關鍵
            if (user.Token) {
                this.setToken(user.Token);
            }
            if (user.RefreshToken) {
                this.setRefreshToken(user.RefreshToken);
                if (user.RefreshTokenExpiry) {
                    this.refreshTokenExpiry = new Date(user.RefreshTokenExpiry);
                    localStorage.setItem('refreshTokenExpiry', this.refreshTokenExpiry.toISOString());
                }
            }

            // 最後設置用戶資訊
            this.user = user;
            localStorage.setItem('user', JSON.stringify(user));
        },
        clearUserInfo() {
            this.user = null;
            this.token = '';
            this.refreshToken = '';
            this.tokenExpiry = null;
            this.refreshTokenExpiry = null;
            localStorage.removeItem('user');
            localStorage.removeItem('token');
            localStorage.removeItem('refreshToken');
            localStorage.removeItem('tokenExpiry');
            localStorage.removeItem('refreshTokenExpiry');
        },
        isTokenExpired() {
            if (!this.tokenExpiry) return true;
            return new Date() > this.tokenExpiry;
        },
        isRefreshTokenExpired() {
            if (!this.refreshTokenExpiry) return true;
            return new Date() > this.refreshTokenExpiry;
        },
        isTokenExpiringSoon() {
            if (!this.tokenExpiry) return true;

            // 當 token 剩餘時間少於 ?? 分鐘時，視為即將過期 (提前主動申請，以避免"第一次"發現需要更新 token 時，已經過期而出現401，體驗不佳)
            const expirationBuffer = 30 * 60 * 1000; // 30 minutes in milliseconds
            return new Date().getTime() + expirationBuffer > this.tokenExpiry.getTime();
        },
        async refreshAccessToken(): Promise<boolean> {
            try {
                if (!this.refreshToken || this.isRefreshTokenExpired()) {
                    return false;
                }

                const userData: UserSessionData = await apiService.callApi(apiService.postApiWebRefreshToken, {
                    RefreshToken: this.refreshToken
                });

                this.setToken(userData.Token || '');
                if (userData.RefreshToken) {
                    this.setRefreshToken(userData.RefreshToken);
                }
                return true;
            } catch (error) {
                console.error('Refresh token failed:', error);
                return false;
            }
        },
        initializeAuthState() {
            this.token = localStorage.getItem('token') || '';
            this.tokenExpiry = localStorage.getItem('tokenExpiry') ? new Date(localStorage.getItem('tokenExpiry')!) : null;
            this.refreshToken = localStorage.getItem('refreshToken') || '';
            this.refreshTokenExpiry = localStorage.getItem('refreshTokenExpiry') ? new Date(localStorage.getItem('refreshTokenExpiry')!) : null;
            this.user = JSON.parse(localStorage.getItem('user') || 'null') as UserSessionData | null;
        },
        checkStorageAvailability() {
            try {
                localStorage.setItem('test', 'test');
                localStorage.removeItem('test');
                return true;
            } catch {
                return false;
            }
        },
        async logout() {
            try {
                if (this.refreshToken) {
                    await apiService.callApi(apiService.postApiWebRevokeToken, {
                        RefreshToken: this.refreshToken
                    });
                }
            } catch (error) {
                console.error('Revoke token failed:', error);
            } finally {
                this.clearUserInfo();
                window.location.href = '/Login'; // 導向登入頁面
            }
        }
    }
});
