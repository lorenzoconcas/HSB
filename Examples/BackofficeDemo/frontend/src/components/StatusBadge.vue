<template>
  <span class="badge-base" :class="badgeClass">{{ value }}</span>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{ value: string }>()

const badgeClass = computed(() => {
  switch (props.value.toLowerCase()) {
    case 'confirmed':
    case 'completed':
      return 'bg-emerald-100 text-emerald-700'
    case 'packed':
    case 'shipped':
      return 'bg-sky-100 text-sky-700'
    case 'cancelled':
      return 'bg-rose-100 text-rose-700'
    default:
      return 'bg-amber-100 text-amber-700'
  }
})
</script>
