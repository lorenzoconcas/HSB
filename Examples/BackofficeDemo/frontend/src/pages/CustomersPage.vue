<template>
  <AppShell>
    <section class="grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_0.85fr]">
      <article class="panel overflow-hidden">
        <div class="border-b border-slate-200 px-5 py-4">
          <div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">CRM</p>
              <h3 class="mt-1 text-lg font-semibold">Customers</h3>
            </div>
            <input v-model="search" class="input-base max-w-sm" placeholder="Search name, email or code" />
          </div>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full text-left text-sm">
            <thead class="bg-slate-50 text-slate-500">
              <tr>
                <th class="px-5 py-3 font-medium">Customer</th>
                <th class="px-5 py-3 font-medium">Contact</th>
                <th class="px-5 py-3 font-medium">City</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="customer in customers"
                :key="customer.id"
                class="cursor-pointer border-t border-slate-100 transition hover:bg-slate-50"
                @click="selectCustomer(customer)"
              >
                <td class="px-5 py-4">
                  <p class="font-semibold text-slate-900">{{ customer.name }}</p>
                  <p class="text-xs text-slate-500">{{ customer.code }}</p>
                </td>
                <td class="px-5 py-4">
                  <p class="text-slate-700">{{ customer.email }}</p>
                  <p class="text-xs text-slate-500">{{ customer.phone }}</p>
                </td>
                <td class="px-5 py-4 text-slate-700">{{ customer.city }}, {{ customer.country }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article class="panel p-5">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Editor</p>
            <h3 class="mt-1 text-lg font-semibold">{{ selectedCustomerId ? 'Update customer' : 'Create customer' }}</h3>
          </div>
          <button class="button-secondary" @click="resetForm">Reset</button>
        </div>

        <form class="mt-5 space-y-4" @submit.prevent="submit">
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Name</label>
            <input v-model="form.name" class="input-base" />
          </div>
          <div class="grid gap-4 md:grid-cols-2">
            <div>
              <label class="mb-2 block text-sm font-medium text-slate-700">Email</label>
              <input v-model="form.email" class="input-base" />
            </div>
            <div>
              <label class="mb-2 block text-sm font-medium text-slate-700">Phone</label>
              <input v-model="form.phone" class="input-base" />
            </div>
          </div>
          <div class="grid gap-4 md:grid-cols-2">
            <div>
              <label class="mb-2 block text-sm font-medium text-slate-700">VAT number</label>
              <input v-model="form.vatNumber" class="input-base" />
            </div>
            <div>
              <label class="mb-2 block text-sm font-medium text-slate-700">City</label>
              <input v-model="form.city" class="input-base" />
            </div>
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Country</label>
            <input v-model="form.country" class="input-base" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium text-slate-700">Notes</label>
            <textarea v-model="form.notes" class="input-base min-h-24"></textarea>
          </div>
          <p v-if="error" class="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{{ error }}</p>
          <button class="button-primary" type="submit">{{ selectedCustomerId ? 'Save changes' : 'Create customer' }}</button>
        </form>
      </article>
    </section>
  </AppShell>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import AppShell from '@/layouts/AppShell.vue'
import { api } from '@/api/backoffice'
import type { Customer } from '@/lib/types'

const customers = ref<Customer[]>([])
const search = ref('')
const selectedCustomerId = ref('')
const error = ref('')

const emptyForm = (): Customer => ({
  id: '',
  code: '',
  name: '',
  email: '',
  phone: '',
  vatNumber: '',
  city: '',
  country: '',
  notes: '',
  createdAtUtc: '',
})

const form = ref<Customer>(emptyForm())

async function load() {
  const query = search.value ? `/?search=${encodeURIComponent(search.value)}` : ''
  const result = await api.listCustomers(query)
  customers.value = result.items
}

function selectCustomer(customer: Customer) {
  selectedCustomerId.value = customer.id
  form.value = { ...customer }
}

function resetForm() {
  selectedCustomerId.value = ''
  form.value = emptyForm()
  error.value = ''
}

async function submit() {
  error.value = ''
  try {
    if (selectedCustomerId.value) {
      await api.updateCustomer(selectedCustomerId.value, form.value)
    } else {
      await api.createCustomer(form.value)
    }

    await load()
    resetForm()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to save customer'
  }
}

watch(search, load)
onMounted(load)
</script>
