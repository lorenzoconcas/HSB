import { createRouter, createWebHashHistory } from 'vue-router'
import LoginPage from '@/pages/LoginPage.vue'
import DashboardPage from '@/pages/DashboardPage.vue'
import ProductsPage from '@/pages/ProductsPage.vue'
import CustomersPage from '@/pages/CustomersPage.vue'
import OrdersPage from '@/pages/OrdersPage.vue'
import InventoryPage from '@/pages/InventoryPage.vue'
import ActivityPage from '@/pages/ActivityPage.vue'
import SettingsPage from '@/pages/SettingsPage.vue'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginPage, meta: { public: true } },
    { path: '/', name: 'dashboard', component: DashboardPage },
    { path: '/products', name: 'products', component: ProductsPage },
    { path: '/customers', name: 'customers', component: CustomersPage },
    { path: '/orders', name: 'orders', component: OrdersPage },
    { path: '/inventory', name: 'inventory', component: InventoryPage },
    { path: '/activity', name: 'activity', component: ActivityPage },
    { path: '/settings', name: 'settings', component: SettingsPage },
  ],
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()
  if (authStore.token && !authStore.user) {
    try {
      await authStore.fetchMe()
    } catch {
      await authStore.logout()
    }
  }

  if (to.meta.public) {
    if (authStore.isAuthenticated) {
      return { name: 'dashboard' }
    }

    return true
  }

  if (!authStore.isAuthenticated) {
    return { name: 'login' }
  }

  return true
})

export default router
