import AppLayout from '@/layout/AppLayout.vue';
import { useAuthStore } from '@/stores/auth';
import { createRouter, createWebHistory } from 'vue-router';

const routes = [
    {
        path: '/',
        component: AppLayout,
        children: [
            {
                path: '/',
                name: 'Dashboard',
                exact: true,
                component: () => import('@/views/Dashboard.vue'),
                meta: {
                    breadcrumb: [{ parent: 'Dashboard' }],
                    requiresAuth: true, // 需要驗證
                    ModuleName: 'Dashboard' // 權限名稱
                }
            },
            {
                path: '/Permissions',
                component: () => import('@/views/Permissions.vue'),
                meta: {
                    breadcrumb: [{ parent: '模組權限定義' }],
                    requiresAuth: true,
                    ModuleName: 'Permissions'
                }
            },
            {
                path: '/PermissionRelationships',
                component: () => import('@/views/PermissionRelationships.vue'),
                meta: {
                    breadcrumb: [{ parent: '權限角色與群組關係定義' }],
                    requiresAuth: true,
                    ModuleName: 'PermissionRelationships'
                }
            },
            {
                path: '/Departments',
                component: () => import('@/views/Departments.vue'),
                meta: {
                    breadcrumb: [{ parent: '部門管理' }],
                    requiresAuth: true,
                    ModuleName: 'Departments'
                }
            },
            {
                path: '/Users',
                component: () => import('@/views/Users.vue'),
                meta: {
                    breadcrumb: [{ parent: '使用者管理' }],
                    requiresAuth: true,
                    ModuleName: 'Users'
                }
            },
            {
                path: '/SalePerformance',
                component: () => import('@/views/SalePerformance.vue'),
                meta: {
                    breadcrumb: [{ parent: '銷貨業績' }],
                    requiresAuth: true,
                    ModuleName: 'SalePerformance'
                }
            },
            {
                path: '/SysAdmin',
                component: () => import('@/views/SysAdmin.vue'),
                meta: {
                    breadcrumb: [{ parent: '核心參數調整與管理' }],
                    requiresAuth: true,
                    ModuleName: 'SysAdmin'
                }
            },
            {
                path: '/ThirdPartyConfig',
                component: () => import('@/views/ThirdPartyConfig.vue'),
                meta: {
                    breadcrumb: [{ parent: '第三方服務設定' }],
                    requiresAuth: true,
                    ModuleName: 'ThirdPartyConfig'
                }
            }
        ]
    },
    {
        path: '/Login',
        name: 'login',
        component: () => import('@/views/Login.vue')
    },
    {
        path: '/Logout',
        name: 'logout',
        component: () => import('@/views/Logout.vue')
    },
    {
        path: '/errors/access',
        name: 'accessDenied',
        component: () => import('@/views/errors/AccessDenied.vue')
    },
    {
        path: '/errors/error',
        name: 'error',
        component: () => import('@/views/errors/Error.vue')
    },
    {
        path: '/errors/notfound',
        name: 'notfound',
        component: () => import('@/views/errors/NotFound.vue')
    },
    {
        path: '/:pathMatch(.*)*',
        name: 'notfound',
        component: () => import('@/views/errors/NotFound.vue')
    },
    {
        path: '/line/login',
        name: 'LineLogin',
        component: () => import('@/views/line/LineLogin.vue')
    },
    {
        path: '/line/liffroute',
        name: 'LiffRoute',
        component: () => import('@/views/line/LiffRoute.vue')
    },
    {
        path: '/line/testpage',
        name: 'TestPage',
        component: () => import('@/views/line/TestPage.vue'),
        meta: {
            skipLoading: true,
            requiresAuth: true, // 需要驗證
            ModuleName: 'Dashboard' // 權限名稱
        }
    }
];

const router = createRouter({
    history: createWebHistory(),
    routes,
    scrollBehavior() {
        return { left: 0, top: 0 };
    }
});

// 全域路由守衛
router.beforeEach((to, from, next) => {
    const authStore = useAuthStore();
    authStore.initializeAuthState(); // 在應用啟動時載入 LocalStorage 資料

    // 如果路由的 meta.requiresAuth 明確標記為 false，則直接放行
    if (to.meta.requiresAuth === false) {
        next();
        return;
    }

    // 如果路由未設置 meta.requiresAuth，默認視為不需要認證
    if (to.meta.requiresAuth === undefined) {
        next();
        return;
    }

    // 檢查 Token 是否過期
    if (authStore.isTokenExpired()) {
        // 如果當前路徑已經是 Login 頁面，避免無限重定向
        const loginPath = '/Login';
        if (to.path !== loginPath) {
            // 先嘗試刷新 token
            authStore.refreshAccessToken().then((refreshed) => {
                if (refreshed) {
                    next(); // 如果刷新成功，繼續導航
                } else {
                    // 如果刷新失敗，清除用戶資訊並重定向到登入頁面
                    authStore.clearUserInfo();
                    next({ path: loginPath });
                }
            });
        } else {
            next(); // 如果已在 Login 頁面，直接放行
        }
        return;
    }

    // 檢查權限
    if (to.meta.ModuleName) {
        const hasPermission = authStore.user?.Permissions?.some((p) => p.ModuleName === to.meta.ModuleName && (!to.meta.ModuleSubType || p.PermissionModuleSubType === to.meta.ModuleSubType));
        if (!hasPermission) {
            console.warn(`權限不足，無法訪問路由模組：${to.meta.ModuleName}`);
            // 踢掉認證，讓其重新登入
            authStore.clearUserInfo();

            if (to.path === '/') next({ path: '/Login' });
            else next({ path: '/errors/access' }); // 跳轉到權限不足頁面

            return;
        }
    }

    next(); // 若通過所有檢查，繼續導航
});

export default router;
