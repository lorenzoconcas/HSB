<template>
  <article class="panel p-5">
    <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">{{ label }}</p>
    <div class="mt-3 flex items-end justify-between gap-4">
      <div>
        <p class="text-3xl font-bold text-slate-950">{{ value }}</p>
        <p v-if="hint" class="mt-2 text-sm text-slate-500">{{ hint }}</p>
      </div>
      <div class="rounded-2xl bg-accentSoft p-3 text-accent">
        <component :is="icon" class="h-5 w-5" />
      </div>
    </div>
  </article>
</template>

<script setup lang="ts">
import type { LucideIcon } from 'lucide-vue-next'

defineProps<{
  label: string
  value: string | number
  hint?: string
  icon: LucideIcon
}>()
</script>
