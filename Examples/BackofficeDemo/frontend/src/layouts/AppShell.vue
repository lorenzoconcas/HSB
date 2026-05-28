<template>
  <div class="flex min-h-screen">
      <aside class="hsb-dark-card hidden w-72 shrink-0 rounded-none border-y-0 border-l-0 p-5 lg:block">
        <div class="mb-8">
          <div class="hsb-diamond">
            <img src="@/assets/newlogo.png" alt="logo" class=" w-[72px]"/>
          </div>
          <p class="mt-6 text-xs font-semibold uppercase tracking-[0.28em] text-slate-400">HSB 0.0.22</p>
          <h1 class="mt-2 text-2xl font-bold text-white">Backoffice Demo</h1>
          <p class="mt-2 text-sm text-slate-300">Orders, inventory, customers, and live notifications on HSB.</p>
        </div>

        <nav class="space-y-2">
          <RouterLink
            v-for="item in navItems"
            :key="item.name"
            :to="item.to"
            class="hsb-nav-link"
            :class="$route.name === item.to.name ? 'hsb-nav-link-active' : ''"
          >
            <component :is="item.icon" class="h-4 w-4" />
            <span>{{ item.label }}</span>
          </RouterLink>
        </nav>

        <div class="hsb-dark-subcard mt-8 p-4">
          <p class="text-xs uppercase tracking-[0.22em] text-slate-400">Session</p>
          <p class="mt-2 text-lg font-semibold">{{ authStore.user?.fullName }}</p>
          <p class="text-sm text-slate-400">{{ authStore.user?.roles.join(', ') }}</p>
        </div>
      </aside>

      <div class="min-w-0 flex-1">
        <header class="hsb-dark-card mb-6 flex flex-col gap-4 rounded-none border-x-0 border-t-0 px-6 py-4 md:flex-row md:items-center md:justify-between">
          <div>
            <p class="text-sm font-medium text-slate-400">{{ pageEyebrow }}</p>
            <h2 class="text-2xl font-bold text-white">{{ pageTitle }}</h2>
          </div>

          <div class="flex flex-wrap items-center gap-3">
            <span class="badge-base" :class="notifications.connected ? 'bg-white/10 text-white' : 'bg-amber-100 text-amber-800'">
              {{ notifications.connected ? 'Live connected' : 'Reconnecting live feed' }}
            </span>
            <button class="button-secondary" @click="refreshPage">Refresh</button>
            <button class="button-primary" @click="logout">Logout</button>
          </div>
        </header>

        <div class="grid gap-6 px-6 pb-6 xl:grid-cols-[minmax(0,1fr)_320px]">
          <main class="min-w-0">
            <slot />
          </main>

          <aside class="space-y-6">
            <section class="panel p-5">
              <div class="flex items-center justify-between">
                <div>
                  <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Live feed</p>
                  <h3 class="mt-1 text-lg font-semibold">Notifications</h3>
                </div>
                <button class="button-secondary px-3 py-1.5 text-xs" @click="pingSocket">Ping</button>
              </div>

              <div class="mt-4 space-y-3">
                <article
                  v-for="item in notifications.items.slice(0, 8)"
                  :key="`${item.type}-${item.timestampUtc}`"
                  class="rounded-2xl border border-slate-200 bg-slate-50 p-3"
                >
                  <p class="text-xs font-semibold uppercase tracking-[0.18em] text-teal-700">{{ item.type }}</p>
                  <p class="mt-1 text-xs text-slate-500">{{ shortDate(item.timestampUtc) }}</p>
                </article>
                <p v-if="notifications.items.length === 0" class="rounded-2xl border border-dashed border-slate-300 p-5 text-sm text-slate-500">
                  No events yet. Notifications appear when orders, products, or inventory records change.
                </p>
              </div>
            </section>

            <section class="panel p-5">
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Shortcuts</p>
              <div class="mt-4 grid gap-3">
                <RouterLink class="button-secondary justify-start" :to="{ name: 'products' }">Create product</RouterLink>
                <RouterLink class="button-secondary justify-start" :to="{ name: 'orders' }">Create order</RouterLink>
                <RouterLink class="button-secondary justify-start" :to="{ name: 'inventory' }">Log adjustment</RouterLink>
              </div>
            </section>
          </aside>
        </div>
      </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { Activity, Boxes, LayoutDashboard, PackageCheck, Settings, ShoppingCart, Users, type LucideIcon } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import { useNotificationsStore } from '@/stores/notifications'
import { shortDate } from '@/lib/format'

interface NavItem {
  label: string
  name: string
  to: { name: string }
  icon: LucideIcon
}

const authStore = useAuthStore()
const notifications = useNotificationsStore()
const route = useRoute()
const router = useRouter()

const navItems: NavItem[] = [
  { label: 'Dashboard', name: 'dashboard', to: { name: 'dashboard' }, icon: LayoutDashboard },
  { label: 'Products', name: 'products', to: { name: 'products' }, icon: Boxes },
  { label: 'Customers', name: 'customers', to: { name: 'customers' }, icon: Users },
  { label: 'Orders', name: 'orders', to: { name: 'orders' }, icon: ShoppingCart },
  { label: 'Inventory', name: 'inventory', to: { name: 'inventory' }, icon: PackageCheck },
  { label: 'Activity', name: 'activity', to: { name: 'activity' }, icon: Activity },
  { label: 'Settings', name: 'settings', to: { name: 'settings' }, icon: Settings },
]

const pageTitle = computed(() => navItems.find((item) => item.name === route.name)?.label ?? 'Backoffice')
const pageEyebrow = computed(() => 'HSB business workspace')

function refreshPage() {
  router.go(0)
}

async function logout() {
  notifications.disconnect()
  await authStore.logout()
  await router.push({ name: 'login' })
}

function pingSocket() {
  notifications.socket?.send('ping')
}

onMounted(() => {
  if (authStore.token) {
    notifications.connect(authStore.token)
  }
})

onUnmounted(() => {
  notifications.disconnect()
})
</script>
