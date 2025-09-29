<script setup lang="ts">
import { useLayout } from '@/layout/composables/layout';
import { computed } from 'vue';
import { useRouter } from 'vue-router';

const { layoutState, layoutConfig, onMenuProfileToggle, isHorizontal, isSlim } = useLayout();
const menuClass = computed(() => (isHorizontal.value ? 'overlay' : null));
const router = useRouter();

function navigateTo(route: string) {
    router.push(route);
    toggleMenu();
}

function toggleMenu() {
    const menu = document.querySelector('.menu-transition') as HTMLElement | null;
    if (!menu) return; // 若 menu 為 null，則直接返回，避免錯誤

    if (layoutState.menuProfileActive) {
        menu.style.maxHeight = '0';
        menu.style.opacity = '0';
        if (isHorizontal.value) {
            menu.style.transform = 'scaleY(0.8)';
        }
    } else {
        menu.style.maxHeight = menu.scrollHeight + 'px';
        menu.style.opacity = '1';
        if (isHorizontal.value) {
            menu.style.transform = 'scaleY(1)';
        }
    }
    onMenuProfileToggle();
}

const iconClass = computed(() => {
    const profilePositionStart = layoutConfig.menuProfilePosition === 'start';

    return {
        'pi-angle-up': (layoutState.menuProfileActive && (profilePositionStart || isHorizontal.value)) || (!layoutState.menuProfileActive && !profilePositionStart && !isHorizontal.value),
        'pi-angle-down': (!layoutState.menuProfileActive && profilePositionStart) || (layoutState.menuProfileActive && !profilePositionStart) || isHorizontal.value
    };
});

function tooltipValue(tooltipText: string) {
    return isSlim.value ? tooltipText : null;
}
</script>

<template>
    <div class="layout-menu-profile">
        <button v-tooltip="{ value: tooltipValue('Profile') }" class="rounded-none" @click="toggleMenu()">
            <img src="/layout/images/avatar/amyelsner.png" alt="avatar" style="width: 32px; height: 32px" />
            <span>
                <strong>Amy Elsner</strong>
                <small>Webmaster</small>
            </span>
            <i class="layout-menu-profile-toggler pi pi-fw" :class="iconClass"></i>
        </button>

        <ul :class="['menu-transition', menuClass]" style="max-height: 0; opacity: 0">
            <li v-tooltip="{ value: tooltipValue('Settings') }">
                <button @click="navigateTo('/profile/create')">
                    <i class="pi pi-cog pi-fw"></i>
                    <span>Settings</span>
                </button>
            </li>

            <li v-tooltip="{ value: tooltipValue('Profile') }">
                <button @click="navigateTo('/profile/list')">
                    <i class="pi pi-file-o pi-fw"></i>
                    <span>Profile</span>
                </button>
            </li>
            <li v-tooltip="{ value: tooltipValue('Support') }">
                <button @click="navigateTo('/documentation')">
                    <i class="pi pi-compass pi-fw"></i>
                    <span>Support</span>
                </button>
            </li>
            <li v-tooltip="{ value: tooltipValue('Logout') }">
                <button @click="navigateTo('/auth/login2')">
                    <i class="pi pi-power-off pi-fw"></i>
                    <span>Logout</span>
                </button>
            </li>
        </ul>
    </div>
</template>

<style scoped>
.layout-menu-profile {
    display: none;
}
.menu-transition {
    transition:
        max-height 400ms cubic-bezier(0.86, 0, 0.07, 1),
        opacity 400ms cubic-bezier(0.86, 0, 0.07, 1);
}
.menu-transition.overlay {
    transition:
        opacity 100ms linear,
        transform 120ms cubic-bezier(0, 0, 0.2, 1);
}
</style>
