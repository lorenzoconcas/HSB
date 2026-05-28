export interface LoginResponse {
  accessToken: string
  username: string
  fullName: string
  roles: string[]
}

export interface CurrentUserResponse {
  username: string
  fullName: string
  roles: string[]
  claims: Record<string, string>
}

export interface Product {
  id: string
  sku: string
  name: string
  description: string
  category: string
  price: number
  stockQuantity: number
  reorderLevel: number
  isActive: boolean
  imageUrl: string
  createdAtUtc: string
  updatedAtUtc: string
}

export interface Customer {
  id: string
  code: string
  name: string
  email: string
  phone: string
  vatNumber: string
  city: string
  country: string
  notes: string
  createdAtUtc: string
}

export interface OrderItem {
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface OrderRecord {
  id: string
  orderNumber: string
  customerId: string
  customerName: string
  status: string
  createdAtUtc: string
  createdBy: string
  items: OrderItem[]
  subtotal: number
  discount: number
  total: number
  notes: string
  attachmentFileName: string
  attachmentUrl: string
}

export interface InventoryAdjustment {
  id: string
  productId: string
  productName: string
  type: string
  quantityDelta: number
  reason: string
  createdBy: string
  createdAtUtc: string
}

export interface AuditEvent {
  id: string
  type: string
  title: string
  description: string
  createdBy: string
  createdAtUtc: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface DashboardSummary {
  totalProducts: number
  totalCustomers: number
  totalOrders: number
  ordersToday: number
  lowStockItems: number
  revenueMonth: number
}

export interface MetricPoint {
  label: string
  value: number
}

export interface NotificationEnvelope {
  type: string
  timestampUtc: string
  payload: unknown
}
