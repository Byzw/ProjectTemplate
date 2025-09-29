<script setup lang="ts">
import { useLayout } from '@/layout/composables/layout';
import { useThemeStore } from '@/stores/themeStore';
import { $t, updatePreset, updateSurfacePalette } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';
import Lara from '@primeuix/themes/lara';
import { nextTick, onMounted, ref } from 'vue';

const themeStore = useThemeStore();
const layout = useLayout();

const showConfigButton = ref(false);

onMounted(() => {
    nextTick(() => {
        applyTheme(); // 初始載入時還原主題
    });
});

defineProps({
    simple: {
        type: Boolean,
        default: false
    }
});

const presets = { Aura, Lara };
const presetOptions = ref(Object.keys(presets));
const preset = ref(themeStore.preset);
const themeOptions = ref([
    { name: 'Light', value: false },
    { name: 'Dark', value: true }
]);
const menuThemeOptions = ref([
    { name: 'Light', value: 'light', disabled: themeStore.darkTheme },
    { name: 'Dark', value: 'dark', disabled: false }
]);

const darkTheme = ref(themeStore.darkTheme);
const menuMode = ref(themeStore.menuMode);
const menuProfilePosition = ref(themeStore.menuProfilePosition);
const selectedScene = ref(null);

const primaryColors = ref([
    { name: 'noir', palette: {} },
    { name: 'emerald', palette: { 50: '#ecfdf5', 100: '#d1fae5', 200: '#a7f3d0', 300: '#6ee7b7', 400: '#34d399', 500: '#10b981', 600: '#059669', 700: '#047857', 800: '#065f46', 900: '#064e3b', 950: '#022c22' } },
    { name: 'green', palette: { 50: '#f0fdf4', 100: '#dcfce7', 200: '#bbf7d0', 300: '#86efac', 400: '#4ade80', 500: '#22c55e', 600: '#16a34a', 700: '#15803d', 800: '#166534', 900: '#14532d', 950: '#052e16' } },
    { name: 'lime', palette: { 50: '#f7fee7', 100: '#ecfccb', 200: '#d9f99d', 300: '#bef264', 400: '#a3e635', 500: '#84cc16', 600: '#65a30d', 700: '#4d7c0f', 800: '#3f6212', 900: '#365314', 950: '#1a2e05' } },
    { name: 'orange', palette: { 50: '#fff7ed', 100: '#ffedd5', 200: '#fed7aa', 300: '#fdba74', 400: '#fb923c', 500: '#f97316', 600: '#ea580c', 700: '#c2410c', 800: '#9a3412', 900: '#7c2d12', 950: '#431407' } },
    { name: 'amber', palette: { 50: '#fffbeb', 100: '#fef3c7', 200: '#fde68a', 300: '#fcd34d', 400: '#fbbf24', 500: '#f59e0b', 600: '#d97706', 700: '#b45309', 800: '#92400e', 900: '#78350f', 950: '#451a03' } },
    { name: 'yellow', palette: { 50: '#fefce8', 100: '#fef9c3', 200: '#fef08a', 300: '#fde047', 400: '#facc15', 500: '#eab308', 600: '#ca8a04', 700: '#a16207', 800: '#854d0e', 900: '#713f12', 950: '#422006' } },
    { name: 'teal', palette: { 50: '#f0fdfa', 100: '#ccfbf1', 200: '#99f6e4', 300: '#5eead4', 400: '#2dd4bf', 500: '#14b8a6', 600: '#0d9488', 700: '#0f766e', 800: '#115e59', 900: '#134e4a', 950: '#042f2e' } },
    { name: 'cyan', palette: { 50: '#ecfeff', 100: '#cffafe', 200: '#a5f3fc', 300: '#67e8f9', 400: '#22d3ee', 500: '#06b6d4', 600: '#0891b2', 700: '#0e7490', 800: '#155e75', 900: '#164e63', 950: '#083344' } },
    { name: 'sky', palette: { 50: '#f0f9ff', 100: '#e0f2fe', 200: '#bae6fd', 300: '#7dd3fc', 400: '#38bdf8', 500: '#0ea5e9', 600: '#0284c7', 700: '#0369a1', 800: '#075985', 900: '#0c4a6e', 950: '#082f49' } },
    { name: 'blue', palette: { 50: '#eff6ff', 100: '#dbeafe', 200: '#bfdbfe', 300: '#93c5fd', 400: '#60a5fa', 500: '#3b82f6', 600: '#2563eb', 700: '#1d4ed8', 800: '#1e40af', 900: '#1e3a8a', 950: '#172554' } },
    { name: 'indigo', palette: { 50: '#eef2ff', 100: '#e0e7ff', 200: '#c7d2fe', 300: '#a5b4fc', 400: '#818cf8', 500: '#6366f1', 600: '#4f46e5', 700: '#4338ca', 800: '#3730a3', 900: '#312e81', 950: '#1e1b4b' } },
    { name: 'violet', palette: { 50: '#f5f3ff', 100: '#ede9fe', 200: '#ddd6fe', 300: '#c4b5fd', 400: '#a78bfa', 500: '#8b5cf6', 600: '#7c3aed', 700: '#6d28d9', 800: '#5b21b6', 900: '#4c1d95', 950: '#2e1065' } },
    { name: 'purple', palette: { 50: '#faf5ff', 100: '#f3e8ff', 200: '#e9d5ff', 300: '#d8b4fe', 400: '#c084fc', 500: '#a855f7', 600: '#9333ea', 700: '#7e22ce', 800: '#6b21a8', 900: '#581c87', 950: '#3b0764' } },
    { name: 'fuchsia', palette: { 50: '#fdf4ff', 100: '#fae8ff', 200: '#f5d0fe', 300: '#f0abfc', 400: '#e879f9', 500: '#d946ef', 600: '#c026d3', 700: '#a21caf', 800: '#86198f', 900: '#701a75', 950: '#4a044e' } },
    { name: 'pink', palette: { 50: '#fdf2f8', 100: '#fce7f3', 200: '#fbcfe8', 300: '#f9a8d4', 400: '#f472b6', 500: '#ec4899', 600: '#db2777', 700: '#be185d', 800: '#9d174d', 900: '#831843', 950: '#500724' } },
    { name: 'rose', palette: { 50: '#fff1f2', 100: '#ffe4e6', 200: '#fecdd3', 300: '#fda4af', 400: '#fb7185', 500: '#f43f5e', 600: '#e11d48', 700: '#be123c', 800: '#9f1239', 900: '#881337', 950: '#4c0519' } },
    { name: 'currentproject', palette: { 50: '#e3f2fd', 100: '#bbdefb', 200: '#90caf9', 300: '#64b5f6', 400: '#42a5f5', 500: '#2196f3', 600: '#1e88e5', 700: '#1976d2', 800: '#1565c0', 900: '#0d47a1', 950: '#08306b' } }
]);

const surfaces = ref([
    {
        name: 'slate',
        palette: { 0: '#ffffff', 50: '#f8fafc', 100: '#f1f5f9', 200: '#e2e8f0', 300: '#cbd5e1', 400: '#94a3b8', 500: '#64748b', 600: '#475569', 700: '#334155', 800: '#1e293b', 900: '#0f172a', 950: '#020617' }
    },
    {
        name: 'gray',
        palette: { 0: '#ffffff', 50: '#f9fafb', 100: '#f3f4f6', 200: '#e5e7eb', 300: '#d1d5db', 400: '#9ca3af', 500: '#6b7280', 600: '#4b5563', 700: '#374151', 800: '#1f2937', 900: '#111827', 950: '#030712' }
    },
    {
        name: 'zinc',
        palette: { 0: '#ffffff', 50: '#fafafa', 100: '#f4f4f5', 200: '#e4e4e7', 300: '#d4d4d8', 400: '#a1a1aa', 500: '#71717a', 600: '#52525b', 700: '#3f3f46', 800: '#27272a', 900: '#18181b', 950: '#09090b' }
    },
    {
        name: 'neutral',
        palette: { 0: '#ffffff', 50: '#fafafa', 100: '#f5f5f5', 200: '#e5e5e5', 300: '#d4d4d4', 400: '#a3a3a3', 500: '#737373', 600: '#525252', 700: '#404040', 800: '#262626', 900: '#171717', 950: '#0a0a0a' }
    },
    {
        name: 'stone',
        palette: { 0: '#ffffff', 50: '#fafaf9', 100: '#f5f5f4', 200: '#e7e5e4', 300: '#d6d3d1', 400: '#a8a29e', 500: '#78716c', 600: '#57534e', 700: '#44403c', 800: '#292524', 900: '#1c1917', 950: '#0c0a09' }
    },
    {
        name: 'soho',
        palette: { 0: '#ffffff', 50: '#f4f4f4', 100: '#e8e9e9', 200: '#d2d2d4', 300: '#bbbcbe', 400: '#a5a5a9', 500: '#8e8f93', 600: '#77787d', 700: '#616268', 800: '#4a4b52', 900: '#34343d', 950: '#1d1e27' }
    },
    {
        name: 'viva',
        palette: { 0: '#ffffff', 50: '#f3f3f3', 100: '#e7e7e8', 200: '#cfd0d0', 300: '#b7b8b9', 400: '#9fa1a1', 500: '#87898a', 600: '#6e7173', 700: '#565a5b', 800: '#3e4244', 900: '#262b2c', 950: '#0e1315' }
    },
    {
        name: 'ocean',
        palette: { 0: '#ffffff', 50: '#fbfcfc', 100: '#F7F9F8', 200: '#EFF3F2', 300: '#DADEDD', 400: '#B1B7B6', 500: '#828787', 600: '#5F7274', 700: '#415B61', 800: '#29444E', 900: '#183240', 950: '#0c1920' }
    }
]);

const scenes = ref([
    {
        sceneName: 'CurrentProject Scene',
        isDarkTheme: false,
        colorSchemeColor: '#EFEFEF',
        menuTheme: 'light',
        menuThemeColor: '#ffffff',
        componentTheme: 'currentproject',
        componentThemeColor: '#4169E1',
        topbarTheme: 'currentproject',
        topbarThemeColor: '#f8fafc',
        menuMode: 'static',
        cardColor: '#ffffff'
    },
    {
        sceneName: 'Green Light',
        isDarkTheme: false,
        colorSchemeColor: '#EFEFEF',
        menuTheme: 'light',
        menuThemeColor: '#ffffff',
        componentTheme: 'green',
        componentThemeColor: '#198754',
        topbarTheme: 'green',
        topbarThemeColor: '#43A047',
        menuMode: 'static',
        cardColor: '#ffffff'
    },
    {
        sceneName: 'Dark Sea',
        isDarkTheme: true,
        colorSchemeColor: '#20262e',
        menuTheme: 'dark',
        menuThemeColor: '#2a323d',
        componentTheme: 'cyan',
        componentThemeColor: '#0dcaf0',
        topbarTheme: 'cyan',
        topbarThemeColor: '#0097A7',
        menuMode: 'static',
        cardColor: '#2a323d'
    },
    {
        sceneName: 'Blue Marble',
        isDarkTheme: false,
        colorSchemeColor: '#EFEFEF',
        menuTheme: 'light',
        menuThemeColor: '#ffffff',
        componentTheme: 'blue',
        componentThemeColor: '#0d6efd',
        topbarTheme: 'blue',
        topbarThemeColor: '#1565C0',
        menuMode: 'static',
        cardColor: '#ffffff'
    },
    {
        sceneName: 'Emerald',
        isDarkTheme: true,
        colorSchemeColor: '#20262e',
        menuTheme: 'dark',
        menuThemeColor: '#2a323d',
        componentTheme: 'teal',
        componentThemeColor: '#20c997',
        topbarTheme: 'teal',
        topbarThemeColor: '#00796B',
        menuMode: 'static',
        cardColor: '#2a323d'
    },
    {
        sceneName: 'Piano Black',
        isDarkTheme: false,
        colorSchemeColor: '#EFEFEF',
        menuTheme: 'light',
        menuThemeColor: '#ffffff',
        componentTheme: 'black',
        componentThemeColor: '#000000',
        topbarTheme: 'light',
        topbarThemeColor: '#FFFFFF',
        menuMode: 'static',
        cardColor: '#ffffff'
    },
    {
        sceneName: 'Bolt',
        isDarkTheme: true,
        colorSchemeColor: '#20262e',
        menuTheme: 'dark',
        menuThemeColor: '#2a323d',
        componentTheme: 'yellow',
        componentThemeColor: '#ffc107',
        topbarTheme: 'yellow',
        topbarThemeColor: '#FBC02D',
        menuMode: 'static',
        cardColor: '#2a323d'
    },
    {
        sceneName: 'Amber',
        isDarkTheme: false,
        colorSchemeColor: '#EFEFEF',
        menuTheme: 'dark',
        menuThemeColor: '#212529',
        componentTheme: 'yellow',
        componentThemeColor: '#ffc107',
        topbarTheme: 'yellow',
        topbarThemeColor: '#FBC02D',
        menuMode: 'horizontal',
        cardColor: '#ffffff'
    },
    {
        sceneName: 'Kingdom',
        isDarkTheme: true,
        colorSchemeColor: '#20262e',
        menuTheme: 'dark',
        menuThemeColor: '#2a323d',
        componentTheme: 'indigo',
        componentThemeColor: '#6610f2',
        topbarTheme: 'purple',
        topbarThemeColor: '#6A1B9A',
        menuMode: 'reveal',
        cardColor: '#2a323d'
    }
]);

const topbarThemes = ref([
    { name: 'light', color: '#FFFFFF' },
    { name: 'dark', color: '#212529' },
    { name: 'blue', color: '#1565C0' },
    { name: 'purple', color: '#6A1B9A' },
    { name: 'pink', color: '#AD1457' },
    { name: 'cyan', color: '#0097A7' },
    { name: 'teal', color: '#00796B' },
    { name: 'green', color: '#43A047' },
    { name: 'yellow', color: '#FBC02D' },
    { name: 'orange', color: '#FB8C00' },
    { name: 'indigo', color: '#3F51B5' },
    { name: 'currentproject', color: '#e3f2fd' }
]);

// getPresetExt：若未傳入 color，則從 themeStore.primary 中尋找
function getPresetExt(color: any = null) {
    if (!color) {
        color = primaryColors.value.find((c) => c.name === themeStore.primary);
    }
    if (color.name === 'noir') {
        return {
            semantic: {
                primary: {
                    50: '{surface.50}',
                    100: '{surface.100}',
                    200: '{surface.200}',
                    300: '{surface.300}',
                    400: '{surface.400}',
                    500: '{surface.500}',
                    600: '{surface.600}',
                    700: '{surface.700}',
                    800: '{surface.800}',
                    900: '{surface.900}',
                    950: '{surface.950}'
                },
                colorScheme: {
                    light: {
                        primary: {
                            color: '{primary.950}',
                            contrastColor: '#ffffff',
                            hoverColor: '{primary.800}',
                            activeColor: '{primary.700}'
                        },
                        highlight: {
                            background: '{primary.950}',
                            focusBackground: '{primary.700}',
                            color: '#ffffff',
                            focusColor: '#ffffff'
                        }
                    },
                    dark: {
                        primary: {
                            color: '{primary.50}',
                            contrastColor: '{primary.950}',
                            hoverColor: '{primary.200}',
                            activeColor: '{primary.300}'
                        },
                        highlight: {
                            background: '{primary.50}',
                            focusBackground: '{primary.300}',
                            color: '{primary.950}',
                            focusColor: '{primary.950}'
                        }
                    }
                }
            }
        };
    } else {
        return {
            semantic: {
                primary: color.palette,
                colorScheme: {
                    light: {
                        primary: {
                            color: '{primary.500}',
                            contrastColor: '#ffffff',
                            hoverColor: '{primary.600}',
                            activeColor: '{primary.700}'
                        },
                        highlight: {
                            background: '{primary.50}',
                            focusBackground: '{primary.100}',
                            color: '{primary.700}',
                            focusColor: '{primary.800}'
                        }
                    },
                    dark: {
                        primary: {
                            color: '{primary.400}',
                            contrastColor: '{surface.900}',
                            hoverColor: '{primary.300}',
                            activeColor: '{primary.200}'
                        },
                        highlight: {
                            background: 'color-mix(in srgb, {primary.400}, transparent 84%)',
                            focusBackground: 'color-mix(in srgb, {primary.400}, transparent 76%)',
                            color: 'rgba(255,255,255,.87)',
                            focusColor: 'rgba(255,255,255,.87)'
                        }
                    }
                }
            }
        };
    }
}

// applyTheme：根據 themeStore.primary 與 themeStore.surface 更新主題
function applyTheme() {
    const primaryColor = primaryColors.value.find((c) => c.name === themeStore.primary);
    if (primaryColor) {
        updatePreset(getPresetExt(primaryColor));
    }
    const surfaceSetting = surfaces.value.find((s) => s.name === themeStore.surface);
    if (surfaceSetting) {
        updateSurfacePalette(surfaceSetting.palette);
    }

    layout.setTopbarTheme(themeStore.topbarTheme);
    layout.setMenuMode(themeStore.menuMode);

    layout.layoutConfig.darkTheme = themeStore.darkTheme;
    layout.setDarkMode(themeStore.darkTheme);
}

// updateColors：更新主題設定後呼叫 applyTheme()
function updateColors(type: any = null, color: any = null) {
    if (type === 'primary') {
        themeStore.setPrimary(color.name);
    } else if (type === 'surface') {
        themeStore.setSurface(color.name);
    }
    applyTheme();
}

function onPresetChange() {
    themeStore.setPreset(preset.value);
    const presetValue = presets[preset.value as keyof typeof presets];
    const surfacePalette = surfaces.value.find((s) => s.name === themeStore.surface)?.palette;
    $t().preset(presetValue).preset(getPresetExt()).surfacePalette(surfacePalette).use({ useDefaultOptions: true });
}

function changeScene(item: any) {
    if (themeStore.darkTheme !== item.isDarkTheme) {
        layout.toggleDarkMode();
    }
    themeStore.setTopbarTheme(item.topbarTheme);
    themeStore.setMenuMode(item.menuMode);
    themeStore.setDarkTheme(item.isDarkTheme);
    themeStore.setMenuProfilePosition(item.menuProfilePosition);

    selectedScene.value = item.sceneName;
    if (item.componentTheme) {
        themeStore.setPrimary(item.componentTheme);
    }
    applyTheme();
}
</script>

<template>
    <button class="layout-config-button config-link" type="button" @click="layout.onConfigSidebarToggle"
        :style="{ display: showConfigButton ? 'block' : 'none' }">
        <i class="pi pi-cog"></i>
    </button>

    <Drawer v-model:visible="layout.layoutState.configSidebarVisible" position="right"
        class="layout-config-sidebar w-80" header="風格設定(先問再動喔)" :pt="{ pcCloseButton: { root: 'ml-auto' } }">
        <div class="flex flex-col gap-4">
            <div>
                <span class="text-lg text-muted-color font-semibold">Primary</span>
                <div class="pt-2 flex gap-2 flex-wrap">
                    <button v-for="primaryColor in primaryColors" :key="primaryColor.name" type="button"
                        @click="updateColors('primary', primaryColor)"
                        :class="['cursor-pointer w-6 h-6 rounded-full flex flex-shrink-0 items-center justify-center p-0 outline-none outline-offset-1', { 'outline-primary': themeStore.primary === primaryColor.name }]"
                        :style="{ backgroundColor: primaryColor.name === 'noir' ? 'var(--text-color)' : primaryColor.palette['500'] }"></button>
                </div>
            </div>

            <div>
                <span class="text-lg text-muted-color font-semibold">Surface</span>
                <div class="pt-2 flex gap-2 flex-wrap">
                    <button v-for="surface in surfaces" :key="surface.name" type="button"
                        @click="updateColors('surface', surface)" :class="[
                            'cursor-pointer w-6 h-6 rounded-full flex flex-shrink-0 items-center justify-center p-0 outline-none outline-offset-1',
                            { 'outline-primary': themeStore.surface ? themeStore.surface === surface.name : layout.isDarkTheme ? surface.name === 'zinc' : surface.name === 'slate' }
                        ]" :style="{ backgroundColor: surface.palette['500'] }"></button>
                </div>
            </div>

            <div>
                <div class="flex flex-col gap-2">
                    <span class="text-lg text-muted-color font-semibold">Presets</span>
                    <SelectButton v-model="preset" @change="onPresetChange" :options="presetOptions"
                        :allowEmpty="false" />
                </div>
            </div>

            <div>
                <div class="flex flex-col gap-2">
                    <span class="text-lg text-muted-color font-semibold">深色模式</span>
                    <SelectButton v-model="darkTheme" @change="
                        layout.toggleDarkMode();
                    themeStore.setDarkTheme(!layout.layoutConfig.darkTheme);
                    " :options="themeOptions" optionLabel="name" optionValue="value" :allowEmpty="false" />
                </div>
            </div>

            <template v-if="!simple">
                <div>
                    <div class="flex flex-col gap-2">
                        <span class="text-lg text-muted-color font-semibold">選單類型</span>
                        <div class="flex flex-wrap flex-col gap-3">
                            <div class="flex">
                                <div class="flex items-center gap-2 w-1/2">
                                    <RadioButton name="menuMode" value="static" v-model="menuMode" @update:modelValue="(val: any = null) => {
                                        layout.setMenuMode(val);
                                        themeStore.setMenuMode(val);
                                    }
                                    " inputId="mode1"></RadioButton>
                                    <label for="mode1">Static</label>
                                </div>
                                <div class="flex items-center gap-2 w-1/2">
                                    <RadioButton name="menuMode" value="overlay" v-model="menuMode" @update:modelValue="(val: any = null) => {
                                        layout.setMenuMode(val);
                                        themeStore.setMenuMode(val);
                                    }
                                    " inputId="mode2"></RadioButton>
                                    <label for="mode2">Overlay</label>
                                </div>
                            </div>
                            <div class="flex">
                                <div class="flex items-center gap-2 w-1/2">
                                    <RadioButton name="menuMode" value="slim" v-model="menuMode" @update:modelValue="(val: any = null) => {
                                        layout.setMenuMode(val);
                                        themeStore.setMenuMode(val);
                                    }
                                    " inputId="mode3"></RadioButton>
                                    <label for="mode3">Slim</label>
                                </div>
                                <div class="flex items-center gap-2 w-1/2">
                                    <RadioButton name="menuMode" value="slim-plus" v-model="menuMode"
                                        @update:modelValue="(val: any = null) => {
                                            layout.setMenuMode(val);
                                            themeStore.setMenuMode(val);
                                        }
                                        " inputId="mode4"></RadioButton>
                                    <label for="mode4">Slim+</label>
                                </div>
                            </div>
                            <div class="flex">
                                <div class="flex items-center gap-2 w-1/2">
                                    <RadioButton name="menuMode" value="reveal" v-model="menuMode" @update:modelValue="(val: any = null) => {
                                        layout.setMenuMode(val);
                                        themeStore.setMenuMode(val);
                                    }
                                    " inputId="mode5"></RadioButton>
                                    <label for="mode5">Reveal</label>
                                </div>
                                <div class="flex items-center gap-2 w-1/2">
                                    <RadioButton name="menuMode" value="drawer" v-model="menuMode" @update:modelValue="(val: any = null) => {
                                        layout.setMenuMode(val);
                                        themeStore.setMenuMode(val);
                                    }
                                    " inputId="mode6"></RadioButton>
                                    <label for="mode6">Drawer</label>
                                </div>
                            </div>
                            <div class="flex">
                                <div class="flex items-center gap-2 w-1/2">
                                    <RadioButton name="menuMode" value="horizontal" v-model="menuMode"
                                        @update:modelValue="(val: any = null) => {
                                            layout.setMenuMode(val);
                                            themeStore.setMenuMode(val);
                                        }
                                        " inputId="mode7"></RadioButton>
                                    <label for="mode7">Horizontal</label>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div>
                    <div class="flex flex-col gap-4">
                        <span class="text-lg text-muted-color font-semibold">Menu Profile Position</span>
                        <div class="flex">
                            <div class="flex items-center gap-2 w-1/2">
                                <RadioButton name="menuProfilePosition" value="start" v-model="menuProfilePosition"
                                    @update:modelValue="layout.setProfilePosition" inputId="profile1"></RadioButton>
                                <label for="profile1">Start</label>
                            </div>
                            <div class="flex items-center gap-2 w-1/2">
                                <RadioButton name="menuProfilePosition" value="end" v-model="menuProfilePosition"
                                    @update:modelValue="layout.setProfilePosition" inputId="profile2"></RadioButton>
                                <label for="profile2">End</label>
                            </div>
                        </div>
                    </div>
                </div>

                <div>
                    <div class="flex flex-col gap-4">
                        <span class="text-lg text-muted-color font-semibold">Menu Theme</span>
                        <template v-if="!layout.isDarkTheme">
                            <SelectButton v-model="layout.layoutConfig.menuTheme" :options="menuThemeOptions"
                                optionLabel="name" optionValue="value" optionDisabled="disabled" :allowEmpty="false" />
                        </template>
                        <template v-else>
                            <p>本功能請在亮色模式下用，僅為批次設定，可以不用動。</p>
                        </template>
                    </div>
                </div>

                <div>
                    <div class="flex flex-col gap-4">
                        <span class="text-lg text-muted-color font-semibold">Scenes</span>
                        <div class="flex flex-wrap p-2 bg-surface-100 dark:bg-surface-700 gap-2 rounded-lg">
                            <Button class="!bg-transparent !border-0 !p-0" v-for="scene in scenes"
                                :key="scene.sceneName" style="flex: 0 0 48%" v-tooltip.top="scene.sceneName"
                                @click="changeScene(scene)">
                                <div v-if="selectedScene === scene.sceneName"
                                    class="absolute w-full h-full flex justify-center items-center"
                                    style="background: rgba(255, 255, 255, 0.85); backdrop-filter: blur(3.56688px)">
                                    <svg width="25" height="26" viewBox="0 0 25 26" fill="none"
                                        xmlns="http://www.w3.org/2000/svg">
                                        <rect x="0.520691" y="0.770691" width="24.4586" height="24.4586" rx="12.2293"
                                            :fill="scene.componentThemeColor" />
                                        <g clip-path="url(#clip0_1_16289)">
                                            <path
                                                d="M11.1158 16.5119C11.0587 16.51 11.0025 16.4964 10.9507 16.472C10.899 16.4476 10.8528 16.4129 10.8149 16.37L7.97597 13.531C7.92185 13.4959 7.8764 13.449 7.84306 13.3938C7.80973 13.3385 7.78938 13.2765 7.78354 13.2122C7.77771 13.148 7.78655 13.0832 7.8094 13.0229C7.83224 12.9626 7.8685 12.9082 7.91542 12.864C7.96234 12.8197 8.01871 12.7867 8.08027 12.7674C8.14183 12.7481 8.20696 12.743 8.27076 12.7526C8.33456 12.7621 8.39535 12.7861 8.44854 12.8226C8.50174 12.8591 8.54595 12.9072 8.57783 12.9632L11.1158 15.4842L17.0606 9.55651C17.1406 9.50462 17.2358 9.4811 17.3308 9.48972C17.4258 9.49834 17.5151 9.53861 17.5845 9.60406C17.6539 9.66952 17.6993 9.75637 17.7134 9.8507C17.7275 9.94503 17.7096 10.0414 17.6625 10.1243L11.4168 16.37C11.3789 16.4129 11.3327 16.4476 11.281 16.472C11.2292 16.4964 11.173 16.51 11.1158 16.5119Z"
                                                fill="white" />
                                        </g>
                                        <defs>
                                            <clipPath id="clip0_1_16289">
                                                <rect width="10.7006" height="10.7006" fill="white"
                                                    transform="translate(7.39966 7.64966)" />
                                            </clipPath>
                                        </defs>
                                    </svg>
                                </div>
                                <svg width="110" height="44.5" viewBox="0 0 110 56" fill="none"
                                    xmlns="http://www.w3.org/2000/svg">
                                    <g clip-path="url(#clip0_1_23714)">
                                        <rect x="0.5" width="109.5" height="56" rx="6" :fill="scene.colorSchemeColor" />
                                        <rect width="109.5" height="10.5" transform="translate(0.5)"
                                            :fill="scene.topbarThemeColor" />
                                        <rect width="42" height="45.5" transform="translate(0.5 10.5)"
                                            :fill="scene.menuThemeColor" />
                                        <rect x="11" y="24.5" width="21" height="3.5" rx="1.75"
                                            :fill="scene.componentThemeColor" />
                                        <rect x="11" y="31.5" width="21" height="3.5" rx="1.75"
                                            :fill="scene.componentThemeColor" />
                                        <rect x="11" y="38.5" width="21" height="3.5" rx="1.75"
                                            :fill="scene.componentThemeColor" />
                                        <rect x="53" y="21" width="46.5" height="24.5" rx="3" :fill="scene.cardColor" />
                                        <rect x="60" y="28" width="32.5" height="10.5" rx="3"
                                            :fill="scene.componentThemeColor" />
                                    </g>
                                    <defs>
                                        <clipPath id="clip0_1_23714">
                                            <rect x="0.5" width="109.5" height="56" rx="6" fill="white" />
                                        </clipPath>
                                    </defs>
                                </svg>
                            </Button>
                        </div>
                    </div>
                </div>

                <div>
                    <div class="flex flex-col gap-4">
                        <span class="text-lg text-muted-color font-semibold">Topbar Themes</span>
                        <div class="pt-2 flex gap-2 flex-wrap">
                            <button v-for="theme in topbarThemes" :key="theme.name" type="button" @click="
                                layout.setTopbarTheme(theme.name);
                            themeStore.setTopbarTheme(theme.name);
                            " :class="['cursor-pointer w-6 h-6 rounded-full flex flex-shrink-0 items-center justify-center p-0 outline-none outline-offset-1 shadow']"
                                :style="{ backgroundColor: theme.color }">
                                <i v-if="theme.name === themeStore.topbarTheme"
                                    :class="['pi pi-check', theme.name === themeStore.topbarTheme && themeStore.topbarTheme !== 'light' ? 'text-white' : 'text-dark']"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </template>
        </div>
    </Drawer>
</template>
