<template>
  <AppShell>
    <section class="grid gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
      <div class="space-y-6">
        <article class="panel overflow-hidden">
          <div class="border-b border-slate-200 px-5 py-4">
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Warehouse</p>
            <h3 class="mt-1 text-lg font-semibold">Low stock watchlist</h3>
          </div>
          <div class="overflow-x-auto">
            <table class="min-w-full text-left text-sm">
              <thead class="bg-slate-50 text-slate-500">
                <tr>
                  <th class="px-5 py-3 font-medium">Product</th>
                  <th class="px-5 py-3 font-medium">Stock</th>
                  <th class="px-5 py-3 font-medium">Reorder</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="product in lowStock" :key="product.id" class="border-t border-slate-100">
                  <td class="px-5 py-4">
                    <p class="font-semibold text-slate-900">{{ product.name }}</p>
                    <p class="text-xs text-slate-500">{{ product.category }}</p>
                  </td>
                  <td class="px-5 py-4">
                    <span class="badge-base bg-rose-100 text-rose-700">{{ product.stockQuantity }}</span>
                  </td>
                  <td class="px-5 py-4 text-slate-700">{{ product.reorderLevel }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </article>

        <article class="panel overflow-hidden">
          <div class="border-b border-slate-200 px-5 py-4">
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Audit</p>
            <h3 class="mt-1 text-lg font-semibold">Inventory adjustments</h3>
          </div>
          <div class="overflow-x-auto">
            <table class="min-w-full text-left text-sm">
              <thead class="bg-slate-50 text-slate-500">
                <tr>
                  <th class="px-5 py-3 font-medium">Product</th>
                  <th class="px-5 py-3 font-medium">Type</th>
                  <th class="px-5 py-3 font-medium">Delta</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="adjustment in adjustments" :key="adjustment.id" class="border-t border-slate-100">
                  <td class="px-5 py-4">
                    <p class="font-semibold text-slate-900">{{ adjustment.productName }}</p>
                    <p class="text-xs text-slate-500">{{ shortDate(adjustment.createdAtUtc) }}</p>
                  </td>
                  <td class="px-5 py-4 text-slate-700">{{ adjustment.type }}</td>
                  <td class="px-5 py-4">
                    <span class="badge-base" :class="adjustment.quantityDelta >= 0 ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'">
                      {{ adjustment.quantityDelta }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </article>
      </div>

      <article class="panel p-5">
        <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Movement</p>
        <h3 class="mt-1 text-lg font-semibold">New adjustment</h3>
        <form class="mt-5 space-y-4" @submit.prevent="submit">
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Product</label>
            <select v-model="form.productId" class="input-base">
              <option value="">Select product</option>
              <option v-for="product in products" :key="product.id" :value="product.id">{{ product.name }}</option>
            </select>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Type</label>
            <select v-model="form.type" class="input-base">
              <option value="Restock">Restock</option>
              <option value="Correction">Correction</option>
              <option value="Damage">Damage</option>
              <option value="ManualDecrease">Manual decrease</option>
            </select>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Quantity delta</label>
            <input v-model.number="form.quantityDelta" class="input-base" type="number" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Reason</label>
            <textarea v-model="form.reason" class="input-base min-h-24"></textarea>
          </div>
          <p v-if="error" class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ error }}</p>
          <button class="button-primary" type="submit">Save adjustment</button>
        </form>
      </article>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import AppShell from '@/layouts/AppShell.vue'
import { api } from '@/api/backoffice'
import { shortDate } from '@/lib/format'
import type { InventoryAdjustment, Product } from '@/lib/types'

const lowStock = ref<Product[]>([])
const products = ref<Product[]>([])
const adjustments = ref<InventoryAdjustment[]>([])
const error = ref('')
const form = ref({
  productId: '',
  type: 'Restock',
  quantityDelta: 1,
  reason: '',
})

async function load() {
  ;[lowStock.value, adjustments.value] = await Promise.all([
    api.inventoryLowStock(),
    api.inventoryAdjustments(),
  ])
  products.value = (await api.listProducts()).result.items
}

async function submit() {
  error.value = ''
  try {
    await api.createAdjustment(form.value)
    form.value = { productId: '', type: 'Restock', quantityDelta: 1, reason: '' }
    await load()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to create adjustment'
  }
}

onMounted(load)
</script>
