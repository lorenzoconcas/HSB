<template>
  <AppShell>
    <section class="grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_0.85fr]">
      <article class="panel overflow-hidden">
        <div class="border-b border-slate-200 px-5 py-4">
          <div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Catalog</p>
              <h3 class="mt-1 text-lg font-semibold">Products</h3>
            </div>
            <div class="grid gap-3 md:grid-cols-3">
              <input v-model="search" class="input-base" placeholder="Search name or SKU" />
              <select v-model="category" class="input-base">
                <option value="">All categories</option>
                <option v-for="item in categories" :key="item" :value="item">{{ item }}</option>
              </select>
              <label class="flex items-center gap-2 rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700">
                <input v-model="lowStockOnly" type="checkbox" />
                Low stock only
              </label>
            </div>
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full text-left text-sm">
            <thead class="bg-slate-50 text-slate-500">
              <tr>
                <th class="px-5 py-3 font-medium">Product</th>
                <th class="px-5 py-3 font-medium">Category</th>
                <th class="px-5 py-3 font-medium">Price</th>
                <th class="px-5 py-3 font-medium">Stock</th>
                <th class="px-5 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="product in products"
                :key="product.id"
                class="cursor-pointer border-t border-slate-100 transition hover:bg-slate-50"
                @click="selectProduct(product)"
              >
                <td class="px-5 py-4">
                  <div class="flex items-center gap-3">
                    <img
                      v-if="product.imageUrl"
                      :src="product.imageUrl"
                      :alt="product.name"
                      class="h-12 w-12 rounded-2xl object-cover"
                    />
                    <div v-else class="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100 text-xs font-semibold text-slate-400">IMG</div>
                    <div>
                      <p class="font-semibold text-slate-900">{{ product.name }}</p>
                      <p class="text-xs text-slate-500">{{ product.sku }}</p>
                    </div>
                  </div>
                </td>
                <td class="px-5 py-4 text-slate-700">{{ product.category }}</td>
                <td class="px-5 py-4 font-medium text-slate-900">{{ money(product.price) }}</td>
                <td class="px-5 py-4">
                  <span class="badge-base" :class="product.stockQuantity <= product.reorderLevel ? 'bg-rose-100 text-rose-700' : 'bg-emerald-100 text-emerald-700'">
                    {{ product.stockQuantity }} / reorder {{ product.reorderLevel }}
                  </span>
                </td>
                <td class="px-5 py-4">
                  <span class="badge-base" :class="product.isActive ? 'bg-slate-100 text-slate-700' : 'bg-amber-100 text-amber-700'">
                    {{ product.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article class="panel p-5">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Editor</p>
            <h3 class="mt-1 text-lg font-semibold">{{ selectedProductId ? 'Update product' : 'Create product' }}</h3>
          </div>
          <button class="button-secondary" @click="resetForm">Reset</button>
        </div>

        <form class="mt-5 space-y-4" @submit.prevent="submit">
          <div class="grid gap-4 md:grid-cols-2">
            <div>
              <label class="mb-2 block text-sm font-medium text-slate-700">SKU</label>
              <input v-model="form.sku" class="input-base" />
            </div>
            <div>
              <label class="mb-2 block text-sm font-medium text-slate-700">Category</label>
              <input v-model="form.category" class="input-base" />
            </div>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Name</label>
            <input v-model="form.name" class="input-base" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Description</label>
            <textarea v-model="form.description" class="input-base min-h-24"></textarea>
          </div>
          <div class="grid gap-4 md:grid-cols-3">
            <div>
              <label class="mb-2 block text-sm font-medium text-slate-700">Price</label>
              <input v-model.number="form.price" class="input-base" type="number" min="0" step="0.01" />
            </div>
            <div>
              <label class="mb-2 block text-sm font-medium text-slate-700">Stock</label>
              <input v-model.number="form.stockQuantity" class="input-base" type="number" min="0" />
            </div>
            <div>
              <label class="mb-2 block text-sm font-medium text-slate-700">Reorder level</label>
              <input v-model.number="form.reorderLevel" class="input-base" type="number" min="0" />
            </div>
          </div>
          <label class="flex items-center gap-2 text-sm text-slate-700">
            <input v-model="form.isActive" type="checkbox" />
            Active
          </label>
          <p v-if="error" class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ error }}</p>
          <div class="flex flex-wrap gap-3">
            <button class="button-primary" type="submit">{{ selectedProductId ? 'Save changes' : 'Create product' }}</button>
            <button v-if="selectedProductId" class="button-secondary" type="button" @click="adjustStock(1)">+1 stock</button>
            <button v-if="selectedProductId" class="button-secondary" type="button" @click="adjustStock(-1)">-1 stock</button>
          </div>
        </form>

        <div v-if="selectedProductId" class="mt-6 border-t border-slate-200 pt-5">
          <p class="text-sm font-semibold text-slate-700">Product image</p>
          <input class="mt-3 block w-full text-sm text-slate-600" type="file" accept="image/*" @change="uploadImage" />
        </div>
      </article>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import AppShell from '@/layouts/AppShell.vue'
import { api } from '@/api/backoffice'
import { money } from '@/lib/format'
import type { Product } from '@/lib/types'

const products = ref<Product[]>([])
const categories = ref<string[]>([])
const selectedProductId = ref('')
const search = ref('')
const category = ref('')
const lowStockOnly = ref(false)
const error = ref('')

const emptyForm = (): Product => ({
  id: '',
  sku: '',
  name: '',
  description: '',
  category: '',
  price: 0,
  stockQuantity: 0,
  reorderLevel: 0,
  isActive: true,
  imageUrl: '',
  createdAtUtc: '',
  updatedAtUtc: '',
})

const form = ref<Product>(emptyForm())

async function load() {
  const query = new URLSearchParams()
  if (search.value) query.set('search', search.value)
  if (category.value) query.set('category', category.value)
  if (lowStockOnly.value) query.set('lowStockOnly', 'true')

  const data = await api.listProducts(query.toString() ? `/?${query.toString()}` : '')
  products.value = data.result.items
  categories.value = data.categories
}

function resetForm() {
  selectedProductId.value = ''
  form.value = emptyForm()
  error.value = ''
}

function selectProduct(product: Product) {
  selectedProductId.value = product.id
  form.value = { ...product }
}

async function submit() {
  error.value = ''
  try {
    if (selectedProductId.value) {
      await api.updateProduct(selectedProductId.value, form.value)
    } else {
      await api.createProduct(form.value)
    }

    await load()
    resetForm()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to save product'
  }
}

async function adjustStock(quantityDelta: number) {
  if (!selectedProductId.value) return
  await api.updateProductStock(selectedProductId.value, quantityDelta)
  await load()
}

async function uploadImage(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file || !selectedProductId.value) return
  await api.uploadProductImage(selectedProductId.value, file)
  await load()
}

watch([search, category, lowStockOnly], load)
onMounted(load)
</script>
