<template>
  <AppShell>
    <section class="grid gap-6 lg:grid-cols-2">
      <article class="panel p-5">
        <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Runtime</p>
        <h3 class="mt-1 text-lg font-semibold">Security posture</h3>
        <div class="mt-5 space-y-3 text-sm text-slate-600">
          <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4">Middleware pipeline enabled with request id, timing and login throttling.</div>
          <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4">Security headers, request validation and token-bucket rate limiting are active.</div>
          <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4">WebSocket notifications require a valid bearer token session.</div>
        </div>
      </article>

      <article class="panel p-5">
        <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Profile</p>
        <h3 class="mt-1 text-lg font-semibold">{{ authStore.user?.fullName }}</h3>
        <p class="mt-2 text-sm text-slate-500">{{ authStore.user?.username }}</p>
        <div class="mt-5 flex flex-wrap gap-2">
          <span v-for="role in authStore.roles" :key="role" class="badge-base bg-accentSoft text-accent">{{ role }}</span>
        </div>
      </article>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import AppShell from '@/layouts/AppShell.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
</script>
