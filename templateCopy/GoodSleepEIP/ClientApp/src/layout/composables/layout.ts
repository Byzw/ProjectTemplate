import { computed, reactive, ref, watch } from 'vue';

const layoutConfig = reactive({
    preset: 'Aura',
    primary: 'noir',
    surface: null,
    darkTheme: false,
    menuMode: 'static',
    menuTheme: 'light',
    topbarTheme: 'light',
    menuProfilePosition: 'end'
});

const layoutState = reactive({
    staticMenuDesktopInactive: false,
    overlayMenuActive: false,
    configSidebarVisible: false,
    staticMenuMobileActive: false,
    menuHoverActive: false,
    rightMenuActive: false,
    topbarMenuActive: false,
    sidebarActive: false,
    anchored: false,
    activeMenuItem: null,
    overlaySubmenuActive: false,
    menuProfileActive: false
});

const outsideClickListener = ref<((event: any) => void) | null>(null);

export function useLayout() {
    const setPrimary = (value: any) => {
        layoutConfig.primary = value;
    };

    const setSurface = (value: any) => {
        layoutConfig.surface = value;
    };

    const setPreset = (value: any) => {
        layoutConfig.preset = value;
    };

    const setMenuMode = (mode: any) => {
        layoutConfig.menuMode = mode;

        if (mode === 'static') {
            layoutState.staticMenuDesktopInactive = false;
        }
    };

    const showConfigSidebar = () => {
        layoutState.configSidebarVisible = true;
    };

    const showSidebar = () => {
        layoutState.rightMenuActive = true;
    };

    const setTopbarTheme = (value: any) => {
        layoutConfig.topbarTheme = value;
    };

    const setProfilePosition = (value: any) => {
        layoutConfig.menuProfilePosition = value;
    };

    const onMenuProfileToggle = () => {
        layoutState.menuProfileActive = !layoutState.menuProfileActive;
    };

    const toggleDarkMode = () => {
        if (!document.startViewTransition) {
            executeDarkModeToggle();

            return;
        }

        document.startViewTransition(() => executeDarkModeToggle());
    };

    const executeDarkModeToggle = () => {
        layoutConfig.darkTheme = !layoutConfig.darkTheme;
        layoutConfig.menuTheme = isDarkTheme.value ? 'dark' : 'light';

        document.documentElement.classList.toggle('app-dark');

        updateAgGridDarkTheme();
    };

    const setDarkMode = (value: any) => {
        layoutConfig.darkTheme = value;
        layoutConfig.menuTheme = value ? 'dark' : 'light';

        if (value) document.documentElement.classList.add('app-dark');
        else document.documentElement.classList.remove('app-dark');

        updateAgGridDarkTheme();
    };

    // 更新 AG-Grid 的主題是否為黑色風格
    const updateAgGridDarkTheme = () => {
        const agGridElements = document.querySelectorAll('.ag-theme-quartz, .ag-theme-quartz-dark');
        agGridElements.forEach((gridElement) => {
            // 獲取當前的 class
            const currentClass = Array.from(gridElement.classList).find((cls) => cls.startsWith('ag-theme-'));
            if (currentClass) {
                if (layoutConfig.darkTheme) gridElement.classList.add('ag-theme-quartz-dark');
                else gridElement.classList.remove('ag-theme-quartz-dark');
            }
        });
    };

    const setActiveMenuItem = (item: any) => {
        layoutState.activeMenuItem = item.value || item;
    };

    const setMenuStates = (value: any) => {
        layoutState.overlaySubmenuActive = value;
        layoutState.menuHoverActive = value;
    };

    const setStaticMenuMobile = () => {
        layoutState.staticMenuMobileActive = !layoutState.staticMenuMobileActive;
    };

    const watchSidebarActive = () => {
        watch(isSidebarActive, (newVal) => {
            if (newVal) {
                bindOutsideClickListener();
            } else {
                unbindOutsideClickListener();
            }
        });
    };

    const onMenuToggle = () => {
        if (layoutConfig.menuMode === 'overlay') {
            layoutState.overlayMenuActive = !layoutState.overlayMenuActive;
        }

        if (window.innerWidth > 991) {
            layoutState.staticMenuDesktopInactive = !layoutState.staticMenuDesktopInactive;
        } else {
            layoutState.staticMenuMobileActive = !layoutState.staticMenuMobileActive;
        }
    };
    const onTopbarMenuToggle = () => {
        layoutState.topbarMenuActive = !layoutState.topbarMenuActive;
    };

    const onProfileSidebarToggle = () => {
        layoutState.rightMenuActive = !layoutState.rightMenuActive;
    };

    const onConfigSidebarToggle = () => {
        if (isSidebarActive.value) {
            resetMenu();
            unbindOutsideClickListener();
        }

        layoutState.configSidebarVisible = !layoutState.configSidebarVisible;
    };

    const onSidebarToggle = (value: any) => {
        layoutState.sidebarActive = value;
    };

    const onAnchorToggle = () => {
        layoutState.anchored = !layoutState.anchored;
    };

    const bindOutsideClickListener = () => {
        if (!outsideClickListener.value) {
            outsideClickListener.value = (event) => {
                if (isOutsideClicked(event)) {
                    resetMenu();
                }
            };
            document.addEventListener('click', outsideClickListener.value);
        }
    };

    const unbindOutsideClickListener = () => {
        if (outsideClickListener.value) {
            document.removeEventListener('click', outsideClickListener.value);
            outsideClickListener.value = null;
        }
    };

    const isOutsideClicked = (event: any) => {
        const sidebarEl = document.querySelector('.layout-sidebar');
        const topbarButtonEl = document.querySelector('.layout-menu-button');

        return !(sidebarEl?.isSameNode(event.target) || sidebarEl?.contains(event.target) || topbarButtonEl?.isSameNode(event.target) || topbarButtonEl?.contains(event.target));
    };

    const resetMenu = () => {
        layoutState.overlayMenuActive = false;
        layoutState.overlaySubmenuActive = false;
        layoutState.staticMenuMobileActive = false;
        layoutState.menuHoverActive = false;
        layoutState.configSidebarVisible = false;
    };

    const isSidebarActive = computed(() => layoutState.overlayMenuActive || layoutState.staticMenuMobileActive || layoutState.overlaySubmenuActive);

    const isDesktop = computed(() => window.innerWidth > 991);

    const isSlim = computed(() => layoutConfig.menuMode === 'slim');
    const isSlimPlus = computed(() => layoutConfig.menuMode === 'slim-plus');
    const isHorizontal = computed(() => layoutConfig.menuMode === 'horizontal');

    const isDarkTheme = computed(() => layoutConfig.darkTheme);
    const getPrimary = computed(() => layoutConfig.primary);
    const getSurface = computed(() => layoutConfig.surface);

    return {
        layoutConfig,
        layoutState,
        getPrimary,
        getSurface,
        isDarkTheme,
        setPrimary,
        setSurface,
        setPreset,
        setMenuMode,
        setTopbarTheme,
        setProfilePosition,
        onMenuProfileToggle,
        toggleDarkMode,
        setDarkMode,
        updateAgGridDarkTheme,
        onMenuToggle,
        onTopbarMenuToggle,
        onProfileSidebarToggle,
        setMenuStates,
        setStaticMenuMobile,
        watchSidebarActive,
        isSidebarActive,
        setActiveMenuItem,
        onConfigSidebarToggle,
        onSidebarToggle,
        onAnchorToggle,
        isSlim,
        isSlimPlus,
        isHorizontal,
        isDesktop,
        showConfigSidebar,
        showSidebar,
        unbindOutsideClickListener
    };
}
