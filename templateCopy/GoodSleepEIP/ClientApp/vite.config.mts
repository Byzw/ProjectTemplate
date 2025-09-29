import { PrimeVueResolver } from '@primevue/auto-import-resolver';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';
import Components from 'unplugin-vue-components/vite';
import { defineConfig } from 'vite';

// https://vitejs.dev/config/
export default defineConfig({
    css: {
        preprocessorOptions: {
            scss: {
                api: 'modern-compiler' // or 'modern'
            }
        }
    },
    plugins: [
        vue(),
        Components({
            resolvers: [PrimeVueResolver()]
        })
    ],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    optimizeDeps: {
        include: ['pinia', 'vue', 'primevue/**', '@primeuix/themes/aura', '@vuepic/vue-datepicker', '@vee-validate/i18n', 'date-fns/locale']
    },
    server: {
        proxy: {
            '/api/': {
                target: 'http://localhost:5566', // 目標伺服器
                changeOrigin: true, // 改變原始請求的來源，讓後端以為請求來自 target 伺服器(自己)，避免 CORS 問題
                //rewrite: (path) => path.replace(/^\/apiBase/, '') // 路徑重寫
            }
        }
    },
    build: {
        outDir: '../wwwroot', // 將構建輸出目錄設置為 wwwroot
        emptyOutDir: true // 清空目標目錄，確保構建文件不會混亂
    }
});
