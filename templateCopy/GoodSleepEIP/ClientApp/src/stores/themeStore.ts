import { defineStore } from 'pinia';

export interface ThemeState {
    primary: string;
    surface: string;
    preset: string;
    darkTheme: boolean;
    menuMode: string;
    topbarTheme: string;
    menuProfilePosition: string;
    selectedScene: string;
}

export const useThemeStore = defineStore('theme', {
    state: (): ThemeState => ({
        primary: localStorage.getItem('theme_primary') || 'currentproject',
        surface: localStorage.getItem('theme_surface') || 'slate',
        preset: localStorage.getItem('theme_preset') || 'Aura',
        darkTheme: localStorage.getItem('theme_darkTheme') === 'true' ? true : false,
        menuMode: localStorage.getItem('theme_menuMode') || 'static',   // static, overlay(選單自動關閉), slim, hidden
        topbarTheme: localStorage.getItem('theme_topbarTheme') || 'currentproject',
        menuProfilePosition: localStorage.getItem('theme_menuProfilePosition') || 'start',
        selectedScene: localStorage.getItem('theme_selectedScene') || 'CurrentProject Scene'
    }),
    actions: {
        setPrimary(this: ThemeState, val: string) {
            this.primary = val;
            localStorage.setItem('theme_primary', val);
        },
        setSurface(this: ThemeState, val: string) {
            this.surface = val;
            localStorage.setItem('theme_surface', val);
        },
        setPreset(this: ThemeState, val: string) {
            this.preset = val;
            localStorage.setItem('theme_preset', val);
        },
        setDarkTheme(this: ThemeState, val: boolean) {
            this.darkTheme = val;
            localStorage.setItem('theme_darkTheme', String(val));
        },
        setMenuMode(this: ThemeState, val: string) {
            this.menuMode = val;
            localStorage.setItem('theme_menuMode', val);
        },
        setTopbarTheme(this: ThemeState, val: string) {
            this.topbarTheme = val;
            localStorage.setItem('theme_topbarTheme', val);
        },
        setMenuProfilePosition(this: ThemeState, val: string) {
            this.menuProfilePosition = val;
            localStorage.setItem('theme_menuProfilePosition', val);
        },
        setSelectedScene(this: ThemeState, val: string) {
            this.selectedScene = val;
            localStorage.setItem('theme_selectedScene', val);
        }
    }
});
