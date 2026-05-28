import { http } from './http'
import type {
  AuditEvent,
  CurrentUserResponse,
  Customer,
  DashboardSummary,
  InventoryAdjustment,
  LoginResponse,
  MetricPoint,
  OrderRecord,
  PagedResult,
  Product,
} from '@/lib/types'

export const api = {
  login(username: string, password: string) {
    return http.post<LoginResponse>('/api/auth/login', { username, password }, { skipAuth: true })
  },
  logout() {
    return http.post<{ ok: boolean }>('/api/auth/logout', {})
  },
  me() {
    return http.get<CurrentUserResponse>('/api/auth/me')
  },
  dashboardSummary() {
    return http.get<DashboardSummary>('/api/dashboard/summary')
  },
  dashboardSales() {
    return http.get<MetricPoint[]>('/api/dashboard/sales')
  },
  dashboardLowStock() {
    return http.get<Product[]>('/api/dashboard/low-stock')
  },
  dashboardRecentOrders() {
    return http.get<OrderRecord[]>('/api/dashboard/recent-orders')
  },
  dashboardActivity() {
    return http.get<{ items: AuditEvent[] }>('/api/dashboard/activity')
  },
  listProducts(query = '') {
    return http.get<{ categories: string[]; result: PagedResult<Product> }>(`/api/products${query}`)
  },
  getProduct(id: string) {
    return http.get<Product>(`/api/products/${id}`)
  },
  createProduct(payload: Partial<Product>) {
    return http.post<Product>('/api/products', payload)
  },
  updateProduct(id: string, payload: Partial<Product>) {
    return http.put<Product>(`/api/products/${id}`, payload)
  },
  updateProductStock(id: string, quantityDelta: number) {
    return http.patch<Product>(`/api/products/${id}/stock`, { quantityDelta })
  },
  uploadProductImage(id: string, file: File) {
    const formData = new FormData()
    formData.append('file', file)
    return http.post<Product>(`/api/products/${id}/image`, formData)
  },
  listCustomers(query = '') {
    return http.get<PagedResult<Customer>>(`/api/customers${query}`)
  },
  getCustomer(id: string) {
    return http.get<Customer>(`/api/customers/${id}`)
  },
  createCustomer(payload: Partial<Customer>) {
    return http.post<Customer>('/api/customers', payload)
  },
  updateCustomer(id: string, payload: Partial<Customer>) {
    return http.put<Customer>(`/api/customers/${id}`, payload)
  },
  customerOrders(id: string) {
    return http.get<OrderRecord[]>(`/api/customers/${id}/orders`)
  },
  listOrders(query = '') {
    return http.get<PagedResult<OrderRecord>>(`/api/orders${query}`)
  },
  getOrder(id: string) {
    return http.get<OrderRecord>(`/api/orders/${id}`)
  },
  createOrder(payload: unknown) {
    return http.post<OrderRecord>('/api/orders', payload)
  },
  updateOrderStatus(id: string, status: string) {
    return http.put<OrderRecord>(`/api/orders/${id}/status`, { status })
  },
  uploadOrderAttachment(id: string, file: File) {
    const formData = new FormData()
    formData.append('file', file)
    return http.post<OrderRecord>(`/api/orders/${id}/attachment`, formData)
  },
  exportOrdersCsv() {
    return window.open('/api/orders/export.csv', '_blank')
  },
  inventoryAdjustments() {
    return http.get<InventoryAdjustment[]>('/api/inventory/adjustments')
  },
  inventoryLowStock() {
    return http.get<Product[]>('/api/inventory/low-stock')
  },
  createAdjustment(payload: unknown) {
    return http.post<InventoryAdjustment>('/api/inventory/adjustments', payload)
  },
  activity() {
    return http.get<{ items: AuditEvent[] }>('/api/activity')
  },
}
