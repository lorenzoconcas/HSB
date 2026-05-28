export function money(value: number): string {
  return new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'EUR',
  }).format(value)
}

export function shortDate(value: string): string {
  return new Date(value).toLocaleString('en-GB', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  })
}
