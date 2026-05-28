<template>
  <AppShell>
    <section class="panel p-5">
      <div class="flex items-center justify-between">
        <div>
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Audit trail</p>
          <h3 class="mt-1 text-lg font-semibold">Operational feed</h3>
        </div>
        <button class="button-secondary" @click="load">Refresh</button>
      </div>

      <div class="mt-5 grid gap-4 lg:grid-cols-2">
        <article v-for="event in events" :key="event.id" class="rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div class="flex items-center justify-between gap-4">
            <p class="text-xs font-semibold uppercase tracking-[0.18em] text-teal-700">{{ event.type }}</p>
            <p class="text-xs text-slate-400">{{ shortDate(event.createdAtUtc) }}</p>
          </div>
          <h4 class="mt-2 font-semibold text-slate-900">{{ event.title }}</h4>
          <p class="mt-2 text-sm text-slate-600">{{ event.description }}</p>
          <p class="mt-4 text-xs text-slate-400">by {{ event.createdBy }}</p>
        </article>
      </div>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import AppShell from '@/layouts/AppShell.vue'
import { api } from '@/api/backoffice'
import { shortDate } from '@/lib/format'
import type { AuditEvent } from '@/lib/types'

const events = ref<AuditEvent[]>([])

async function load() {
  events.value = (await api.activity()).items
}

onMounted(load)
</script>
