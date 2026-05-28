<template>
  <AppShell>
    <section class="grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_0.9fr]">
      <article class="panel overflow-hidden">
        <div class="border-b border-slate-200 px-5 py-4">
          <div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Operations</p>
              <h3 class="mt-1 text-lg font-semibold">Orders</h3>
            </div>
            <div class="grid gap-3 md:grid-cols-2">
              <input v-model="search" class="input-base" placeholder="Search order or customer" />
              <select v-model="status" class="input-base">
                <option value="">All statuses</option>
                <option v-for="item in statuses" :key="item" :value="item">{{ item }}</option>
              </select>
            </div>
          </div>
        </div>
        <div class="overflow-x-auto">
          <table class="min-w-full text-left text-sm">
            <thead class="bg-slate-50 text-slate-500">
              <tr>
                <th class="px-5 py-3 font-medium">Order</th>
                <th class="px-5 py-3 font-medium">Customer</th>
                <th class="px-5 py-3 font-medium">Total</th>
                <th class="px-5 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="order in orders"
                :key="order.id"
                class="cursor-pointer border-t border-slate-100 transition hover:bg-slate-50"
                @click="selectOrder(order)"
              >
                <td class="px-5 py-4">
                  <p class="font-semibold text-slate-900">{{ order.orderNumber }}</p>
                  <p class="text-xs text-slate-500">{{ shortDate(order.createdAtUtc) }}</p>
                </td>
                <td class="px-5 py-4 text-slate-700">{{ order.customerName }}</td>
                <td class="px-5 py-4 font-medium text-slate-900">{{ money(order.total) }}</td>
                <td class="px-5 py-4"><StatusBadge :value="order.status" /></td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article class="panel p-5">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Composer</p>
            <h3 class="mt-1 text-lg font-semibold">{{ selectedOrder ? 'Manage selected order' : 'Create order' }}</h3>
          </div>
          <button class="button-secondary" @click="resetForm">Reset</button>
        </div>

        <div v-if="selectedOrder" class="mt-5 space-y-4">
          <div class="rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <p class="text-sm font-semibold text-slate-900">{{ selectedOrder.orderNumber }}</p>
            <p class="mt-1 text-sm text-slate-500">{{ selectedOrder.customerName }} · {{ money(selectedOrder.total) }}</p>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Update status</label>
            <select v-model="statusForm" class="input-base">
              <option v-for="item in statuses" :key="item" :value="item">{{ item }}</option>
            </select>
          </div>
          <div class="flex flex-wrap gap-3">
            <button class="button-primary" @click="saveStatus">Save status</button>
            <button class="button-secondary" @click="api.exportOrdersCsv()">Export CSV</button>
          </div>
          <div class="border-t border-slate-200 pt-5">
            <p class="text-sm font-semibold text-slate-700">Attachment</p>
            <input class="mt-3 block w-full text-sm text-slate-600" type="file" @change="uploadAttachment" />
          </div>
        </div>

        <form v-else class="mt-5 space-y-4" @submit.prevent="submitOrder">
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Customer</label>
            <select v-model="orderForm.customerId" class="input-base">
              <option value="">Select customer</option>
              <option v-for="customer in customers" :key="customer.id" :value="customer.id">{{ customer.name }}</option>
            </select>
          </div>
          <div class="space-y-3">
            <div v-for="(item, index) in orderForm.items" :key="index" class="grid gap-3 md:grid-cols-[1fr_120px_auto]">
              <select v-model="item.productId" class="input-base">
                <option value="">Select product</option>
                <option v-for="product in products" :key="product.id" :value="product.id">{{ product.name }}</option>
              </select>
              <input v-model.number="item.quantity" class="input-base" type="number" min="1" />
              <button class="button-secondary" type="button" @click="removeItem(index)">Remove</button>
            </div>
          </div>
          <button class="button-secondary" type="button" @click="addItem">Add line</button>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Discount</label>
            <input v-model.number="orderForm.discount" class="input-base" type="number" min="0" step="0.01" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Notes</label>
            <textarea v-model="orderForm.notes" class="input-base min-h-24"></textarea>
          </div>
          <p v-if="error" class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ error }}</p>
          <button class="button-primary" type="submit">Create order</button>
        </form>
      </article>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import AppShell from '@/layouts/AppShell.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { api } from '@/api/backoffice'
import { money, shortDate } from '@/lib/format'
import type { Customer, OrderRecord, Product } from '@/lib/types'

const orders = ref<OrderRecord[]>([])
const customers = ref<Customer[]>([])
const products = ref<Product[]>([])
const selectedOrder = ref<OrderRecord | null>(null)
const statusForm = ref('Confirmed')
const search = ref('')
const status = ref('')
const error = ref('')

const statuses = ['Draft', 'Confirmed', 'Packed', 'Shipped', 'Completed', 'Cancelled']

const orderForm = ref({
  customerId: '',
  discount: 0,
  notes: '',
  items: [{ productId: '', quantity: 1 }],
})

async function load() {
  const query = new URLSearchParams()
  if (search.value) query.set('search', search.value)
  if (status.value) query.set('status', status.value)
  orders.value = (await api.listOrders(query.toString() ? `/?${query.toString()}` : '')).items
}

async function loadAux() {
  customers.value = (await api.listCustomers()).items
  products.value = (await api.listProducts()).result.items
}

function addItem() {
  orderForm.value.items.push({ productId: '', quantity: 1 })
}

function removeItem(index: number) {
  orderForm.value.items.splice(index, 1)
}

function resetForm() {
  selectedOrder.value = null
  statusForm.value = 'Confirmed'
  orderForm.value = {
    customerId: '',
    discount: 0,
    notes: '',
    items: [{ productId: '', quantity: 1 }],
  }
  error.value = ''
}

function selectOrder(order: OrderRecord) {
  selectedOrder.value = order
  statusForm.value = order.status
}

async function submitOrder() {
  error.value = ''
  try {
    await api.createOrder(orderForm.value)
    await load()
    resetForm()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to create order'
  }
}

async function saveStatus() {
  if (!selectedOrder.value) return
  await api.updateOrderStatus(selectedOrder.value.id, statusForm.value)
  await load()
}

async function uploadAttachment(event: Event) {
  if (!selectedOrder.value) return
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  await api.uploadOrderAttachment(selectedOrder.value.id, file)
  await load()
}

watch([search, status], load)
onMounted(async () => {
  await Promise.all([load(), loadAux()])
})
</script>
