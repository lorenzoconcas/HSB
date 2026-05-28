import { defineStore } from 'pinia'
import { api } from '@/api/backoffice'
import type { CurrentUserResponse } from '@/lib/types'

const TOKEN_KEY = 'backoffice-demo-token'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem(TOKEN_KEY) ?? '',
    user: null as CurrentUserResponse | null,
    loading: false,
  }),
  getters: {
    isAuthenticated: (state) => Boolean(state.token),
    roles: (state) => state.user?.roles ?? [],
  },
  actions: {
    async login(username: string, password: string) {
      this.loading = true
      try {
        const result = await api.login(username, password)
        this.token = result.accessToken
        localStorage.setItem(TOKEN_KEY, this.token)
        await this.fetchMe()
      } finally {
        this.loading = false
      }
    },
    async fetchMe() {
      if (!this.token) {
        this.user = null
        return
      }

      this.user = await api.me()
    },
    async logout() {
      if (this.token) {
        try {
          await api.logout()
        } catch {
          // ignored
        }
      }

      this.token = ''
      this.user = null
      localStorage.removeItem(TOKEN_KEY)
    },
  },
})
