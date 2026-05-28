<template>
  <AppShell>
    <section class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <StatCard label="Products" :value="summary?.totalProducts ?? '—'" hint="Catalog currently loaded" :icon="Boxes" />
      <StatCard label="Customers" :value="summary?.totalCustomers ?? '—'" hint="Accounts tracked in CRM" :icon="Users" />
      <StatCard label="Orders" :value="summary?.totalOrders ?? '—'" hint="Operational order volume" :icon="ShoppingCart" />
      <StatCard label="Monthly revenue" :value="money(summary?.revenueMonth ?? 0)" hint="Completed and active orders" :icon="Wallet" />
    </section>

    <section class="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_0.85fr]">
      <article class="panel p-5">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Last 7 days</p>
            <h3 class="mt-1 text-lg font-semibold">Sales trend</h3>
          </div>
          <span class="badge-base bg-slate-100 text-slate-700">{{ summary?.ordersToday ?? 0 }} orders today</span>
        </div>

        <div class="mt-6 flex h-64 items-end gap-3">
          <div v-for="point in sales" :key="point.label" class="flex flex-1 flex-col items-center gap-3">
            <div class="flex w-full items-end justify-center rounded-t-2xl bg-gradient-to-b from-teal-400 to-teal-700" :style="{ height: `${Math.max(16, point.value * scale)}px` }"></div>
            <div class="text-center">
              <p class="text-sm font-semibold text-slate-700">{{ money(point.value) }}</p>
              <p class="text-xs text-slate-500">{{ point.label }}</p>
            </div>
          </div>
        </div>
      </article>

      <article class="panel p-5">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Attention</p>
            <h3 class="mt-1 text-lg font-semibold">Low stock</h3>
          </div>
          <span class="badge-base bg-amber-100 text-amber-700">{{ summary?.lowStockItems ?? 0 }} items</span>
        </div>

        <div class="mt-4 space-y-3">
          <article v-for="product in lowStock" :key="product.id" class="rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <div class="flex items-center justify-between gap-4">
              <div>
                <p class="font-semibold text-slate-900">{{ product.name }}</p>
                <p class="text-sm text-slate-500">{{ product.category }} · {{ product.sku }}</p>
              </div>
              <span class="badge-base bg-rose-100 text-rose-700">{{ product.stockQuantity }} left</span>
            </div>
          </article>
        </div>
      </article>
    </section>

    <section class="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
      <article class="panel overflow-hidden">
        <div class="border-b border-slate-200 px-5 py-4">
          <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Operations</p>
          <h3 class="mt-1 text-lg font-semibold">Recent orders</h3>
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
              <tr v-for="order in recentOrders" :key="order.id" class="border-t border-slate-100">
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
        <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Activity</p>
        <h3 class="mt-1 text-lg font-semibold">Recent audit trail</h3>
        <div class="mt-4 space-y-3">
          <article v-for="event in activity" :key="event.id" class="rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <p class="text-xs font-semibold uppercase tracking-[0.18em] text-teal-700">{{ event.type }}</p>
            <p class="mt-1 font-semibold text-slate-900">{{ event.title }}</p>
            <p class="mt-2 text-sm text-slate-600">{{ event.description }}</p>
            <p class="mt-3 text-xs text-slate-400">{{ event.createdBy }} · {{ shortDate(event.createdAtUtc) }}</p>
          </article>
        </div>
      </article>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Boxes, ShoppingCart, Users, Wallet } from 'lucide-vue-next'
import AppShell from '@/layouts/AppShell.vue'
import StatCard from '@/components/StatCard.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { api } from '@/api/backoffice'
import type { AuditEvent, DashboardSummary, MetricPoint, OrderRecord, Product } from '@/lib/types'
import { money, shortDate } from '@/lib/format'

const summary = ref<DashboardSummary | null>(null)
const sales = ref<MetricPoint[]>([])
const lowStock = ref<Product[]>([])
const recentOrders = ref<OrderRecord[]>([])
const activity = ref<AuditEvent[]>([])

const scale = computed(() => {
  const max = Math.max(...sales.value.map((item) => item.value), 1)
  return 220 / max
})

onMounted(async () => {
  summary.value = await api.dashboardSummary()
  sales.value = await api.dashboardSales()
  lowStock.value = await api.dashboardLowStock()
  recentOrders.value = await api.dashboardRecentOrders()
  activity.value = (await api.dashboardActivity()).items
})
</script>
