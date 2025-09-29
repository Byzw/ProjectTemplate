<script setup lang="ts">
import { useAuthStore } from '@/stores/auth';
import { onMounted, ref } from 'vue';

const authStore = useAuthStore();

onMounted(() => {
    authStore.logout();
});
</script>
<template></template>