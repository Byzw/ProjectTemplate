<script setup lang="ts">
import type { MenuFolder, MenuItem } from '@/models/Menu';
import { useAuthStore } from '@/stores/auth';
import { computed } from 'vue';
import AppMenuItem from './AppMenuItem.vue';

// ✨✨✨✨✨✨✨✨✨✨✨✨ Menu 定義區 ✨✨✨✨✨✨✨✨✨✨✨✨
// 📑成員定義說明請見 @/models/Menu，
const menuModel: MenuFolder[] = [
    {
        folderLabel: '功能區',
        folderItems: [
            {
                itemLabel: 'Dashboard',
                itemIcon: 'fa-solid fa-house',
                itemUrlPath: '/',
                ModuleName: 'Dashboard'
            },
            {
                itemLabel: '銷貨業績',
                itemIcon: 'fa-solid fa-boxes-packing',
                itemUrlPath: '/SalePerformance',
                ModuleName: 'SalePerformance',
                ModuleSubType: '管理'
            }
        ]
    },
    {
        folderLabel: '系統管理區',
        ModuleName: '',
        folderItems: [
            {
                folderLabel: '權限與帳號管理',
                folderIcon: 'pi pi-shield',
                ModuleName: 'Permissions',
                folderItems: [
                    {
                        itemLabel: '模組權限定義',
                        itemIcon: 'fa-solid fa-laptop-code',
                        itemUrlPath: '/Permissions',
                        ModuleName: 'Permissions'
                    },
                    {
                        itemLabel: '權限關係定義',
                        itemIcon: 'fa-regular fa-address-card',
                        itemUrlPath: '/PermissionRelationships',
                        ModuleName: 'PermissionRelationships'
                    },
                    {
                        itemLabel: '部門管理',
                        itemIcon: 'fa-solid fa-building-user',
                        itemUrlPath: '/Departments',
                        ModuleName: 'Departments'
                    },
                    {
                        itemLabel: '使用者管理',
                        itemIcon: 'fa-solid fa-user-pen',
                        itemUrlPath: '/Users',
                        ModuleName: 'Users'
                    }
                ]
            },
            {
                folderLabel: '系統設定',
                folderIcon: 'pi pi-sliders-h',
                folderItems: [
                    {
                        itemLabel: '核心參數調整',
                        itemIcon: 'fa-solid fa-screwdriver-wrench',
                        itemUrlPath: '/SysAdmin'
                    },
                    {
                        itemLabel: '第三方服務設定',
                        itemIcon: 'fa-solid fa-network-wired',
                        itemUrlPath: '/ThirdPartyConfig',
                        ModuleName: 'ThirdPartyConfig',
                        ModuleSubType: '管理'
                    }
                ]
            }
        ]
    }
];

const authStore = useAuthStore();
const filterModel = computed(() => filterMenu(menuModel)); // 過濾後的選單

// 遞迴過濾選單，移除無權限的項目（包含子選單）。
function filterMenu(menuFolders: (MenuFolder | MenuItem)[]): (MenuFolder | MenuItem)[] {
    return menuFolders
        .map((item) => {
            // 檢查權限
            if (item.ModuleName) {
                const hasPermission = authStore.user?.Permissions?.some((p) => p.ModuleName === item.ModuleName && (!item.ModuleSubType || p.PermissionModuleSubType === item.ModuleSubType));
                if (!hasPermission) return null;
            }

            // 遞迴處理 `folderItems`
            if ('folderItems' in item) {
                const filteredItems = filterMenu(item.folderItems ?? []);
                return filteredItems.length > 0 ? { ...item, folderItems: filteredItems } : null;
            }

            return item;
        })
        .filter((item) => item !== null);
}
</script>

<template>
    <ul class="layout-menu">
        <template v-for="(item, i) in filterModel" :key="item">
            <AppMenuItem :item="item" root :index="i" />
            <li class="menu-separator"></li>
        </template>
    </ul>
</template>

<style lang="scss" scoped></style>
