<script setup lang="ts">
import { useLayout } from '@/layout/composables/layout';
import { calculateScrollbarWidth } from '@primeuix/utils/dom';
import { nextTick, onBeforeMount, ref, watch } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const { layoutConfig, layoutState, setActiveMenuItem, setMenuStates, setStaticMenuMobile, onMenuToggle, isHorizontal, isSlim, isSlimPlus, isDesktop } = useLayout();

const props = defineProps({
    item: {
        type: Object,
        default: () => ({})
    },
    index: {
        type: Number,
        default: 0
    },
    root: {
        type: Boolean,
        default: true
    },
    parentItemKey: {
        type: String,
        default: null
    }
});

const isActiveMenu = ref(false);
const subMenuRef = ref(null);
const menuItemRef = ref(null);
const itemKey = ref<string>('');

onBeforeMount(() => {
    itemKey.value = props.parentItemKey ? `${props.parentItemKey}-${props.index}` : String(props.index);
    const activeItem = layoutState.activeMenuItem as string | null;

    isActiveMenu.value = activeItem === itemKey.value || activeItem?.startsWith(`${itemKey.value}-`) || false;
    handleRouteChange(route.path);
});

watch(
    () => isActiveMenu.value,
    () => {
        const rootIndex = props.root ? props.index : parseInt(`${props.parentItemKey}`[0], 10);
        const overlay = document.querySelectorAll('.layout-root-submenulist')[rootIndex];
        const target = document.querySelectorAll('.layout-root-menuitem')[rootIndex];

        if ((isSlim.value || isSlimPlus.value || isHorizontal.value) && isDesktop) {
            nextTick(() => {
                calculatePosition(overlay, target);
            });
        }
    }
);

watch(
    () => layoutState.activeMenuItem,
    (newVal: string | null = '') => {
        isActiveMenu.value = newVal === itemKey.value || (newVal !== null && newVal.startsWith(itemKey.value + '-'));
    }
);

watch(
    () => layoutConfig.menuMode,
    () => {
        isActiveMenu.value = false;
    }
);

watch(
    () => layoutState.overlaySubmenuActive,
    (newValue) => {
        if (!newValue) {
            isActiveMenu.value = false;
        }
    }
);

function handleRouteChange(newPath: string) {
    if (!(isSlim.value || isSlimPlus.value || isHorizontal.value) && props.item.itemUrlPath && props.item.itemUrlPath === newPath) {
        setActiveMenuItem(itemKey);
    } else if (isSlim.value || isSlimPlus.value || isHorizontal.value) {
        isActiveMenu.value = false;
    }
}

watch(() => route.path, handleRouteChange);

async function itemClick(event: Event, item: any) {
    if (item.disabled) {
        event.preventDefault();
        return;
    }

    if ((item.itemUrlPath || item.url) && (layoutState.staticMenuMobileActive || layoutState.overlayMenuActive)) {
        onMenuToggle();
    }

    if (item.command) {
        item.command({ originalEvent: event, item: item });
    }

    if (item.folderItems) {
        if (props.root && isActiveMenu.value && (isSlim.value || isSlimPlus.value || isHorizontal.value)) {
            setMenuStates(false);

            return;
        }

        setActiveMenuItem(isActiveMenu.value ? props.parentItemKey : itemKey);

        if (props.root && !isActiveMenu.value && (isSlim.value || isSlimPlus.value || isHorizontal.value)) {
            setMenuStates(true);
            isActiveMenu.value = true;

            removeAllTooltips();
        }
    } else {
        if (!isDesktop) {
            setStaticMenuMobile();
        }

        if (isSlim.value || isSlimPlus.value || isHorizontal.value) {
            setMenuStates(false);

            return;
        }

        setActiveMenuItem(itemKey);
    }
}

function onMouseEnter() {
    if (props.root && (isSlim.value || isSlimPlus.value || isHorizontal.value) && isDesktop) {
        if (!isActiveMenu.value && layoutState.menuHoverActive) {
            setActiveMenuItem(itemKey);
        }
    }
}

function removeAllTooltips() {
    const tooltips = document.querySelectorAll('.p-tooltip');
    tooltips.forEach((tooltip) => {
        tooltip.remove();
    });
}

function calculatePosition(overlay: any, target: any) {
    if (overlay) {
        const { left, top } = target.getBoundingClientRect();
        const [vWidth, vHeight] = [window.innerWidth, window.innerHeight];
        const [oWidth, oHeight] = [overlay.offsetWidth, overlay.offsetHeight];
        const scrollbarWidth = calculateScrollbarWidth();
        const topbarEl = document.querySelector('.layout-topbar');
        const topbarHeight = (topbarEl as HTMLElement)?.offsetHeight || 0;

        // reset
        overlay.style.top = overlay.style.left = '';

        if (isHorizontal.value) {
            const width = left + oWidth + scrollbarWidth;
            overlay.style.left = vWidth < width ? `${left - (width - vWidth)}px` : `${left}px`;
        } else if (isSlim.value || isSlimPlus.value) {
            const topOffset = top - topbarHeight;
            const height = topOffset + oHeight + topbarHeight;
            overlay.style.top = vHeight < height ? `${topOffset - (height - vHeight)}px` : `${topOffset}px`;
        }
    }
}

function checkActiveRoute(item: any) {
    return route.path === item.itemUrlPath;
}
</script>

<template>
    <li ref="menuItemRef" :class="{ 'layout-root-menuitem': root, 'active-menuitem': isActiveMenu }">
        <div v-if="root && item.enable !== false && (item.folderLabel || item.itemLabel)" class="layout-menuitem-root-text">
            <span>{{ item.folderLabel || item.itemLabel }}</span> <i class="layout-menuitem-root-icon pi pi-fw pi-ellipsis-h"></i>
        </div>

        <a
            v-if="(!item.itemUrlPath || item.folderItems) && item.enable !== false"
            :href="item.url"
            @click="itemClick($event, item)"
            :class="item.class"
            :target="item.target"
            tabindex="0"
            @mouseenter="onMouseEnter"
            v-tooltip.hover="isSlim && root && !isActiveMenu ? item.folderLabel || item.itemLabel : null"
        >
            <i :class="item.folderIcon || item.itemIcon" class="layout-menuitem-icon"></i>
            <span class="layout-menuitem-text">{{ item.folderLabel || item.itemLabel }}</span>
            <i class="pi pi-fw pi-angle-down layout-submenu-toggler" v-if="item.folderItems"></i>
        </a>
        <router-link
            v-if="item.itemUrlPath && !item.folderItems && item.enable !== false"
            @click="itemClick($event, item)"
            :class="[item.class, { 'active-route': checkActiveRoute(item) }]"
            tabindex="0"
            :to="item.itemUrlPath || '/'"
            @mouseenter="onMouseEnter"
            v-tooltip.hover="(isSlim || isSlimPlus) && root && !isActiveMenu ? item.folderLabel || item.itemLabel : null"
        >
            <i :class="item.folderIcon || item.itemIcon" class="layout-menuitem-icon"></i>
            <span class="layout-menuitem-text">{{ item.folderLabel || item.itemLabel }}</span>
            <i class="pi pi-fw pi-angle-down layout-submenu-toggler" v-if="item.folderItems"></i>
        </router-link>

        <ul ref="subMenuRef" :class="{ 'layout-root-submenulist': root }" v-if="item.folderItems && item.enable !== false">
            <AppMenuItem v-for="(child, i) in item.folderItems" :key="i" :index="i" :item="child" :parentItemKey="itemKey" :root="false"></AppMenuItem>
        </ul>
    </li>
</template>
