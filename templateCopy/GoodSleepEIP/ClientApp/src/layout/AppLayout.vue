<script setup lang="ts">
import { useLayout } from '@/layout/composables/layout';
import { isLoading } from '@/service/apiClient';
import { computed, onBeforeUnmount, onMounted } from 'vue';

import AppBreadCrumb from './AppBreadcrumb.vue';
import AppConfig from './AppConfig.vue';
import AppFooter from './AppFooter.vue';
import AppSidebar from './AppSidebar.vue';
import AppTopbar from './AppTopbar.vue';

import { useAuthStore } from '@/stores/auth';

const { layoutConfig, layoutState, watchSidebarActive, unbindOutsideClickListener, updateAgGridDarkTheme } = useLayout();
const authStore = useAuthStore();

onMounted(() => {
    watchSidebarActive();

    const layoutContent = document.querySelector('.layout-content');
    if (layoutContent) {
        // 監聽 AG-Grid 的變化
        const observer = new MutationObserver(() => {
            updateAgGridDarkTheme();
        });
        observer.observe(layoutContent, { childList: true, subtree: true }); // 監聽 `layout-content` 內的變化
        onBeforeUnmount(() => observer.disconnect()); // 記住 observer，確保在組件卸載時移除監聽
    }
});

onBeforeUnmount(() => {
    unbindOutsideClickListener();
});

const containerClass = computed(() => {
    return [
        'layout-container',
        'layout-topbar-' + layoutConfig.topbarTheme,
        'layout-menu-' + layoutConfig.menuTheme,
        'layout-menu-profile-' + layoutConfig.menuProfilePosition,
        {
            'layout-overlay': layoutConfig.menuMode === 'overlay',
            'layout-static': layoutConfig.menuMode === 'static',
            'layout-slim': layoutConfig.menuMode === 'slim',
            'layout-slim-plus': layoutConfig.menuMode === 'slim-plus',
            'layout-horizontal': layoutConfig.menuMode === 'horizontal',
            'layout-reveal': layoutConfig.menuMode === 'reveal',
            'layout-drawer': layoutConfig.menuMode === 'drawer',
            'layout-sidebar-dark': layoutConfig.darkTheme,
            'layout-static-inactive': layoutState.staticMenuDesktopInactive && layoutConfig.menuMode === 'static',
            'layout-overlay-active': layoutState.overlayMenuActive,
            'layout-mobile-active': layoutState.staticMenuMobileActive,
            'layout-topbar-menu-active': layoutState.topbarMenuActive,
            'layout-menu-profile-active': layoutState.rightMenuActive,
            'layout-sidebar-active': layoutState.sidebarActive,
            'layout-sidebar-anchored': layoutState.anchored
        }
    ];
});
</script>

<template>
    <div :class="containerClass">
        <AppTopbar />
        <AppSidebar />

        <div class="layout-content-wrapper">
            <AppBreadCrumb class="content-breadcrumb"></AppBreadCrumb>
            <div class="layout-content" :class="{ 'loading-disabled': isLoading }">
                <router-view :key="authStore.CurrentSelectedCompany"></router-view>
            </div>
            <AppFooter></AppFooter>
        </div>

        <AppConfig />
        <Toast></Toast>
        <div class="layout-mask"></div>
    </div>
</template>
