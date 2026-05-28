<template>
  <div class="flex min-h-screen items-center justify-center px-4 py-8">
    <section class="hsb-dark-card w-full max-w-md overflow-hidden">
      <div class="flex flex-col items-center px-8 pb-6 pt-8 text-center">
        <div class="hsb-diamond">
          <img src="@/assets/newlogo.png" alt="logo w-[72px]" />
        </div>
        <p class="mt-6 text-xs font-semibold uppercase tracking-[0.32em] text-slate-400">Backoffice demo</p>
        <h1 class="mt-3 text-3xl font-bold text-white">Sign in</h1>
        <p class="mt-3 max-w-sm text-sm leading-6 text-slate-300">
          A compact operational workspace showcasing middleware, auth, uploads, and live updates.
        </p>
      </div>

      <div class="border-t border-white/5 bg-[#16212a] px-8 py-7 text-white">
        <form class="space-y-4" @submit.prevent="submit">
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-300">Username</label>
            <input v-model="username" class="input-base" placeholder="admin" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-300">Password</label>
            <input v-model="password" class="input-base" type="password" placeholder="admin123" />
          </div>
          <p v-if="error" class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ error }}</p>
          <button class="button-primary w-full justify-center py-3" :disabled="authStore.loading">
            {{ authStore.loading ? 'Signing in…' : 'Login' }}
          </button>
        </form>

        <div class="mt-6 rounded-2xl border border-white/10 bg-white/5 p-4">
          <p class="text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Demo accounts</p>
          <div class="mt-4 space-y-2.5 text-sm text-slate-300">
            <div class="flex items-center justify-between rounded-xl border border-white/10 bg-white/5 px-3 py-2.5">
              <span><strong>admin</strong> / admin123</span>
              <span class="badge-base bg-white/10 text-white">admin</span>
            </div>
            <div class="flex items-center justify-between rounded-xl border border-white/10 bg-white/5 px-3 py-2.5">
              <span><strong>manager</strong> / manager123</span>
              <span class="badge-base bg-white/10 text-white">manager</span>
            </div>
            <div class="flex items-center justify-between rounded-xl border border-white/10 bg-white/5 px-3 py-2.5">
              <span><strong>operator</strong> / operator123</span>
              <span class="badge-base bg-white/10 text-white">operator</span>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const router = useRouter()

const username = ref('admin')
const password = ref('admin123')
const error = ref('')

async function submit() {
  error.value = ''
  try {
    await authStore.login(username.value, password.value)
    await router.push({ name: 'dashboard' })
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to login'
  }
}
</script>
