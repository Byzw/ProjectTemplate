<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const route = useRoute();
const router = useRouter();

const home = { icon: 'pi pi-home', to: '/' };
const items = ref<{ label: any }[]>([]);

function navigate() {
    router.push(home.to);
}

function watchRouter() {
    if (route.meta.breadcrumb) {
        items.value = [];
        const bc = Array.isArray(route.meta.breadcrumb) ? route.meta.breadcrumb[0] : {};

        for (let pro in bc) {
            items.value.push({ label: bc[pro] });
        }
    }
}

watch(
    route,
    () => {
        watchRouter();
    },
    { immediate: true }
);
</script>

<template>
    <div class="layout-breadcrumb-container">
        <nav class="layout-breadcrumb">
            <ol>
                <li>
                    <i :class="home.icon" @click="navigate"></i>
                </li>
                <li><i class="pi pi-angle-right"></i></li>
                <template v-for="(item, index) in items" :key="index">
                    <li>
                        <span> {{ item.label }}</span>
                    </li>
                    <li v-if="index !== items.length - 1">
                        <i class="pi pi-angle-right"></i>
                    </li>
                </template>
            </ol>
        </nav>

        <!-- <div class="layout-breadcrumb-buttons">
            <Button icon="pi pi-cloud-upload" rounded text plain></Button>
            <Button icon="pi pi-bookmark" rounded text plain></Button>
            <Button icon="pi pi-power-off" rounded text plain></Button>
        </div> -->
    </div>
</template>
