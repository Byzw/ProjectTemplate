import { createPinia } from 'pinia';
import { createApp } from 'vue';
import App from './App.vue';
import router from './router';

import BlockViewer from '@/components/BlockViewer.vue';
import PrimeLocale from '@/locales/primelocale/zh-TW.json';
import Aura from '@primeuix/themes/aura';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import ToastService from 'primevue/toastservice';

import VueDatePicker from '@vuepic/vue-datepicker';
import '@vuepic/vue-datepicker/dist/main.css';
import { zhTW } from 'date-fns/locale';

import '@/assets/styles.scss';
import '@/assets/tailwind.css';

// 引入 VeeValidate
import { localize, setLocale } from '@vee-validate/i18n';
import { all } from '@vee-validate/rules';
import { ErrorMessage, Field, Form, configure, defineRule } from 'vee-validate';
Object.entries(all).forEach(([name, rule]) => {
    // 使用 Object.keys 將 AllRules 轉為陣列，使用 forEach 迴圈將驗證規則加入 VeeValidate
    defineRule(name, rule);
});
configure({
    generateMessage: localize({ zh_TW: zhTW as any }),
    validateOnInput: true
});
setLocale('zh_TW');

import { ModuleRegistry, provideGlobalGridOptions } from 'ag-grid-community';
import { AllEnterpriseModule, LicenseManager } from 'ag-grid-enterprise';
import 'ag-grid-enterprise/styles/ag-grid.css'; // Mandatory CSS required by the Data Grid
import 'ag-grid-enterprise/styles/ag-theme-quartz.min.css'; // Optional Theme applied to the Data Grid
import { AgGridVue } from 'ag-grid-vue3'; // Vue Data Grid Component

ModuleRegistry.registerModules([AllEnterpriseModule]); // Register all enterprise features
provideGlobalGridOptions({ theme: 'legacy' }); // Mark all grids as using legacy themes
// AG-Grid：
// 請注意: 33.0.1 之後，採用模組化載入，功能改動比較大
// 2025-01-16更新，版本33.0.4
//LicenseManager.setLicenseKey('[v3][Release][0102]_MTczOTgwMzI0NzgzNQ==3b9561ae3098b5c5b5fd2c89219db05d');

(async () => {
    await fetchInitialData(); // 先取得數據，再開始掛載 Vue App

    const app = createApp(App);
    const pinia = createPinia();

    app.use(router);
    app.use(pinia);
    app.use(PrimeVue, {
        locale: PrimeLocale['zh-TW'],
        theme: {
            preset: Aura,
            options: {
                darkModeSelector: '.app-dark'
            }
        }
    });

    app.use(ToastService);
    app.use(ConfirmationService);

    app.component('BlockViewer', BlockViewer);

    app.component('AgGridVue', AgGridVue); // 全局註冊 AgGridVue 組件

    // 掛載 VeeValidate 元件
    app.component('VField', Field);
    app.component('VForm', Form);
    app.component('ErrorMessage', ErrorMessage);
    app.component('VueDatePicker', VueDatePicker);

    app.mount("#app");

})();

async function fetchInitialData(timeout = 5000) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeout);

    try {
        const response = await fetch('/api/open/GetAgl');
        if (!response.ok) throw new Error(`Response error: ${response.status}`);

        const data = await response.json();
        if (!data.AG_GRID_LICENSE_KEY) throw new Error('Data not found!');

        LicenseManager.setLicenseKey(data.AG_GRID_LICENSE_KEY);
    } catch (error) {
        console.error('Something error about keys:', error);
    } finally {
        clearTimeout(timeoutId);
    }
}
