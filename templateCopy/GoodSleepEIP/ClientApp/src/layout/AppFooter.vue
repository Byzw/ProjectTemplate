<script setup lang="ts">
import { useLayout } from '@/layout/composables/layout';

const { isDarkTheme } = useLayout();

const openLink = (url: string = '') => {
    window.open(url, '_blank');
};

const copyrightText = `
%c
██╗    ██╗██╗  ██╗██╗   ██╗███╗   ██╗    ██╗  ██╗███████╗███╗   ██╗ ██████╗██╗  ██╗██╗   ██╗ █████╗ 
██║    ██║██║  ██║██║   ██║████╗  ██║    ██║ ██╔╝██╔════╝████╗  ██║██╔════╝██║  ██║██║   ██║██╔══██╗
██║ █╗ ██║███████║██║   ██║██╔██╗ ██║    █████╔╝ █████╗  ██╔██╗ ██║██║ █╗  ███████║██║   ██║███████║
██║███╗██║██╔══██║██║   ██║██║╚██╗██║    ██╔═██╗ ██╔══╝  ██║╚██╗██║██║ ███ ██╔══██║██║   ██║██╔══██║
╚███╔███╔╝██║  ██║╚══███╔╝ ██║ ╚████║    ██║  ██║███████╗██║ ╚████║╚██████╗██║  ██║╚██████╔╝██║  ██║
 ╚══╝╚══╝ ╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═══╝    ╚═╝  ╚═╝╚══════╝╚═╝  ╚═══╝ ╚═════╝╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═╝

%c©2025 WHVNFramework, All Rights Reserved.
`;

console.log(copyrightText, 'color: #4CAF50; font-size: 12px;', 'color: #FFC107; font-size: 14px; font-weight: bold;');
console.log('%c', 'color: #4CAF50; font-size: 12px;');
</script>

<template>
    <div class="layout-footer">
        ©2025 WHVNFramework v1.7.0, All Rights Reserved.
        <div class="flex gap-2">
            <Button icon="pi pi-globe" rounded text plain @click="() => openLink('https://www.mstech.tw')"></Button>
            <!-- <Button icon="pi pi-facebook" rounded text plain @click="() => openLink('https://www.facebook.com/xxx')"></Button>
            <Button icon="pi pi-youtube" rounded text plain @click="() => openLink('https://www.youtube.com/channel/xxx')"></Button> -->
        </div>
    </div>
</template>
