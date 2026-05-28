import { defineStore } from 'pinia'
import type { NotificationEnvelope } from '@/lib/types'

export const useNotificationsStore = defineStore('notifications', {
  state: () => ({
    connected: false,
    items: [] as NotificationEnvelope[],
    socket: null as WebSocket | null,
  }),
  actions: {
    connect(token: string) {
      if (!token || this.socket) {
        return
      }

      const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:'
      const socket = new WebSocket(`${protocol}//${window.location.host}/ws/notifications?token=${encodeURIComponent(token)}`)

      socket.onopen = () => {
        this.connected = true
      }

      socket.onclose = () => {
        this.connected = false
        this.socket = null
      }

      socket.onerror = () => {
        this.connected = false
      }

      socket.onmessage = (event) => {
        try {
          const message = JSON.parse(event.data) as NotificationEnvelope
          this.items.unshift(message)
          this.items = this.items.slice(0, 50)
        } catch {
          // ignored
        }
      }

      this.socket = socket
    },
    disconnect() {
      this.socket?.close()
      this.socket = null
      this.connected = false
    },
  },
})
